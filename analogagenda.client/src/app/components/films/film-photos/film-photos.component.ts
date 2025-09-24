import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PhotoService, FilmService } from '../../../services';
import { PhotoDto, FilmDto } from '../../../DTOs';

@Component({
  selector: 'app-film-photos',
  templateUrl: './film-photos.component.html',
  styleUrl: './film-photos.component.css'
})
export class FilmPhotosComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private photoService = inject(PhotoService);
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
      this.filmService.getFilm(this.filmId).toPromise(),
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
    this.currentPhotoIndex = this.photos.findIndex(p => p.rowKey === photo.rowKey);
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
      this.photoService.deletePhoto(this.currentPreviewPhoto.rowKey).subscribe({
        next: () => {
          // Remove photo from local array
          const deletedPhotoRowKey = this.currentPreviewPhoto!.rowKey;
          this.photos = this.photos.filter(p => p.rowKey !== deletedPhotoRowKey);
          
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
    this.photoService.downloadPhoto(photo.rowKey).subscribe({
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
      },
      error: (err) => {
        this.errorMessage = 'Error downloading photos archive.';
      }
    });
  }


  private sanitizeFileName(fileName: string): string {
    const sanitized = fileName.replace(/[^a-zA-Z0-9.\-_]/g, '');
    return (sanitized || 'photos').substring(0, 50);
  }
}
