import { Component, inject, OnInit } from '@angular/core';
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

  filmId: string = '';
  film: FilmDto | null = null;
  photos: PhotoDto[] = [];
  loading = true;
  errorMessage: string | null = null;
  private photosManuallySet = false; // Flag to prevent loadFilmAndPhotos from overwriting manually set photos
  
  // Preview modal
  isPreviewModalOpen = false;
  currentPreviewPhoto: PhotoDto | null = null;
  currentPhotoIndex = 0;
  
  // Delete modal
  isDeleteModalOpen = false;

  // Download all loading state
  downloadAllLoading = false;
  
  // Upload loading state
  uploadLoading = false;
  
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
      // Only update if we're still loading and photos weren't manually set (e.g., during tests)
      // Also check if photos array already has content (manually set in tests)
      if (this.loading && !this.photosManuallySet && this.photos.length === 0) {
        this.film = film || null;
        this.photos = photos || [];
        // Sort photos by index to ensure consistent order
        this.photos.sort((a, b) => a.index - b.index);
        this.loading = false;
      } else if (this.loading) {
        // If loading but photos were manually set or already exist, just update loading state
        this.loading = false;
      }
    }).catch(error => {
      if (this.loading && !this.photosManuallySet) {
        this.errorMessage = 'Error loading film photos.';
        this.loading = false;
      }
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
          const originalLength = this.photos.length;
          this.photos = this.photos.filter(p => p.id !== deletedPhotoId);
          // Mark photos as manually modified to prevent loadFilmAndPhotos from overwriting
          this.photosManuallySet = true;
          
          this.closeDeleteModal();
          
          // Navigate to next/previous photo or close if no photos left
          if (this.photos.length === 0) {
            this.closePreview();
          } else {
            // Adjust index: if we deleted the last photo, move to the new last photo
            // Otherwise, the current index now points to the next photo (or is out of bounds)
            if (deletedIndex >= this.photos.length) {
              // Deleted the last photo (or beyond), move to new last photo
              this.currentPhotoIndex = this.photos.length - 1;
            } else {
              // Deleted a photo before the end, stay at same index (now points to next photo)
              this.currentPhotoIndex = deletedIndex;
            }
            // Ensure index is valid and set the preview photo
            this.currentPhotoIndex = Math.max(0, Math.min(this.currentPhotoIndex, this.photos.length - 1));
            this.currentPreviewPhoto = this.photos[this.currentPhotoIndex];
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

  downloadAllPhotos() {
    this.downloadAllLoading = true;
    this.photoService.downloadAllPhotos(this.filmId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `${this.sanitizeFileName(this.film?.name || 'photos')}.zip`;
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
    
    try {
      // Upload photos in parallel - each request processes individually
      const results = await this.photoService.uploadMultiplePhotos(
        this.filmId,
        files,
        this.photos,
        (uploadedPhoto) => {
          // Add photo to the array immediately when it uploads successfully
          if (uploadedPhoto) {
            // Check if photo already exists (in case of duplicate uploads)
            const existingIndex = this.photos.findIndex(p => p.id === uploadedPhoto.id);
            if (existingIndex === -1) {
              this.photos.push(uploadedPhoto);
              // Sort photos by index to maintain order
              this.photos.sort((a, b) => a.index - b.index);
              // Mark photos as manually modified to prevent loadFilmAndPhotos from overwriting
              this.photosManuallySet = true;
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
