import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PhotoService, FilmService, AccountService } from '../../../services';
import { PhotoDto, FilmDto } from '../../../DTOs';

@Component({
  selector: 'app-film-photos',
  templateUrl: './film-photos.component.html',
  styleUrl: './film-photos.component.css',
  standalone: false,
})
export class FilmPhotosComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private filmService = inject(FilmService);
  private photoService = inject(PhotoService);
  private accountService = inject(AccountService);

  filmId: string = '';
  film: FilmDto | null = null;
  photos: PhotoDto[] = [];
  loading = true;
  errorMessage: string | null = null;
  currentUsername: string = '';
  isOwner = false;
  uploadLoading = false;
  uploadProgress: { current: number; total: number } = { current: 0, total: 0 };
  downloadAllLoading = false;

  ngOnInit() {
    this.filmId = this.route.snapshot.paramMap.get('id') || '';
    if (this.filmId) {
      this.accountService.whoAmI().subscribe({
        next: (identity) => {
          this.currentUsername = identity.username;
          this.loadFilmAndPhotos();
        },
        error: () => this.loadFilmAndPhotos(),
      });
    } else {
      this.router.navigate(['/films']);
    }
  }

  private loadFilmAndPhotos() {
    this.loading = true;
    this.errorMessage = null;
    Promise.all([
      this.filmService.getById(this.filmId).toPromise(),
      this.photoService.getPhotosByFilmId(this.filmId).toPromise(),
    ])
      .then(([film, photos]) => {
        this.film = film || null;
        this.photos = photos || [];
        this.photos.sort((a, b) => a.index - b.index);
        this.isOwner = !!(this.film && this.currentUsername && this.film.purchasedBy === this.currentUsername);
        this.loading = false;
      })
      .catch(() => {
        this.errorMessage = 'Error loading film photos.';
        this.loading = false;
      });
  }

  onDownloadPhoto(photo: PhotoDto) {
    this.photoService.downloadPhoto(photo.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        const displayName = (this.film?.name?.trim() || this.film?.brand) || 'photo';
        link.download = `${photo.index.toString().padStart(3, '0')}-${this.sanitizeFileName(displayName)}.jpg`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.errorMessage = 'Error downloading photo.';
      },
    });
  }

  onDownloadAllPhotos(small: boolean) {
    this.downloadAllLoading = true;
    this.photoService.downloadAllPhotos(this.filmId, small).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        const hasName = this.film?.name?.trim();
        const brand = this.film?.brand ? this.sanitizeNameForFileName(this.film.brand) : '';
        const titlePart = hasName
          ? `${this.sanitizeNameForFileName(this.film!.name!.trim())} - ${brand}`
          : brand;
        const isoPart = this.film?.iso ? ` - ISO ${this.sanitizeFileName(this.film.iso)}` : '';
        const formattedDate = this.film?.formattedExposureDate
          ? this.sanitizeDateForFileName(this.film.formattedExposureDate)
          : '';
        const datePart = formattedDate ? ` - ${formattedDate}` : '';
        const sizeSuffix = small ? '-small' : '';
        const baseName = [titlePart || 'photos', isoPart, datePart].filter(Boolean).join('');
        link.download = `${baseName}${sizeSuffix}.zip`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        this.downloadAllLoading = false;
      },
      error: () => {
        this.errorMessage = 'Error downloading photos archive.';
        this.downloadAllLoading = false;
      },
    });
  }

  onDeletePhoto(photo: PhotoDto) {
    this.photoService.deletePhoto(photo.id).subscribe({
      next: () => {
        this.photos = this.photos.filter((p) => p.id !== photo.id);
      },
      error: () => {
        this.errorMessage = 'Error deleting photo.';
      },
    });
  }

  onRestrictToggle(photo: PhotoDto) {
    const newRestricted = !photo.restricted;
    this.photoService.setRestricted(photo.id, newRestricted).subscribe({
      next: (updated) => {
        const idx = this.photos.findIndex((p) => p.id === updated.id);
        if (idx >= 0) this.photos[idx].restricted = updated.restricted;
      },
      error: () => {
        this.errorMessage = 'Error updating photo access.';
      },
    });
  }

  onUploadPhotos() {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    fileInput.multiple = true;
    fileInput.accept = 'image/*';
    fileInput.onchange = (event: Event) => {
      const files = (event.target as HTMLInputElement).files;
      if (files && files.length > 0) this.processPhotoUploads(files);
    };
    fileInput.click();
  }

  private async processPhotoUploads(files: FileList) {
    this.uploadLoading = true;
    this.errorMessage = null;
    this.uploadProgress = { current: 0, total: files.length };
    try {
      const results = await this.photoService.uploadMultiplePhotos(
        this.filmId,
        files,
        this.photos,
        (uploadedPhoto) => {
          this.uploadProgress.current++;
          if (uploadedPhoto) {
            const existingIndex = this.photos.findIndex((p) => p.id === uploadedPhoto.id);
            if (existingIndex === -1) {
              this.photos.push(uploadedPhoto);
              this.photos.sort((a, b) => a.index - b.index);
            }
          }
        }
      );
      const successCount = results.filter((r) => r.success).length;
      const failureCount = results.filter((r) => !r.success).length;
      this.uploadLoading = false;
      if (failureCount > 0) {
        this.errorMessage = `${successCount} photo(s) uploaded successfully, ${failureCount} failed.`;
      }
      this.filmService.getById(this.filmId).subscribe({
        next: (film) => (this.film = film || null),
        error: () => {},
      });
    } catch (err: unknown) {
      this.uploadLoading = false;
      this.errorMessage = 'There was an error uploading photos.';
    }
  }

  private sanitizeFileName(fileName: string): string {
    const sanitized = fileName.replace(/[^a-zA-Z0-9.\-_]/g, '');
    return (sanitized || 'photos').substring(0, 50);
  }

  private sanitizeDateForFileName(dateString: string): string {
    return dateString.replace(/[<>:"/\\|?*]/g, '').substring(0, 50);
  }

  private sanitizeNameForFileName(name: string): string {
    return name.replace(/[<>:"/\\|?*]/g, '').substring(0, 50);
  }
}
