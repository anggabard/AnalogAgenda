import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';
import { BaseUpsertComponent } from '../../common/base-upsert/base-upsert.component';
import { SessionService, DevKitService, FilmService } from '../../../services';
import { UsernameType } from '../../../enums';
import { SessionDto, DevKitDto, FilmDto } from '../../../DTOs';
import { DateHelper } from '../../../helpers/date.helper';

@Component({
  selector: 'app-upsert-session',
  templateUrl: './upsert-session.component.html',
  styleUrl: './upsert-session.component.css'
})
export class UpsertSessionComponent extends BaseUpsertComponent<SessionDto> implements OnInit {

  constructor(
    private sessionService: SessionService, 
    private devKitService: DevKitService,
    private filmService: FilmService
  ) {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    this.loadAvailableItems();
    
    // If editing, process loaded data after base initialization
    if (!this.isInsert && this.rowKey) {
      // Add a small delay to ensure the base class has loaded the item
      setTimeout(() => {
        this.processLoadedItem();
      }, 100);
    }
  }

  // Component-specific properties
  participantOptions = Object.values(UsernameType);
  selectedParticipants: string[] = [];
  successMessage: string | null = null;
  imagePreview: string | null = null;
  override isDeleteModalOpen: boolean = false;
  
  // Available items for selection
  availableDevKits: DevKitDto[] = [];
  availableFilms: FilmDto[] = [];
  selectedDevKits: string[] = [];
  selectedFilms: string[] = [];
  
  // Filters
  showExpiredDevKits = false;

  // Computed properties to match template expectations
  get formGroup(): FormGroup { return this.form; }
  get isEditMode(): boolean { return !this.isInsert; }

  protected createForm(): FormGroup {
    return this.fb.group({
      sessionDate: [DateHelper.getTodayForInput(), Validators.required],
      location: ['', Validators.required],
      participants: [[], Validators.required],
      imageUrl: [''],
      imageBase64: [''],
      description: [''],
      usedSubstances: [[]],
      developedFilms: [[]]
    });
  }

  protected getCreateObservable(item: SessionDto): Observable<any> {
    const processedItem = this.processFormData(item);
    return this.sessionService.add(processedItem);
  }

  protected getUpdateObservable(rowKey: string, item: SessionDto): Observable<any> {
    const processedItem = this.processFormData(item);
    return this.sessionService.update(rowKey, processedItem);
  }

  protected getDeleteObservable(rowKey: string): Observable<any> {
    return this.sessionService.deleteById(rowKey);
  }

  protected getItemObservable(rowKey: string): Observable<SessionDto> {
    return this.sessionService.getById(rowKey);
  }

  protected getBaseRoute(): string {
    return '/sessions';
  }

  protected getEntityName(): string {
    return 'Session';
  }

  // Process form data before submission
  private processFormData(formValue: any): SessionDto {
    return {
      ...formValue,
      participants: JSON.stringify(this.selectedParticipants),
      usedSubstances: JSON.stringify(this.selectedDevKits),
      developedFilms: JSON.stringify(this.selectedFilms)
    };
  }

