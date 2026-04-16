import {
  Component,
  EventEmitter,
  HostListener,
  inject,
  Input,
  ElementRef,
  Output,
  OnChanges,
  OnInit,
  SimpleChanges,
} from '@angular/core';
import { Router } from '@angular/router';
import { PhotoService } from '../../../services';
import { PhotoDto, FilmDto, CollectionOptionDto } from '../../../DTOs';

@Component({
  selector: 'app-photos-content',
  templateUrl: './photos-content.component.html',
  styleUrl: './photos-content.component.css',
  standalone: false,
})
export class PhotosContentComponent implements OnInit, OnChanges {
  private router = inject(Router);
  private elementRef = inject(ElementRef);
  public photoService = inject(PhotoService);

  @Input() photos: PhotoDto[] = [];
  @Input() film: FilmDto | null = null;
  @Input() mode: 'edit' | 'view' | 'ideaResults' | 'collectionEdit' | 'publicCollection' = 'view';
  /** Hide restrict/delete in preview; only Download + navigation. */
  @Input() previewDownloadOnly = false;
  @Input() isOwner = false;
  @Input() uploadLoading = false;
  @Input() uploadProgress: { current: number; total: number } = { current: 0, total: 0 };
  @Input() downloadAllLoading = false;
  @Input() showOwner = false;
  /** Owner-only: bulk select, delete, attach to wacky idea */
  @Input() bulkSelectionEnabled = false;

