import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Observable, forkJoin, Subject } from 'rxjs';
import { switchMap, debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { BaseUpsertComponent } from '../../common/base-upsert/base-upsert.component';
import { FilmService, PhotoService, SessionService, DevKitService, UsedFilmThumbnailService } from '../../../services';
import { FilmType, UsernameType } from '../../../enums';
import { FilmDto, PhotoBulkUploadDto, PhotoUploadDto, SessionDto, DevKitDto, UsedFilmThumbnailDto, ExposureDateEntry } from '../../../DTOs';
import { FileUploadHelper } from '../../../helpers/file-upload.helper';
import { DateHelper } from '../../../helpers/date.helper';
import { ErrorHandlingHelper } from '../../../helpers/error-handling.helper';

@Component({
    selector: 'app-upsert-film',
    templateUrl: './upsert-film.component.html',
    styleUrl: './upsert-film.component.css',
    standalone: false
})
export class UpsertFilmComponent extends BaseUpsertComponent<FilmDto> implements OnInit {

  constructor(
    private filmService: FilmService, 
    private photoService: PhotoService,
    private sessionService: SessionService,
    private devKitService: DevKitService,
    private thumbnailService: UsedFilmThumbnailService
  ) {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Load available sessions and DevKits for modals
    this.loadAvailableSessions();
    this.loadAvailableDevKits();
    
    // Watch for changes to the 'developed' field to clear session/DevKit when not developed
    this.form.get('developed')?.valueChanges.subscribe(developed => {
      if (!developed) {
        this.form.patchValue({
          developedInSessionId: null,
          developedWithDevKitId: null
        });
      }
    });
    
    // Set up debounced thumbnail search
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchQuery => {
      this.performThumbnailSearch(searchQuery);
    });

  }

  private performThumbnailSearch(searchQuery: string): void {
    // Search for all thumbnails if query is empty, or filter by query
    this.thumbnailService.searchByFilmName(searchQuery || '').subscribe({
      next: (results) => {
        this.thumbnailSearchResults = results;
        this.showThumbnailDropdown = true;
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'searching thumbnails');
      }
    });
  }

  // Component-specific properties
  filmTypeOptions = Object.values(FilmType);
  purchasedByOptions = Object.values(UsernameType);

  // Modal state
  showSessionModal = false;
  showDevKitModal = false;
  showExpiredDevKits = false;
  
  // Available items for modals
  availableSessions: SessionDto[] = [];
  availableDevKits: DevKitDto[] = [];
  
  // Selected items
  selectedSessionId: string | null = null;
  selectedDevKitId: string | null = null;
  
  // Success message
  successMessage: string | null = null;
  
  // Thumbnail search state
  thumbnailSearchQuery: string = '';
  thumbnailSearchResults: UsedFilmThumbnailDto[] = [];
  showThumbnailDropdown: boolean = false;
  
  // Add thumbnail modal state
  showAddThumbnailModal: boolean = false;
  newThumbnailFile: File | null = null;
  newThumbnailFilmName: string = '';
  newThumbnailPreview: string = '';

  // Exposure dates modal state
  isExposureDatesModalOpen = false;
  exposureDates: ExposureDateEntry[] = [];
  uploadingThumbnail: boolean = false;
  
  // Thumbnail preview modal
  showThumbnailPreview: boolean = false;
  
  // Bulk upload state
  bulkCount: number = 1;
  
  private searchSubject = new Subject<string>();

  protected createForm(): FormGroup {
    return this.fb.group({
      name: ['', Validators.required],
      iso: ['400', [Validators.required, this.isoValidator]],
      type: [FilmType.ColorNegative, Validators.required],
      numberOfExposures: [36, [Validators.required, Validators.min(1)]],
      cost: [0, [Validators.required, Validators.min(0)]],
      purchasedBy: ['', Validators.required],
      purchasedOn: [DateHelper.getTodayForInput(), Validators.required],
      imageUrl: [''],
      imageId: [''],
      description: [''],
      developed: [false, Validators.required],
      developedInSessionId: [null],
      developedWithDevKitId: [null],
      exposureDates: ['']
    });
  }

  // Custom ISO validator
  private isoValidator(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    
    if (!value) {
      return null; // Let required validator handle empty values
    }

    const iso = value.toString();

    // Check if it contains spaces (not allowed) - before trim!
    if (iso.includes(' ')) {
      return { invalidIso: 'ISO cannot contain spaces' };
    }

    // Check if it's a range (contains dash)
    if (iso.includes('-')) {
      const parts = iso.split('-');
      
      // Must have exactly 2 parts
      if (parts.length !== 2) {
        return { invalidIso: 'ISO range must be in format: number-number (e.g., 100-400)' };
      }

      // Both parts must be valid positive integers and exactly match the parsed value (no trailing chars)
      const first = parseInt(parts[0], 10);
      const second = parseInt(parts[1], 10);

      if (isNaN(first) || isNaN(second) || parts[0] !== first.toString() || parts[1] !== second.toString()) {
        return { invalidIso: 'ISO range values must be numbers' };
      }

      // Both must be greater than 0
      if (first <= 0 || second <= 0) {
        return { invalidIso: 'ISO values must be greater than 0' };
      }

      // First must be less than second
      if (first >= second) {
        return { invalidIso: 'First ISO value must be less than the second (e.g., 100-400)' };
      }

      return null;
    } else {
      // Single number case - must be exactly a number with no other characters
      const parsedValue = parseInt(iso, 10);

      if (isNaN(parsedValue) || iso !== parsedValue.toString()) {
        return { invalidIso: 'ISO must be a number or a range (e.g., 400 or 100-400)' };
      }

      // Must be greater than 0
      if (parsedValue <= 0) {
        return { invalidIso: 'ISO must be greater than 0' };
      }

      return null;
    }
  }

  protected getCreateObservable(item: FilmDto): Observable<any> {
    const queryParams = this.bulkCount > 1 ? { bulkCount: this.bulkCount } : undefined;
    return this.filmService.add(item, queryParams);
  }

  protected getUpdateObservable(id: string, item: FilmDto): Observable<any> {
    // Check if film is being marked as not developed
    if (!item.developed) {
      return this.handleFilmNotDeveloped(id, item);
    }
    
    // If both session and DevKit are assigned, handle both updates
    if (item.developedWithDevKitId && item.developedInSessionId) {
      return this.updateFilmWithBothSessionAndDevKit(id, item);
    }
    
    // If a DevKit is assigned, we need to increment its filmsDeveloped count
    if (item.developedWithDevKitId) {
      return this.updateFilmWithDevKitIncrement(id, item);
    }
    
    // If a session is assigned, we need to update the session's developedFilmsList
    if (item.developedInSessionId) {
      return this.updateFilmWithSessionUpdate(id, item);
    }
    
    return this.filmService.update(id, item);
  }

  private updateFilmWithDevKitIncrement(id: string, item: FilmDto): Observable<any> {
    // First get the current DevKit to increment its filmsDeveloped
    return this.devKitService.getById(item.developedWithDevKitId!).pipe(
      switchMap(devKit => {
        const updatedDevKit = {
          ...devKit,
          filmsDeveloped: (devKit.filmsDeveloped || 0) + 1
        };
        
        // Update both the film and the DevKit
        return forkJoin({
          filmUpdate: this.filmService.update(id, item),
          devKitUpdate: this.devKitService.update(item.developedWithDevKitId!, updatedDevKit)
        });
      })
    );
  }

  private updateFilmWithSessionUpdate(id: string, item: FilmDto): Observable<any> {
    // Get the current session to update its developedFilmsList
    return this.sessionService.getById(item.developedInSessionId!).pipe(
      switchMap(session => {
        const currentDevelopedFilms = session.developedFilmsList || [];
        
        // Add this film to the session's developedFilmsList if not already present
        if (!currentDevelopedFilms.includes(id)) {
          const updatedSession = {
            ...session,
            developedFilmsList: [...currentDevelopedFilms, id]
          };
          
          // Update both the film and the session
          return forkJoin({
            filmUpdate: this.filmService.update(id, item),
            sessionUpdate: this.sessionService.update(item.developedInSessionId!, updatedSession)
          });
        } else {
          // Film is already in the session, just update the film
          return this.filmService.update(id, item);
        }
      })
    );
  }

  private updateFilmWithBothSessionAndDevKit(id: string, item: FilmDto): Observable<any> {
    // Get both the session and DevKit to update both
    return forkJoin({
      session: this.sessionService.getById(item.developedInSessionId!),
      devKit: this.devKitService.getById(item.developedWithDevKitId!)
    }).pipe(
      switchMap(({ session, devKit }) => {
        const currentDevelopedFilms = session.developedFilmsList || [];
        const updatedDevKit = {
          ...devKit,
          filmsDeveloped: (devKit.filmsDeveloped || 0) + 1
        };
        
        // Add this film to the session's developedFilmsList if not already present
        const updatedSession = !currentDevelopedFilms.includes(id) ? {
          ...session,
          developedFilmsList: [...currentDevelopedFilms, id]
        } : session;
        
        // Update film, session, and DevKit
        const updates = [
          this.filmService.update(id, item),
          this.devKitService.update(item.developedWithDevKitId!, updatedDevKit)
        ];
        
        if (updatedSession !== session) {
          updates.push(this.sessionService.update(item.developedInSessionId!, updatedSession));
        }
        
        return forkJoin(updates);
      })
    );
  }

  private handleFilmNotDeveloped(id: string, item: FilmDto): Observable<any> {
    // Get the original film to check what associations need to be cleaned up
    return this.filmService.getById(id).pipe(
      switchMap(originalFilm => {
        const updates = [this.filmService.update(id, item)];
        
        // If the film was previously assigned to a session, remove it from the session's developedFilmsList
        if (originalFilm.developedInSessionId) {
          updates.push(
            this.sessionService.getById(originalFilm.developedInSessionId).pipe(
              switchMap(session => {
                const currentDevelopedFilms = session.developedFilmsList || [];
                const updatedDevelopedFilms = currentDevelopedFilms.filter(filmId => filmId !== id);
                
                const updatedSession = {
                  ...session,
                  developedFilmsList: updatedDevelopedFilms
                };
                
                return this.sessionService.update(originalFilm.developedInSessionId!, updatedSession);
              })
            )
          );
        }
        
        // If the film was previously assigned to a DevKit, decrement the DevKit's filmsDeveloped count
        if (originalFilm.developedWithDevKitId) {
          updates.push(
            this.devKitService.getById(originalFilm.developedWithDevKitId).pipe(
              switchMap(devKit => {
                const updatedDevKit = {
                  ...devKit,
                  filmsDeveloped: Math.max(0, (devKit.filmsDeveloped || 0) - 1)
                };
                
                return this.devKitService.update(originalFilm.developedWithDevKitId!, updatedDevKit);
              })
            )
          );
        }
        
        return forkJoin(updates);
      })
    );
  }

  protected getDeleteObservable(id: string): Observable<any> {
    return this.filmService.deleteById(id);
  }

  protected getItemObservable(id: string): Observable<FilmDto> {
    return this.filmService.getById(id);
  }

  protected getBaseRoute(): string {
    return '/films';
  }

  protected getEntityName(): string {
    return 'Film';
  }

  // Film-specific functionality (photo uploads) - now using helper
  async onUploadPhotos(): Promise<void> {
    try {
      const files = await FileUploadHelper.selectFiles(true, 'image/*');
      if (!files) return; // User cancelled

      await this.processPhotoUploads(files);
    } catch (error) {
      this.loading = false;
      this.errorMessage = ErrorHandlingHelper.handleError(error, 'photo file selection');
    }
  }

  private async processPhotoUploads(files: FileList): Promise<void> {
    this.loading = true;
    this.errorMessage = null;
    
    try {
      // Validate files before processing
      const validation = FileUploadHelper.validateFiles(files);
      if (!validation.isValid) {
        this.errorMessage = validation.errors.join('; ');
        this.loading = false;
        return;
      }

      // Convert files to photo DTOs using helper
      const photos = await FileUploadHelper.filesToPhotoUploadDtos(
        files, 
        (imageBase64) => ({ imageBase64 } as PhotoUploadDto)
      );

      const uploadDto: PhotoBulkUploadDto = {
        filmId: this.id!,
        photos: photos
      };
      
      this.photoService.uploadPhotos(uploadDto).subscribe({
        next: () => {
          this.loading = false;
          // Navigate to the film photos page
          this.router.navigate(['/films', this.id, 'photos']);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'photo upload');
        }
      });
    } catch (error) {
      this.loading = false;
      this.errorMessage = ErrorHandlingHelper.handleError(error, 'photo processing');
    }
  }

  onViewPhotos(): void {
    this.router.navigate(['/films', this.id, 'photos']);
  }

  // Session assignment methods
  onAssignSession(): void {
    this.loadAvailableSessions();
    this.showSessionModal = true;
    // Pre-select current session if already assigned
    this.selectedSessionId = this.form.get('developedInSessionId')?.value || null;
  }

  closeSessionModal(): void {
    this.showSessionModal = false;
    this.selectedSessionId = null;
  }

  selectSession(sessionId: string): void {
    this.selectedSessionId = sessionId;
  }

  assignSession(): void {
    if (this.selectedSessionId) {
      this.form.patchValue({ developedInSessionId: this.selectedSessionId });
      // Clear DevKit selection when session changes
      this.form.patchValue({ developedWithDevKitId: null });
      this.closeSessionModal();
      this.successMessage = 'Session assigned! Changes will be saved when you click Save.';
      setTimeout(() => this.successMessage = null, 3000);
      
      // Refresh DevKit list to show only DevKits from the selected session
      this.loadAvailableDevKits();
    }
  }


  // DevKit assignment methods
  onAssignDevKit(): void {
    this.loadAvailableDevKits();
    this.showDevKitModal = true;
    // Pre-select current DevKit if already assigned
    this.selectedDevKitId = this.form.get('developedWithDevKitId')?.value || null;
    
    // If current DevKit is expired, show expired DevKits by default
    if (this.selectedDevKitId) {
      const currentDevKit = this.availableDevKits.find(dk => dk.id === this.selectedDevKitId);
      if (currentDevKit?.expired) {
        this.showExpiredDevKits = true;
      }
    }
  }

  closeDevKitModal(): void {
    this.showDevKitModal = false;
    this.selectedDevKitId = null;
    // Reset expired checkbox when closing modal
    this.showExpiredDevKits = false;
  }

  selectDevKit(devKitId: string): void {
    this.selectedDevKitId = devKitId;
  }

  assignDevKit(): void {
    if (this.selectedDevKitId) {
      this.form.patchValue({ developedWithDevKitId: this.selectedDevKitId });
      this.closeDevKitModal();
      this.successMessage = 'DevKit assigned! Changes will be saved when you click Save.';
      setTimeout(() => this.successMessage = null, 3000);
    }
  }


  // Helper methods
  private loadAvailableSessions(): void {
    this.sessionService.getAll().subscribe({
      next: (sessions) => {
        this.availableSessions = sessions;
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading sessions');
      }
    });
  }

  private loadAvailableDevKits(): void {
    // If a session is selected, we need to get DevKits from that session
    const selectedSessionId = this.form.get('developedInSessionId')?.value;
    
    if (selectedSessionId) {
      // Load DevKits from the specific session
      this.sessionService.getById(selectedSessionId).subscribe({
        next: (session) => {
          const useddevKitIds = session.usedSubstancesList || [];
          
          this.devKitService.getAll().subscribe({
            next: (allDevKits) => {
              this.availableDevKits = allDevKits.filter(devKit => 
                useddevKitIds.includes(devKit.id)
              );
              this.validateCurrentDevKitSelection();
            },
            error: (err) => {
              this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading devkits');
            }
          });
        },
        error: (err) => {
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading session devkits');
        }
      });
    } else {
      // No session selected, show all DevKits
      this.devKitService.getAll().subscribe({
        next: (devKits) => {
          this.availableDevKits = devKits;
          this.validateCurrentDevKitSelection();
        },
        error: (err) => {
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading devkits');
        }
      });
    }
  }

  get filteredAvailableDevKits(): DevKitDto[] {
    if (this.showExpiredDevKits) {
      // Show all DevKits, but sort so expired appear last, then alphabetically within each group
      return this.availableDevKits.sort((a, b) => {
        if (a.expired !== b.expired) {
          return a.expired ? 1 : -1; // Non-expired first
        }
        return a.name.localeCompare(b.name);
      });
    }
    // Show only non-expired DevKits, sorted alphabetically
    return this.availableDevKits
      .filter(devKit => !devKit.expired)
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  get hasExpiredDevKits(): boolean {
    return this.availableDevKits.some(devKit => devKit.expired);
  }

  // Method to check if current DevKit selection is still valid after session change
  private validateCurrentDevKitSelection(): void {
    const currentDevKitId = this.form.get('developedWithDevKitId')?.value;
    const currentSessionId = this.form.get('developedInSessionId')?.value;
    
    if (currentDevKitId && currentSessionId) {
      // Check if the current DevKit is still available in the selected session
      const isDevKitInSession = this.availableDevKits.some(devKit => devKit.id === currentDevKitId);
      if (!isDevKitInSession) {
        // Clear the DevKit selection if it's not available in the current session
        this.form.patchValue({ developedWithDevKitId: null });
        this.successMessage = 'DevKit selection cleared because it\'s not available in the selected session.';
        setTimeout(() => this.successMessage = null, 3000);
      }
    }
  }

  // Thumbnail search methods
  onThumbnailSearchClick(): void {
    // Show all thumbnails when clicking on search box
    this.performThumbnailSearch('');
  }

  onThumbnailSearchChange(): void {
    this.searchSubject.next(this.thumbnailSearchQuery);
  }

  onSelectThumbnail(thumbnail: UsedFilmThumbnailDto): void {
    // Set the imageUrl and imageId from the selected thumbnail
    this.form.patchValue({ 
      imageUrl: thumbnail.imageUrl,
      imageId: thumbnail.imageId
    });
    this.thumbnailSearchQuery = thumbnail.filmName;
    this.showThumbnailDropdown = false;
  }

  onThumbnailSearchBlur(): void {
    // Delay closing to allow click events on dropdown items to fire
    setTimeout(() => {
      this.closeThumbnailDropdown();
    }, 200);
  }

  closeThumbnailDropdown(): void {
    this.showThumbnailDropdown = false;
  }

  // Add new thumbnail methods
  get canAddThumbnail(): boolean {
    const filmName = this.form.get('name')?.value;
    return filmName && filmName.trim().length > 0;
  }

  onAddNewThumbnail(): void {
    if (!this.canAddThumbnail) return;
    
    const filmName = this.form.get('name')?.value;
    const iso = this.form.get('iso')?.value;
    // Include ISO in the film name for better identification
    this.newThumbnailFilmName = iso ? `${filmName} ${iso}` : filmName;
    this.showAddThumbnailModal = true;
  }

  onThumbnailFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.newThumbnailFile = file;
      
      // Create preview
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        this.newThumbnailPreview = reader.result as string;
      };
    }
  }

  onUploadThumbnail(): void {
    if (!this.newThumbnailFile || !this.newThumbnailFilmName.trim()) {
      this.errorMessage = 'Please select a file and provide a film name.';
      return;
    }

    this.uploadingThumbnail = true;
    this.errorMessage = null;

    const reader = new FileReader();
    reader.readAsDataURL(this.newThumbnailFile);
    reader.onload = () => {
      const imageBase64 = reader.result as string;
      
      this.thumbnailService.uploadThumbnail(this.newThumbnailFilmName, imageBase64).subscribe({
        next: (uploadedThumbnail) => {
          this.uploadingThumbnail = false;
          
          // Auto-select the newly uploaded thumbnail
          this.form.patchValue({ 
            imageUrl: uploadedThumbnail.imageUrl,
            imageId: uploadedThumbnail.imageId
          });
          
          // Set the search query to show the uploaded film name
          this.thumbnailSearchQuery = uploadedThumbnail.filmName;
          
          // Close modal and reset
          this.closeAddThumbnailModal();
        },
        error: (err) => {
          this.uploadingThumbnail = false;
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'uploading thumbnail');
        }
      });
    };
  }

  closeAddThumbnailModal(): void {
    this.showAddThumbnailModal = false;
    this.newThumbnailFile = null;
    this.newThumbnailFilmName = '';
    this.newThumbnailPreview = '';
  }

  // Thumbnail preview methods
  get hasThumbnailSelected(): boolean {
    const imageUrl = this.form.get('imageUrl')?.value;
    return imageUrl && imageUrl.trim().length > 0;
  }

  openThumbnailPreview(): void {
    if (this.hasThumbnailSelected) {
      this.showThumbnailPreview = true;
    }
  }

  closeThumbnailPreview(): void {
    this.showThumbnailPreview = false;
  }

  // Bulk upload methods
  incrementBulkCount(): void {
    if (this.bulkCount < 10) {
      this.bulkCount++;
    }
  }

  decrementBulkCount(): void {
    if (this.bulkCount > 1) {
      this.bulkCount--;
    }
  }

  getBulkSaveButtonText(): string {
    return this.bulkCount === 1 ? 'Save' : `Save ${this.bulkCount} Films`;
  }

  // Exposure dates methods
  openExposureDatesModal(): void {
    this.isExposureDatesModalOpen = true;
    
    // Load existing exposure dates from form if editing
    if (!this.isInsert && this.form.get('exposureDates')?.value) {
      const exposureDatesValue = this.form.get('exposureDates')?.value;
      if (typeof exposureDatesValue === 'string' && exposureDatesValue.trim() !== '') {
        try {
          this.exposureDates = JSON.parse(exposureDatesValue);
        } catch (e) {
          console.error('Error parsing exposure dates JSON:', e);
          this.exposureDates = [];
        }
      } else if (Array.isArray(exposureDatesValue)) {
        this.exposureDates = exposureDatesValue;
      } else {
        this.exposureDates = [];
      }
    }
    
    // Initialize with one empty row if no data exists
    if (this.exposureDates.length === 0) {
      this.exposureDates = [{ date: '', description: '' }];
    }
  }

  closeExposureDatesModal(): void {
    this.isExposureDatesModalOpen = false;
  }

  addExposureDateRow(): void {
    this.exposureDates.push({ date: '', description: '' });
  }

  removeExposureDateRow(index: number): void {
    if (this.exposureDates.length > 1) {
      this.exposureDates.splice(index, 1);
    } else {
      // If it's the last row, clear the values but keep the row
      this.exposureDates[0] = { date: '', description: '' };
    }
  }

  // Save exposure dates when Save button is clicked in modal
  saveExposureDates(): void {
    // Filter out empty entries and trim whitespace
    const validExposureDates = this.exposureDates.filter(entry => 
      entry.date && entry.date.trim() !== ''
    ).map(entry => ({
      date: entry.date.trim(),
      description: entry.description.trim()
    }));
    
    // Serialize to JSON string for backend, or empty string if no valid dates
    const exposureDatesJson = validExposureDates.length > 0 ? JSON.stringify(validExposureDates) : '';
    
    // Update the form control with the JSON string
    this.form.patchValue({ exposureDates: exposureDatesJson });
    
    // Close the modal
    this.closeExposureDatesModal();
  }

}
