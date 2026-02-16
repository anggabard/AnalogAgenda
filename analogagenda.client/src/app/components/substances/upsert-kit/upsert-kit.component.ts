import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators } from '@angular/forms';
import { Observable, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { BaseUpsertComponent } from '../../common';
import { DevKitService, UsedDevKitThumbnailService } from '../../../services';
import { DevKitType, UsernameType } from '../../../enums';
import { DevKitDto, UsedDevKitThumbnailDto } from '../../../DTOs';
import { DateHelper } from '../../../helpers/date.helper';
import { ErrorHandlingHelper } from '../../../helpers/error-handling.helper';

@Component({
    selector: 'app-upsert-kit',
    templateUrl: './upsert-kit.component.html',
    styleUrl: './upsert-kit.component.css',
    standalone: false
})
export class UpsertKitComponent extends BaseUpsertComponent<DevKitDto> implements OnInit {

  constructor(
    private devKitService: DevKitService,
    private thumbnailService: UsedDevKitThumbnailService
  ) {
    super();
  }

  override ngOnInit(): void {
    super.ngOnInit();
    
    // Setup thumbnail search with debounce
    this.searchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(query => this.performThumbnailSearch(query))
    ).subscribe({
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
  devKitOptions = Object.values(DevKitType);
  purchasedByOptions = Object.values(UsernameType);

  // Thumbnail search state
  thumbnailSearchQuery: string = '';
  thumbnailSearchResults: UsedDevKitThumbnailDto[] = [];
  showThumbnailDropdown: boolean = false;
  
  // Add thumbnail modal state
  showAddThumbnailModal: boolean = false;
  newThumbnailFile: File | null = null;
  newThumbnailDevKitName: string = '';
  newThumbnailPreview: string = '';
  uploadingThumbnail: boolean = false;
  
  // Thumbnail preview modal
  showThumbnailPreview: boolean = false;
  
  private searchSubject = new Subject<string>();

  protected createForm(): FormGroup {
    return this.fb.group({
      name: ['', Validators.required],
      url: ['', [Validators.required]],
      type: [DevKitType.C41, Validators.required],
      purchasedBy: ['', Validators.required],
      purchasedOn: [DateHelper.getTodayForInput(), Validators.required],
      mixedOn: [''],
      validForWeeks: [6, Validators.required],
      validForFilms: [8, Validators.required],
      filmsDeveloped: [0, Validators.required],
      imageUrl: [''],
      imageId: [''],
      description: [''],
      expired: [false, Validators.required]
    });
  }

  protected getCreateObservable(item: DevKitDto): Observable<any> {
    return this.devKitService.add(item);
  }

  protected getUpdateObservable(id: string, item: DevKitDto): Observable<any> {
    return this.devKitService.update(id, item);
  }

  protected getDeleteObservable(id: string): Observable<any> {
    return this.devKitService.deleteById(id);
  }

  protected getItemObservable(id: string): Observable<DevKitDto> {
    return this.devKitService.getById(id);
  }

  protected getBaseRoute(): string {
    return '/substances';
  }

  protected getEntityName(): string {
    return 'Kit';
  }

  override submit(): void {
    if (this.form.invalid) return;

    const raw = this.form.value as DevKitDto;
    const mixedOnVal = raw.mixedOn?.trim();
    const payload: DevKitDto = {
      ...raw,
      mixedOn: mixedOnVal ? mixedOnVal : (null as unknown as string)
    };

    this.loading = true;
    this.errorMessage = null;

    const operation$ = this.isInsert
      ? this.getCreateObservable(payload)
      : this.getUpdateObservable(this.id!, payload);

    const actionName = this.isInsert ? 'saving' : 'updating';

    operation$.subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate([this.getBaseRoute()]);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, `${actionName} ${this.getEntityName()}`);
      }
    });
  }

  // Thumbnail search methods
  performThumbnailSearch(query: string): Observable<UsedDevKitThumbnailDto[]> {
    return this.thumbnailService.searchByDevKitName(query);
  }

  onThumbnailSearchClick(): void {
    this.performThumbnailSearch('').subscribe({
      next: (results) => {
        this.thumbnailSearchResults = results;
        this.showThumbnailDropdown = true;
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading thumbnails');
      }
    });
  }

  onThumbnailSearchChange(): void {
    this.searchSubject.next(this.thumbnailSearchQuery);
  }

  onSelectThumbnail(thumbnail: UsedDevKitThumbnailDto): void {
    // Set the imageUrl and imageId from the selected thumbnail
    this.form.patchValue({ 
      imageUrl: thumbnail.imageUrl,
      imageId: thumbnail.imageId
    });
    this.thumbnailSearchQuery = thumbnail.devKitName;
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
    const devKitName = this.form.get('name')?.value;
    return devKitName && devKitName.trim().length > 0;
  }

  onAddNewThumbnail(): void {
    if (!this.canAddThumbnail) return;
    
    const devKitName = this.form.get('name')?.value;
    const devKitType = this.form.get('type')?.value;
    // Include type in the devkit name for better identification
    this.newThumbnailDevKitName = devKitType ? `${devKitName} ${devKitType}` : devKitName;
    this.showAddThumbnailModal = true;
  }

  onThumbnailFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.newThumbnailFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.newThumbnailPreview = reader.result as string;
      };
      reader.readAsDataURL(file);
    }
  }

  onUploadThumbnail(): void {
    if (!this.newThumbnailFile || !this.newThumbnailDevKitName) {
      this.errorMessage = 'Please select a file and enter a devkit name.';
      return;
    }

    this.uploadingThumbnail = true;
    this.errorMessage = null;

    const reader = new FileReader();
    reader.readAsDataURL(this.newThumbnailFile);
    reader.onload = () => {
      const imageBase64 = reader.result as string;
      
      this.thumbnailService.uploadThumbnail(this.newThumbnailDevKitName, imageBase64).subscribe({
        next: (uploadedThumbnail) => {
          this.uploadingThumbnail = false;
          
          // Auto-select the newly uploaded thumbnail
          this.form.patchValue({ 
            imageUrl: uploadedThumbnail.imageUrl,
            imageId: uploadedThumbnail.imageId
          });
          
          // Set the search query to show the uploaded devkit name
          this.thumbnailSearchQuery = uploadedThumbnail.devKitName;
          
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
    this.newThumbnailDevKitName = '';
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

  /** Expiration date as "day - month - year" for tooltip, or null when mixedOn is not set */
  get expirationDateTooltip(): string | null {
    const mixedOn = this.form.get('mixedOn')?.value as string | null | undefined;
    const validForWeeks = this.form.get('validForWeeks')?.value as number;
    if (!mixedOn || typeof mixedOn !== 'string' || !mixedOn.trim() || validForWeeks == null || validForWeeks <= 0) return null;
    const s = mixedOn.trim();
    const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(s);
    if (!match) return null;
    const y = parseInt(match[1], 10);
    const m = parseInt(match[2], 10) - 1;
    const d = parseInt(match[3], 10);
    const date = new Date(y, m, d);
    if (isNaN(date.getTime())) return null;
    date.setDate(date.getDate() + validForWeeks * 7);
    const day = date.getDate();
    const month = date.getMonth() + 1;
    const year = date.getFullYear();
    return `Expires: ${day} - ${month} - ${year}`;
  }
}
