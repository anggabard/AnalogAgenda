import { Component, HostListener } from '@angular/core';
import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { BaseUpsertComponent } from '../../common';
import { CollectionService, PhotoService } from '../../../services';
import { CollectionDto, PhotoDto } from '../../../DTOs';
import { ErrorHandlingHelper } from '../../../helpers/error-handling.helper';
import { DownloadHelper } from '../../../helpers/download.helper';
import { formatCollectionArchiveDateSegment } from '../../../helpers/date-archive-formatting.helper';

function collectionDateRangeValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const from = (group.get('fromDate')?.value as string | undefined)?.trim();
    const to = (group.get('toDate')?.value as string | undefined)?.trim();
    if (!from || !to) return null;
    if (from <= to) return null;
    return { dateRangeInvalid: true };
  };
}

@Component({
  selector: 'app-upsert-collection',
  templateUrl: './upsert-collection.component.html',
  styleUrl: './upsert-collection.component.css',
  standalone: false,
})
export class UpsertCollectionComponent extends BaseUpsertComponent<CollectionDto> {
  constructor(
    private collectionService: CollectionService,
    private photoService: PhotoService
  ) {
    super();
  }

  /** Server state for photo list (not edited on this page). */
  private loadedCollection: CollectionDto | null = null;

  downloadLoading = false;
  showFeaturedModal = false;
  featuredModalLoading = false;
  featuredModalPhotos: PhotoDto[] = [];

  protected createForm(): FormGroup {
    return this.fb.group(
      {
        id: [''],
        name: ['', Validators.required],
        fromDate: [''],
        toDate: [''],
        location: [''],
        isOpen: [true],
        imageId: [''],
        owner: [''],
      },
      { validators: [collectionDateRangeValidator()] }
    );
  }

  get collectionHasPhotos(): boolean {
    return (this.loadedCollection?.photoCount ?? 0) > 0;
  }

  showNameRequiredError(): boolean {
    const c = this.form.get('name');
    return !!(c && c.invalid && c.touched);
  }

  showDateRangeError(): boolean {
    return !!(
      this.form.hasError('dateRangeInvalid') &&
      (this.form.get('fromDate')?.touched || this.form.get('toDate')?.touched)
    );
  }

  protected getCreateObservable(item: CollectionDto): Observable<CollectionDto> {
    const dto = this.toWriteDto(item, true);
    return this.collectionService.create(dto);
  }

  protected getUpdateObservable(id: string, item: CollectionDto): Observable<CollectionDto> {
    const dto = this.toWriteDto(item, false);
    dto.id = id;
    return this.collectionService.update(id, dto);
  }

  protected getDeleteObservable(id: string): Observable<void> {
    return this.collectionService.deleteById(id);
  }

  protected getItemObservable(id: string): Observable<CollectionDto> {
    return this.collectionService.getById(id);
  }

  protected getBaseRoute(): string {
    return '/collections';
  }

  protected getEntityName(): string {
    return 'collection';
  }

  protected override getDisplayName(item: CollectionDto): string {
    return item.name ?? '';
  }

  protected override afterPatchValueForEdit(item: CollectionDto): void {
    this.loadedCollection = item;
    const fd = item.fromDate ? String(item.fromDate).slice(0, 10) : '';
    const td = item.toDate ? String(item.toDate).slice(0, 10) : '';
    this.form.patchValue({
      fromDate: fd,
      toDate: td,
      imageId: item.imageId ?? '',
    });
    this.form.updateValueAndValidity();
  }

  /** Refreshes membership + featured from server without clobbering unsaved edits. */
  @HostListener('document:visibilitychange')
  onVisibilityChange(): void {
    if (document.visibilityState !== 'visible' || this.isInsert || !this.id) return;
    this.collectionService.getById(this.id).subscribe({
      next: (item) => {
        this.loadedCollection = item;
        const next = item.imageId ?? '';
        const cur = (this.form.get('imageId')?.value as string) ?? '';
        if (cur !== next) {
          this.form.patchValue({ imageId: next }, { emitEvent: false });
        }
      },
      error: () => {},
    });
  }

