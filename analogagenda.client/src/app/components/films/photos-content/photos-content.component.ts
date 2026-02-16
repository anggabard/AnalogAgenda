import {
  Component,
  EventEmitter,
  HostListener,
  inject,
  Input,
  ElementRef,
  Output,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { Router } from '@angular/router';
import { PhotoService } from '../../../services';
import { PhotoDto, FilmDto } from '../../../DTOs';

@Component({
  selector: 'app-photos-content',
  templateUrl: './photos-content.component.html',
  styleUrl: './photos-content.component.css',
  standalone: false,
})
export class PhotosContentComponent implements OnChanges {
  private router = inject(Router);
  private elementRef = inject(ElementRef);
  public photoService = inject(PhotoService);

  @Input() photos: PhotoDto[] = [];
  @Input() film: FilmDto | null = null;
  @Input() mode: 'edit' | 'view' = 'view';
  @Input() isOwner = false;
  @Input() uploadLoading = false;
  @Input() uploadProgress: { current: number; total: number } = { current: 0, total: 0 };
  @Input() downloadAllLoading = false;
  @Input() showOwner = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['photos'] && this.currentPreviewPhoto && this.photos.length > 0) {
      const stillPresent = this.photos.some((p) => p.id === this.currentPreviewPhoto!.id);
      if (!stillPresent) {
        this.closePreview();
      }
    }
  }

  @Output() addPhotos = new EventEmitter<void>();
  @Output() download = new EventEmitter<PhotoDto>();
  @Output() downloadAll = new EventEmitter<boolean>();
  @Output() deletePhoto = new EventEmitter<PhotoDto>();
  @Output() restrictToggle = new EventEmitter<PhotoDto>();

  isPreviewModalOpen = false;
  currentPreviewPhoto: PhotoDto | null = null;
  currentPhotoIndex = 0;
  isDeleteModalOpen = false;
  downloadDropdownOpen = false;
  private touchStartX = 0;
  private touchStartY = 0;

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

  onTouchStart(event: TouchEvent) {
    this.touchStartX = event.touches[0].clientX;
    this.touchStartY = event.touches[0].clientY;
  }

  onTouchEnd(event: TouchEvent) {
    if (!event.changedTouches.length) return;
    const touchEndX = event.changedTouches[0].clientX;
    const touchEndY = event.changedTouches[0].clientY;
    const deltaX = touchEndX - this.touchStartX;
    const deltaY = touchEndY - this.touchStartY;
    if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > 50) {
      if (deltaX > 0) this.previousPhoto();
      else this.nextPhoto();
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

  toggleDownloadDropdown() {
    this.downloadDropdownOpen = !this.downloadDropdownOpen;
  }

  onDownloadAll(small: boolean) {
    this.downloadDropdownOpen = false;
    this.downloadAll.emit(small);
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
  }
}
