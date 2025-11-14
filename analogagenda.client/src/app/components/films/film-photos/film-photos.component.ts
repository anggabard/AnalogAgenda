import { Component, inject, OnInit, HostListener, ElementRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PhotoService, FilmService } from '../../../services';
import { PhotoDto, FilmDto } from '../../../DTOs';

@Component({
    selector: 'app-film-photos',
    templateUrl: './film-photos.component.html',
    styleUrl: './film-photos.component.css',
    standalone: false
})
export class FilmPhotosComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  public photoService = inject(PhotoService);
  private filmService = inject(FilmService);
  private elementRef = inject(ElementRef);

  filmId: string = '';
  film: FilmDto | null = null;
  photos: PhotoDto[] = [];
  loading = true;
  errorMessage: string | null = null;
  
  // Preview modal
  isPreviewModalOpen = false;
  currentPreviewPhoto: PhotoDto | null = null;
  currentPhotoIndex = 0;
  
  // Delete modal
  isDeleteModalOpen = false;

  // Download all loading state
  downloadAllLoading = false;
  downloadDropdownOpen = false;
  
  // Upload loading state
  uploadLoading = false;
  uploadProgress: { current: number; total: number } = { current: 0, total: 0 };
  
  // Touch handling
  private touchStartX = 0;
  private touchStartY = 0;

  ngOnInit() {
    this.filmId = this.route.snapshot.paramMap.get('id') || '';
    if (this.filmId) {
      this.loadFilmAndPhotos();
    } else {
      this.router.navigate(['/films']);
    }
  }

  private loadFilmAndPhotos() {
    this.loading = true;
    this.errorMessage = null;

    // Load film details and photos in parallel
    Promise.all([
      this.filmService.getById(this.filmId).toPromise(),
      this.photoService.getPhotosByFilmId(this.filmId).toPromise()
    ]).then(([film, photos]) => {
      this.film = film || null;
      this.photos = photos || [];
      // Sort photos by index to ensure consistent order
      this.photos.sort((a, b) => a.index - b.index);
      this.loading = false;
    }).catch(error => {
      this.errorMessage = 'Error loading film photos.';
      this.loading = false;
    });
  }

  openPreview(photo: PhotoDto) {
    this.currentPreviewPhoto = photo;
    const foundIndex = this.photos.findIndex(p => p.id === photo.id);
    // Ensure index is valid, default to 0 if not found
    this.currentPhotoIndex = foundIndex >= 0 ? foundIndex : 0;
    this.isPreviewModalOpen = true;
    
    // Focus the preview overlay to enable keyboard events
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
    if (this.canNavigatePrevious()) {
      this.currentPhotoIndex--;
      this.currentPreviewPhoto = this.photos[this.currentPhotoIndex];
    }
  }

  nextPhoto() {
    if (this.canNavigateNext()) {
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

  confirmDelete() {
    if (this.currentPreviewPhoto) {
      this.photoService.deletePhoto(this.currentPreviewPhoto.id).subscribe({
        next: () => {
          // Remove photo from local array
          const deletedPhotoId = this.currentPreviewPhoto!.id;
          const deletedIndex = this.currentPhotoIndex;
          this.photos = this.photos.filter(p => p.id !== deletedPhotoId);
          
          this.closeDeleteModal();
          
          // Navigate to next/previous photo or close if no photos left
          if (this.photos.length === 0) {
            this.closePreview();
          } else {
            // Adjust index: if we deleted the last photo, move to the new last photo
            // Otherwise, stay at the same position (which now points to the next photo)
            if (deletedIndex >= this.photos.length) {
              this.currentPhotoIndex = this.photos.length - 1;
            }
            // Ensure we have a valid photo at the current index
            if (this.currentPhotoIndex >= 0 && this.currentPhotoIndex < this.photos.length) {
              this.currentPreviewPhoto = this.photos[this.currentPhotoIndex];
            } else {
              // Fallback: set to last photo if index is invalid
              this.currentPhotoIndex = this.photos.length - 1;
              this.currentPreviewPhoto = this.photos[this.currentPhotoIndex];
            }
          }
        },
        error: (err) => {
          this.errorMessage = 'Error deleting photo.';
          this.closeDeleteModal();
        }
      });
    }
  }

  downloadPhoto(photo: PhotoDto) {
    this.photoService.downloadPhoto(photo.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${photo.index.toString().padStart(3, '0')}-${this.sanitizeFileName(this.film?.name || 'photo')}.jpg`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.errorMessage = 'Error downloading photo.';
      }
    });
  }

  downloadAllPhotos(small: boolean = false) {
    this.downloadAllLoading = true;
    this.downloadDropdownOpen = false;
    this.photoService.downloadAllPhotos(this.filmId, small).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        const formattedDate = this.film?.formattedExposureDate 
          ? ` - ${this.sanitizeFileName(this.film.formattedExposureDate)}` 
          : '';
        const sizeSuffix = small ? '-small' : '';
        link.download = `${this.sanitizeFileName(this.film?.name || 'photos')}${formattedDate}${sizeSuffix}.zip`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        this.downloadAllLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error downloading photos archive.';
        this.downloadAllLoading = false;
      }
    });
  }

  toggleDownloadDropdown() {
    this.downloadDropdownOpen = !this.downloadDropdownOpen;
  }

  closeDownloadDropdown() {
    this.downloadDropdownOpen = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdownContainer = this.elementRef.nativeElement.querySelector('.download-dropdown-container');
    
    if (dropdownContainer && !dropdownContainer.contains(target)) {
      this.downloadDropdownOpen = false;
    }
  }

  onUploadPhotos() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.multiple = true;
    fileInput.accept = 'image/*';
    
    fileInput.onchange = (event: any) => {
      const files = event.target.files;
      if (files && files.length > 0) {
        this.processPhotoUploads(files);
      }
    };
    
    fileInput.click();
  }

  private async processPhotoUploads(files: FileList) {
    this.uploadLoading = true;
    this.errorMessage = null;
    
    // Initialize upload progress counter
    this.uploadProgress = { current: 0, total: files.length };
    
    try {
      // Upload photos sequentially - one at a time
      const results = await this.photoService.uploadMultiplePhotos(
        this.filmId,
        files,
        this.photos,
        (uploadedPhoto) => {
          // Update progress counter
          this.uploadProgress.current++;
          
          // Add photo to the array immediately when it uploads successfully
          if (uploadedPhoto) {
            // Check if photo already exists (in case of duplicate uploads)
            const existingIndex = this.photos.findIndex(p => p.id === uploadedPhoto.id);
            if (existingIndex === -1) {
              this.photos.push(uploadedPhoto);
              // Sort photos by index to maintain order
              this.photos.sort((a, b) => a.index - b.index);
            }
          }
        }
      );

      // Count successes and failures
      const successCount = results.filter(r => r.success).length;
      const failureCount = results.filter(r => !r.success).length;

      this.uploadLoading = false;

      if (failureCount > 0) {
        this.errorMessage = `${successCount} photo(s) uploaded successfully, ${failureCount} failed.`;
      }

      // Reload film to ensure "developed" status is updated
      this.filmService.getById(this.filmId).subscribe({
        next: (film) => {
          this.film = film || null;
        },
        error: () => {
          // Ignore errors when reloading film
        }
      });
      
    } catch (err: any) {
      this.uploadLoading = false;
      this.errorMessage = 'There was an error uploading photos: ' + (err?.message || 'Unknown error');
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
    
    // Only handle horizontal swipes (not vertical scrolling)
    if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > 50) {
      if (deltaX > 0) {
        // Swipe right - go to previous photo
        this.previousPhoto();
      } else {
        // Swipe left - go to next photo
        this.nextPhoto();
      }
    }
  }

  private sanitizeFileName(fileName: string): string {
    const sanitized = fileName.replace(/[^a-zA-Z0-9.\-_]/g, '');
    return (sanitized || 'photos').substring(0, 50);
  }
}