  private toWriteDto(_item: CollectionDto, isCreate: boolean): CollectionDto {
    const v = this.form.value;
    const photoIds = isCreate ? [] : (this.loadedCollection?.photoIds ?? []);
    return {
      id: isCreate ? '' : (this.loadedCollection?.id ?? ''),
      name: (v.name as string)?.trim() ?? '',
      fromDate: (v.fromDate as string) || null,
      toDate: (v.toDate as string) || null,
      location: (v.location as string) ?? '',
      imageId: ((v.imageId as string) ?? '').trim(),
      isOpen: !!v.isOpen,
      owner: (v.owner as string) ?? '',
      photoIds,
      photoCount: this.loadedCollection?.photoCount ?? 0,
      imageUrl: this.loadedCollection?.imageUrl ?? '',
    };
  }

  openFeaturedModal(): void {
    if (!this.id || this.isInsert) return;
    this.showFeaturedModal = true;
    this.featuredModalLoading = true;
    this.featuredModalPhotos = [];
    this.collectionService.getPhotos(this.id).subscribe({
      next: (rows) => {
        this.featuredModalPhotos = rows ?? [];
        this.featuredModalLoading = false;
      },
      error: (err) => {
        this.featuredModalLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading collection photos');
      },
    });
  }

  closeFeaturedModal(): void {
    this.showFeaturedModal = false;
  }

  isCardImageSelectedInModal(photo: PhotoDto): boolean {
    const cur = ((this.form.get('imageId')?.value as string) ?? '').toLowerCase();
    const id = (photo.imageId ?? '').toLowerCase();
    return !!id && cur === id;
  }

  selectCardImageFromPhoto(photo: PhotoDto): void {
    if (!photo.imageId) return;
    const prev = ((this.form.get('imageId')?.value as string) ?? '').trim();
    if (prev === photo.imageId.trim()) return;
    this.form.patchValue({ imageId: photo.imageId });
    this.form.markAsDirty();
  }

  /** Checkbox checked means “Finished” (collection no longer open). */
  onFinishedToggle(ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    this.form.patchValue({ isOpen: !checked });
  }

  get finishedToggleChecked(): boolean {
    return !this.form.get('isOpen')?.value;
  }

  previewUrl(photo: PhotoDto): string {
    return this.photoService.getPreviewUrl(photo);
  }

  private collectionZipFileName(small: boolean): string {
    const rawName = (this.form.get('name')?.value as string)?.trim() || 'collection';
    const namePart = DownloadHelper.sanitizePathUnsafeChars(rawName);
    const fd = (this.form.get('fromDate')?.value as string)?.trim() || null;
    const td = (this.form.get('toDate')?.value as string)?.trim() || null;
    const dateSeg = formatCollectionArchiveDateSegment(fd, td);
    const datePart = dateSeg ? ` - ${DownloadHelper.sanitizePathUnsafeChars(dateSeg)}` : '';
    const sizeSuffix = small ? '-small' : '';
    return `${namePart}${datePart}${sizeSuffix}.zip`;
  }

  override submit(): void {
    this.form.markAllAsTouched();
    super.submit();
  }

  override onDelete(): void {
    this.isDeleteModalOpen = false;
    super.onDelete();
  }

  onDownload(small: boolean): void {
    if (!this.id || this.isInsert || !this.collectionHasPhotos) return;
    this.downloadLoading = true;
    this.errorMessage = null;
    this.collectionService.downloadArchive(this.id, small).subscribe({
      next: (blob) => {
        DownloadHelper.triggerBlobDownload(blob, this.collectionZipFileName(small));
        this.downloadLoading = false;
      },
      error: (err) => {
        this.downloadLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'downloading archive');
      },
    });
  }
}
