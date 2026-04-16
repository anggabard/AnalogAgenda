import { Component, HostListener } from '@angular/core';
import { AbstractControl, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { BaseUpsertComponent } from '../../common';
import { CollectionService, PhotoService, PublicCollectionService } from '../../../services';
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
    private photoService: PhotoService,
    private publicCollectionService: PublicCollectionService
  ) {
    super();
  }

  private loadedCollection: CollectionDto | null = null;

  collectionPhotos: PhotoDto[] = [];
  photosLoading = false;
  downloadLoading = false;

  showPublicPasswordModal = false;
  publicPasswordDraft = '';
  pendingNavigateToPublic = false;
  /** Loading a suggested password before opening the modal (Regenerate). */
  regeneratePasswordLoading = false;
  /** True when modal was opened via "Regenerate Password" (confirm saves via API, no navigate). */
  publicPasswordModalRegenerateMode = false;
  /** Saving after confirm in regenerate mode. */
  savingPublicPassword = false;

  protected createForm(): FormGroup {
    return this.fb.group(
      {
        id: [''],
        name: ['', Validators.required],
        fromDate: [''],
        toDate: [''],
        location: [''],
        description: [''],
        isOpen: [true],
        isPublic: [false],
        publicPassword: [''],
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
      isPublic: !!item.isPublic,
      description: item.description ?? '',
    });
    this.form.updateValueAndValidity();
    this.loadCollectionPhotos();
  }

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
        this.form.patchValue({ isPublic: !!item.isPublic }, { emitEvent: false });
      },
      error: () => {},
    });
  }

  onPublicPrivateToggle(ev: Event): void {
    const el = ev.target as HTMLInputElement;
    if (el.checked) {
      el.checked = false;
      this.publicPasswordModalRegenerateMode = false;
      this.collectionService.getPublicPasswordSuggestion().subscribe({
        next: (r) => {
          this.publicPasswordDraft = r.password ?? '';
          this.showPublicPasswordModal = true;
        },
        error: () => {
          this.publicPasswordDraft = '';
          this.showPublicPasswordModal = true;
        },
      });
    } else {
      this.form.patchValue({ isPublic: false, publicPassword: '' });
      this.form.markAsDirty();
    }
  }

  confirmPublicPassword(): void {
    const pwd = this.publicPasswordDraft.trim();
    if (!pwd) {
      this.errorMessage = 'Password cannot be empty.';
      return;
    }
    if (pwd.length > 32) {
      this.errorMessage = 'Password must be at most 32 characters.';
      return;
    }
    this.errorMessage = null;

    if (this.publicPasswordModalRegenerateMode) {
      if (!this.id) return;
      this.savingPublicPassword = true;
      this.form.patchValue({ isPublic: true, publicPassword: pwd });
      this.form.markAsDirty();
      const dto = this.toWriteDto({} as CollectionDto, false);
      this.collectionService.update(this.id, dto).subscribe({
        next: (c) => {
          this.loadedCollection = c;
          this.savingPublicPassword = false;
          this.showPublicPasswordModal = false;
          this.publicPasswordModalRegenerateMode = false;
          this.publicPasswordDraft = '';
        },
        error: (err) => {
          this.savingPublicPassword = false;
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'updating password');
        },
      });
      return;
    }

    this.form.patchValue({ isPublic: true, publicPassword: pwd });
    this.form.markAsDirty();
    this.showPublicPasswordModal = false;
    this.pendingNavigateToPublic = true;
  }

  cancelPublicPasswordModal(): void {
    this.showPublicPasswordModal = false;
    this.publicPasswordDraft = '';
    this.publicPasswordModalRegenerateMode = false;
  }

  /** Open password modal with a fresh server suggestion (user can edit, then Confirm to save). */
  openRegeneratePasswordModal(): void {
    if (!this.id) return;
    this.regeneratePasswordLoading = true;
    this.errorMessage = null;
    this.collectionService.getPublicPasswordSuggestion().subscribe({
      next: (r) => {
        this.regeneratePasswordLoading = false;
        this.publicPasswordDraft = (r.password ?? '').trim();
        this.publicPasswordModalRegenerateMode = true;
        this.showPublicPasswordModal = true;
      },
      error: (err) => {
        this.regeneratePasswordLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'generating password');
      },
    });
  }

  /** Checkbox checked means “Finished” (collection no longer open). */
  onFinishedToggle(ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    this.form.patchValue({ isOpen: !checked });
    this.form.markAsDirty();
  }

  get finishedToggleChecked(): boolean {
    return !this.form.get('isOpen')?.value;
  }

  get publicToggleChecked(): boolean {
    return !!this.form.get('isPublic')?.value;
  }

  private toWriteDto(_item: CollectionDto, isCreate: boolean): CollectionDto {
    const v = this.form.value;
    const photoIds = isCreate ? [] : (this.loadedCollection?.photoIds ?? []);
    const isPublic = !!v.isPublic;
    const pwd = ((v.publicPassword as string) ?? '').trim();
    return {
      id: isCreate ? '' : (this.loadedCollection?.id ?? ''),
      name: (v.name as string)?.trim() ?? '',
      fromDate: (v.fromDate as string) || null,
      toDate: (v.toDate as string) || null,
      location: (v.location as string) ?? '',
      description: ((v.description as string) ?? '').trim(),
      imageId: ((v.imageId as string) ?? '').trim(),
      isOpen: !!v.isOpen,
      isPublic,
      publicPassword: isPublic && pwd ? pwd : undefined,
      owner: (v.owner as string) ?? '',
      photoIds,
      photoCount: this.loadedCollection?.photoCount ?? 0,
      imageUrl: this.loadedCollection?.imageUrl ?? '',
    };
  }

  loadCollectionPhotos(): void {
    if (!this.id || this.isInsert) {
      this.collectionPhotos = [];
      return;
    }
    this.photosLoading = true;
    this.collectionService.getPhotos(this.id).subscribe({
      next: (rows) => {
        this.collectionPhotos = rows ?? [];
        this.photosLoading = false;
      },
      error: (err) => {
        this.photosLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading photos');
      },
    });
  }

  onPhotosDownloadAll(small: boolean): void {
    if (!this.id || this.isInsert) return;
    this.downloadLoading = true;
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

  onPhotosDownloadSelected(payload: { small: boolean; photos: PhotoDto[] }): void {
    if (!this.id || this.isInsert) return;
    const ids = payload.photos.map((p) => p.id);
    if (ids.length === 0) return;
    this.downloadLoading = true;
    // Options menu always requests full-size (XL) selection archives.
    this.collectionService.downloadSelectedArchive(this.id, ids, false).subscribe({
      next: (blob) => {
        DownloadHelper.triggerBlobDownload(blob, this.collectionSelectedArchiveFileName());
        this.downloadLoading = false;
      },
      error: (err) => {
        this.downloadLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'downloading archive');
      },
    });
  }

  onCollectionRemove(photos: PhotoDto[]): void {
    if (!this.id || photos.length === 0) return;
    this.collectionService
      .removePhotos(
        this.id,
        photos.map((p) => p.id)
      )
      .subscribe({
        next: (c) => {
          this.loadedCollection = c;
          this.form.patchValue({ imageId: c.imageId ?? '' }, { emitEvent: false });
          this.loadCollectionPhotos();
        },
        error: (err) => {
          this.errorMessage = ErrorHandlingHelper.handleError(err, 'removing photos');
        },
      });
  }

  onCollectionFeatured(photo: PhotoDto): void {
    if (!this.id) return;
    this.collectionService.setFeaturedPhoto(this.id, photo.id).subscribe({
      next: (c) => {
        this.loadedCollection = c;
        this.form.patchValue({ imageId: c.imageId ?? '' });
        this.form.markAsDirty();
        this.loadCollectionPhotos();
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'setting featured photo');
      },
    });
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

  /** Inserts <c>-selected</c> before <c>.zip</c> so the suffix is not treated as part of the extension. */
  private collectionSelectedArchiveFileName(): string {
    const full = this.collectionZipFileName(false);
    const lower = full.toLowerCase();
    if (lower.endsWith('.zip')) {
      return `${full.slice(0, -4)}-selected.zip`;
    }
    return `${full}-selected.zip`;
  }

  openPublicPageInNewTab(): void {
    if (!this.id) return;
    const url = this.router.serializeUrl(this.router.createUrlTree(['/collections', this.id, 'public']));
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  copyPublicPassword(): void {
    const t = this.publicPasswordDraft ?? '';
    if (!t) return;
    void navigator.clipboard.writeText(t);
  }

  onPreviewPhotoDownload(photo: PhotoDto): void {
    this.photoService.downloadPhoto(photo.id).subscribe({
      next: (blob) => {
        const rawName = (this.form.get('name')?.value as string)?.trim() || 'collection';
        const namePart = DownloadHelper.sanitizeForFileName(rawName);
        const idx = String(photo.collectionIndex ?? photo.index).padStart(3, '0');
        const ext = blob.type?.includes('png')
          ? 'png'
          : blob.type?.includes('webp')
            ? 'webp'
            : 'jpg';
        DownloadHelper.triggerBlobDownload(blob, `${idx}-${namePart}.${ext}`);
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'downloading photo');
      },
    });
  }

  override submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.loading = true;
    this.errorMessage = null;

    const obs = this.isInsert
      ? this.getCreateObservable({} as CollectionDto)
      : this.getUpdateObservable(this.id!, {} as CollectionDto);

    obs.subscribe({
      next: (item: CollectionDto) => {
        this.loading = false;
        this.loadedCollection = item;
        if (!this.isInsert) {
          this.loadCollectionPhotos();
        }

        if (this.pendingNavigateToPublic && item.isPublic) {
          const targetId = item.id || this.id;
          this.pendingNavigateToPublic = false;
          if (targetId) {
            const pwd = (this.form.get('publicPassword')?.value as string)?.trim() ?? '';
            const go = () => this.router.navigate(['/collections', targetId, 'public']);
            if (pwd) {
              this.publicCollectionService.verify(targetId, pwd).subscribe({ next: go, error: go });
            } else {
              go();
            }
          } else {
            this.router.navigate([this.getBaseRoute()]);
          }
        } else {
          this.router.navigate([this.getBaseRoute()]);
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(
          err,
          `${this.isInsert ? 'saving' : 'updating'} ${this.getEntityName()}`
        );
      },
    });
  }

  override onDelete(): void {
    this.isDeleteModalOpen = false;
    super.onDelete();
  }
}