  ngOnInit(): void {
    this.rebuildAllowedBulkIdSet();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['photos'] && this.photos.length === 0) {
      this.closePreview();
    }
    if (changes['photos'] && this.currentPreviewPhoto && this.photos.length > 0) {
      const stillPresent = this.photos.some((p) => p.id === this.currentPreviewPhoto!.id);
      if (!stillPresent) {
        this.closePreview();
      }
    }
    if (changes['photos'] && this.selectedPhotoIds.size > 0) {
      const valid = new Set(this.photos.map((p) => p.id));
      const next = new Set([...this.selectedPhotoIds].filter((id) => valid.has(id)));
      if (next.size !== this.selectedPhotoIds.size) {
        this.selectedPhotoIds = next;
      }
    }
  }

  @Output() addPhotos = new EventEmitter<void>();
  @Output() download = new EventEmitter<PhotoDto>();
  @Output() downloadAll = new EventEmitter<boolean>();
  @Output() downloadSelected = new EventEmitter<{ small: boolean; photos: PhotoDto[] }>();
  @Output() deletePhoto = new EventEmitter<PhotoDto>();
  @Output() restrictToggle = new EventEmitter<PhotoDto>();
  @Output() bulkDeleteRequest = new EventEmitter<PhotoDto[]>();
  @Output() wackyResultRequest = new EventEmitter<PhotoDto[]>();
  /** Idea results page: remove link only (not blob delete) */
  @Output() removeLinkedPhotosRequest = new EventEmitter<PhotoDto[]>();
  @Output() collectionRemoveRequest = new EventEmitter<PhotoDto[]>();
  @Output() collectionFeaturedRequest = new EventEmitter<PhotoDto>();
  /** Owner-only: open collections for bulk “Add to collection”. */
  @Input() openCollectionOptions: CollectionOptionDto[] = [];
  @Input() addToCollectionBusy = false;
  @Output() addToCollectionRequest = new EventEmitter<{ collectionId: string; photoIds: string[] }>();
  collectionSubmenuOpen = false;
  private _allowedBulkPhotoIds: string[] | null = null;

  /**
   * When null/undefined: no allowlist — all photos are bulk-eligible.
   * When [] or non-empty: only IDs in the set are eligible ([] means none).
   */
  @Input()
  set allowedBulkPhotoIds(value: string[] | null) {
    this._allowedBulkPhotoIds = value;
    this.rebuildAllowedBulkIdSet();
  }

  get allowedBulkPhotoIds(): string[] | null {
    return this._allowedBulkPhotoIds;
  }

  isPreviewModalOpen = false;
  currentPreviewPhoto: PhotoDto | null = null;
  currentPhotoIndex = 0;
  isDeleteModalOpen = false;
  downloadDropdownOpen = false;
  optionsDropdownOpen = false;
  bulkSelectionMode = false;
  selectedPhotoIds = new Set<string>();
  /** null = no allowlist; otherwise membership set (may be empty). */
  private allowedBulkIdSet: Set<string> | null = null;

  private rebuildAllowedBulkIdSet(): void {
    if (this._allowedBulkPhotoIds == null) {
      this.allowedBulkIdSet = null;
    } else {
      this.allowedBulkIdSet = new Set(this._allowedBulkPhotoIds);
    }
  }

  get selectedBulkCount(): number {
    return this.selectedPhotoIds.size;
  }

  /** Hide “N photos” beside the toolbar for collection public/edit (owner request). */
  showPhotoCountInBar(): boolean {
    if (this.mode === 'collectionEdit' || this.mode === 'publicCollection') {
      return false;
    }
    return true;
  }

  getSelectedPhotos(): PhotoDto[] {
    return this.photos.filter((p) => this.selectedPhotoIds.has(p.id));
  }

  startBulkSelection(): void {
    this.bulkSelectionMode = true;
    this.selectedPhotoIds = new Set();
    this.optionsDropdownOpen = false;
    this.downloadDropdownOpen = false;
  }

  /** Photos that can be toggled in bulk mode (respects idea-results allowlist). */
  getEligibleBulkPhotos(): PhotoDto[] {
    if (this.allowedBulkIdSet === null) {
      return this.photos;
    }
    return this.photos.filter((p) => this.allowedBulkIdSet!.has(p.id));
  }

  selectAllPhotos(): void {
    const eligible = this.getEligibleBulkPhotos();
    this.selectedPhotoIds = new Set(eligible.map((p) => p.id));
  }

  /** Select every eligible photo, or clear selection when all are already selected. */
  toggleSelectAllOrDeselectAll(): void {
    if (this.allEligibleBulkSelected) {
      this.selectedPhotoIds = new Set();
    } else {
      this.selectAllPhotos();
    }
  }

  get allEligibleBulkSelected(): boolean {
    const eligible = this.getEligibleBulkPhotos();
    if (eligible.length === 0) {
      return false;
    }
    return eligible.every((p) => this.selectedPhotoIds.has(p.id));
  }

  cancelBulkSelection(): void {
    this.bulkSelectionMode = false;
    this.selectedPhotoIds = new Set();
    this.optionsDropdownOpen = false;
  }

  exitBulkSelectionMode(): void {
    this.cancelBulkSelection();
  }

  toggleOptionsDropdown(): void {
    const next = !this.optionsDropdownOpen;
    if (next) {
      this.downloadDropdownOpen = false;
      this.collectionSubmenuOpen = false;
    } else {
      this.collectionSubmenuOpen = false;
    }
    this.optionsDropdownOpen = next;
  }

  toggleCollectionSubmenu(event: Event): void {
    event.stopPropagation();
    this.collectionSubmenuOpen = !this.collectionSubmenuOpen;
  }

  onAddToCollectionPick(collectionId: string): void {
    const photoIds = [...this.selectedPhotoIds];
    if (photoIds.length === 0) {
      return;
    }
    this.addToCollectionRequest.emit({ collectionId, photoIds });
    this.optionsDropdownOpen = false;
    this.collectionSubmenuOpen = false;
  }

  onBulkDeleteFromMenu(): void {
    this.optionsDropdownOpen = false;
    const selected = this.getSelectedPhotos();
    if (selected.length === 0) return;
    this.bulkDeleteRequest.emit(selected);
  }

  onWackyResultFromMenu(): void {
    this.optionsDropdownOpen = false;
    const selected = this.getSelectedPhotos();
    if (selected.length === 0) return;
    this.wackyResultRequest.emit(selected);
  }

  isPhotoBulkSelected(photo: PhotoDto): boolean {
    return this.selectedPhotoIds.has(photo.id);
  }

  togglePhotoBulkSelected(photo: PhotoDto): void {
    const next = new Set(this.selectedPhotoIds);
    if (next.has(photo.id)) {
      next.delete(photo.id);
    } else {
      next.add(photo.id);
    }
    this.selectedPhotoIds = next;
  }

  canToggleBulkForPhoto(photo: PhotoDto): boolean {
    if (this.allowedBulkIdSet === null) {
      return true;
    }
    return this.allowedBulkIdSet.has(photo.id);
  }

  onPhotoItemClick(photo: PhotoDto): void {
    if (this.bulkSelectionMode && this.bulkSelectionEnabled) {
      if (!this.canToggleBulkForPhoto(photo)) {
        return;
      }
      this.togglePhotoBulkSelected(photo);
      return;
    }
    this.openPreview(photo);
  }

  onRemoveFromIdeaMenu(): void {
    this.optionsDropdownOpen = false;
    const selected = this.getSelectedPhotos();
    if (selected.length === 0) {
      return;
    }
    this.removeLinkedPhotosRequest.emit(selected);
  }

  onCollectionRemoveFromMenu(): void {
    this.optionsDropdownOpen = false;
    const selected = this.getSelectedPhotos();
    if (selected.length === 0) return;
    this.collectionRemoveRequest.emit(selected);
  }

  onCollectionFeaturedFromMenu(): void {
    this.optionsDropdownOpen = false;
    const selected = this.getSelectedPhotos();
    if (selected.length !== 1) return;
    this.collectionFeaturedRequest.emit(selected[0]);
  }

  displayIndex(photo: PhotoDto): string {
    const n = photo.collectionIndex ?? photo.index;
    return n.toString().padStart(3, '0');
  }

  openPreview(photo: PhotoDto) {
    this.currentPreviewPhoto = photo;
    const foundIndex = this.photos.findIndex((p) => p.id === photo.id);
    this.currentPhotoIndex = foundIndex >= 0 ? foundIndex : 0;
    this.isPreviewModalOpen = true;
    setTimeout(() => {
      const overlay = document.querySelector('.image-preview-overlay') as HTMLElement;
      if (overlay) overlay.focus();
    }, 100);
  }

  closePreview() {
    this.isPreviewModalOpen = false;
    this.currentPreviewPhoto = null;
    this.currentPhotoIndex = 0;
  }

  previousPhoto() {
    if (this.currentPhotoIndex > 0) {
      this.currentPhotoIndex--;
      this.currentPreviewPhoto = this.photos[this.currentPhotoIndex];
    }
  }

  nextPhoto() {
    if (this.currentPhotoIndex < this.photos.length - 1) {
      this.currentPhotoIndex++;
      this.currentPreviewPhoto = this.photos[this.currentPhotoIndex];
    }
  }

  canNavigatePrevious(): boolean {
    return this.currentPhotoIndex > 0;
  }

  canNavigateNext(): boolean {
    return this.currentPhotoIndex < this.photos.length - 1;
  }

  onKeyDown(event: KeyboardEvent) {
    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.previousPhoto();
    } else if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.nextPhoto();
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.closePreview();
    }
  }

  openDeleteModal() {
    this.isDeleteModalOpen = true;
  }

  closeDeleteModal() {
    this.isDeleteModalOpen = false;
  }

  getDeleteConfirmMessage(): string {
    if (!this.currentPreviewPhoto) return '';
    const index = this.currentPreviewPhoto.index.toString().padStart(3, '0');
    return `Are you sure you want to delete photo #${index}?`;
  }

  confirmDelete() {
    if (this.currentPreviewPhoto) {
      this.deletePhoto.emit(this.currentPreviewPhoto);
      this.closeDeleteModal();
    }
  }

  onDownload(photo: PhotoDto) {
    this.download.emit(photo);
  }

  onRestrictToggle() {
    if (this.currentPreviewPhoto) this.restrictToggle.emit(this.currentPreviewPhoto);
  }

  toggleDownloadDropdown(): void {
    const next = !this.downloadDropdownOpen;
    if (next) {
      this.optionsDropdownOpen = false;
    }
    this.downloadDropdownOpen = next;
  }

  onDownloadAll(small: boolean) {
    this.downloadDropdownOpen = false;
    this.downloadAll.emit(small);
  }

  onDownloadSelected(small: boolean): void {
    const photos = this.getSelectedPhotos();
    if (photos.length === 0) {
      return;
    }
    this.downloadDropdownOpen = false;
    this.optionsDropdownOpen = false;
    this.downloadSelected.emit({ small, photos });
  }

  navigateToEditFilm() {
    if (this.film?.id) this.router.navigate(['/films', this.film.id]);
  }

  navigateToEditPhotos() {
    if (this.film?.id) this.router.navigate(['/films', this.film.id, 'photos']);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdownContainer = this.elementRef.nativeElement.querySelector('.download-dropdown-container');
    if (dropdownContainer && !dropdownContainer.contains(target)) {
      this.downloadDropdownOpen = false;
    }
    const optionsContainer = this.elementRef.nativeElement.querySelector('.options-dropdown-container');
    if (optionsContainer && !optionsContainer.contains(target)) {
      this.optionsDropdownOpen = false;
      this.collectionSubmenuOpen = false;
    }
  }
}