  // Override submit to handle custom redirection
  override submit(): void {
    if (this.form.invalid) return;

    const formData = this.processFormData(this.form.value);
    this.loading = true;
    this.errorMessage = null;

    const operation$ = this.isInsert 
      ? this.getCreateObservable(formData)
      : this.getUpdateObservable(this.rowKey!, formData);

    operation$.subscribe({
      next: (response: any) => {
        this.loading = false;
        if (this.isInsert) {
          // For new sessions, redirect to the session management view
          const createdSession = response as SessionDto;
          if (createdSession && createdSession.rowKey) {
            this.router.navigate(['/sessions', createdSession.rowKey]);
          } else {
            this.router.navigate(['/sessions']);
          }
        } else {
          // For updates, stay on the same page
          this.successMessage = 'Session updated successfully!';
          setTimeout(() => this.successMessage = null, 3000);
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = `Error ${this.isInsert ? 'creating' : 'updating'} session: ${err.message || 'Unknown error'}`;
      }
    });
  }

  // Handle loaded item for editing
  private processLoadedItem(): void {
    // Get current form values to parse JSON fields
    const formValue = this.form.value;
    
    try {
      this.selectedParticipants = JSON.parse(formValue.participants || '[]');
      this.selectedDevKits = JSON.parse(formValue.usedSubstances || '[]');
      this.selectedFilms = JSON.parse(formValue.developedFilms || '[]');
      
      if (formValue.imageUrl) {
        this.imagePreview = formValue.imageUrl;
      }
    } catch (error) {
      console.error('Error parsing session data:', error);
      this.selectedParticipants = [];
      this.selectedDevKits = [];
      this.selectedFilms = [];
    }
  }

  private loadAvailableItems(): void {
    forkJoin({
      devKits: this.devKitService.getAll(),
      films: this.filmService.getAll()
    }).subscribe({
      next: (data) => {
        this.availableDevKits = data.devKits;
        this.availableFilms = data.films.filter(f => !f.developed); // Only show undeveloped films
      },
      error: (err) => {
        console.error('Error loading available items:', err);
      }
    });
  }

  get filteredDevKits(): DevKitDto[] {
    return this.showExpiredDevKits 
      ? this.availableDevKits 
      : this.availableDevKits.filter(dk => !dk.expired);
  }

  onParticipantChange(participant: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const checked = target.checked;
    
    if (checked) {
      if (!this.selectedParticipants.includes(participant)) {
        this.selectedParticipants.push(participant);
      }
    } else {
      this.selectedParticipants = this.selectedParticipants.filter(p => p !== participant);
    }
    
    // Update the form control to trigger validation
    this.form.patchValue({
      participants: this.selectedParticipants
    });
  }

  onDevKitChange(devKitRowKey: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const checked = target.checked;
    
    if (checked) {
      if (!this.selectedDevKits.includes(devKitRowKey)) {
        this.selectedDevKits.push(devKitRowKey);
      }
    } else {
      this.selectedDevKits = this.selectedDevKits.filter(dk => dk !== devKitRowKey);
    }
  }

  onFilmChange(filmRowKey: string, event: Event): void {
    const target = event.target as HTMLInputElement;
    const checked = target.checked;
    
    if (checked) {
      if (!this.selectedFilms.includes(filmRowKey)) {
        this.selectedFilms.push(filmRowKey);
      }
    } else {
      this.selectedFilms = this.selectedFilms.filter(f => f !== filmRowKey);
    }
  }

  toggleDevKit(devKitRowKey: string): void {
    if (this.selectedDevKits.includes(devKitRowKey)) {
      this.selectedDevKits = this.selectedDevKits.filter(dk => dk !== devKitRowKey);
    } else {
      this.selectedDevKits.push(devKitRowKey);
    }
    
    // Update the form control
    this.form.patchValue({
      usedSubstances: this.selectedDevKits
    });
  }

  toggleFilm(filmRowKey: string): void {
    if (this.selectedFilms.includes(filmRowKey)) {
      this.selectedFilms = this.selectedFilms.filter(f => f !== filmRowKey);
    } else {
      this.selectedFilms.push(filmRowKey);
    }
    
    // Update the form control
    this.form.patchValue({
      developedFilms: this.selectedFilms
    });
  }

  isParticipantSelected(participant: string): boolean {
    return this.selectedParticipants.includes(participant);
  }

  isDevKitSelected(devKitRowKey: string): boolean {
    return this.selectedDevKits.includes(devKitRowKey);
  }

  isFilmSelected(filmRowKey: string): boolean {
    return this.selectedFilms.includes(filmRowKey);
  }

  // Handle file selection with proper typing
  onFileSelected(event: Event): void {
    this.onImageSelected(event);
    
    // Also set image preview
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        this.imagePreview = reader.result as string;
      };
    }
  }

  // Add cancel method
  onCancel(): void {
    this.router.navigate([this.getBaseRoute()]);
  }

  // Add delete method
  override onDelete(): void {
    if (this.rowKey && confirm('Are you sure you want to delete this session?')) {
      this.loading = true;
      this.getDeleteObservable(this.rowKey).subscribe({
        next: () => {
          this.router.navigate([this.getBaseRoute()]);
        },
        error: (err) => {
          this.loading = false;
          this.errorMessage = `Error deleting session: ${err.message || 'Unknown error'}`;
        }
      });
    }
  }
}
