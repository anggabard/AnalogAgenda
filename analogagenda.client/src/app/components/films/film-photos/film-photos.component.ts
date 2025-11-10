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
  uploadProgress = 0;
  uploadTotal = 0;
  
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
      this.loading = false;
    }).catch(error => {
      this.errorMessage = 'Error loading film photos.';
      this.loading = false;
    });
  }

  openPreview(photo: PhotoDto) {
    this.currentPreviewPhoto = photo;
    this.currentPhotoIndex = this.photos.findIndex(p => p.id === photo.id);
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
          this.photos = this.photos.filter(p => p.id !== deletedPhotoId);
          
          this.closeDeleteModal();
          
          // Navigate to next/previous photo or close if no photos left
          if (this.photos.length === 0) {
            this.closePreview();
          } else {
            // Adjust index if we deleted the last photo
            if (this.currentPhotoIndex >= this.photos.length) {
              this.currentPhotoIndex = this.photos.length - 1;
            }
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
    this.uploadProgress = 0;
    this.uploadTotal = files.length;
    
    try {
      await this.photoService.uploadMultiplePhotos(
        this.filmId,
        files,
        this.photos,
        (current, total) => {
          this.uploadProgress = current;
          this.uploadTotal = total;
        }
      );

      // All uploads successful
      this.uploadLoading = false;
      this.uploadProgress = 0;
      this.uploadTotal = 0;
      this.loadFilmAndPhotos();
    } catch (err) {
      this.uploadLoading = false;
      this.uploadProgress = 0;
      this.uploadTotal = 0;
      this.errorMessage = 'There was an error uploading the photos.';
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
