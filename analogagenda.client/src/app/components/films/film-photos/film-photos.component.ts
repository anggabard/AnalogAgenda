import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PhotoService, FilmService, AccountService, IdeaService } from '../../../services';
import { PhotoDto, FilmDto, IdeaDto } from '../../../DTOs';
import { DownloadHelper } from '../../../helpers/download.helper';
import { modalListMatches } from '../../../helpers/modal-list-search.helper';
import { PhotosContentComponent } from '../photos-content/photos-content.component';

@Component({
  selector: 'app-film-photos',
  templateUrl: './film-photos.component.html',
  styleUrl: './film-photos.component.css',
  standalone: false,
})
export class FilmPhotosComponent implements OnInit {
  @ViewChild(PhotosContentComponent) photosContent?: PhotosContentComponent;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private filmService = inject(FilmService);
  private photoService = inject(PhotoService);
  private accountService = inject(AccountService);
  private ideaService = inject(IdeaService);

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

  isBulkDeleteModalOpen = false;
  photosPendingBulkDelete: PhotoDto[] = [];
  isWackyModalOpen = false;
  wackyModalSearch = '';
  ideasForPicker: IdeaDto[] = [];
  photosForWacky: PhotoDto[] = [];

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
        const displayName = (this.film?.name?.trim() || this.film?.brand) || 'photo';
        const filename = `${photo.index.toString().padStart(3, '0')}-${DownloadHelper.sanitizeForFileName(displayName)}.jpg`;
        DownloadHelper.triggerBlobDownload(blob, filename);
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
        DownloadHelper.triggerBlobDownload(blob, this.zipDownloadFileName(small, false));
        this.downloadAllLoading = false;
      },
      error: () => {
        this.errorMessage = 'Error downloading photos archive.';
        this.downloadAllLoading = false;
      },
    });
  }

  onDownloadSelectedPhotos(payload: { small: boolean; photos: PhotoDto[] }) {
    if (payload.photos.length === 0) {
      return;
    }
    this.downloadAllLoading = true;
    const ids = payload.photos.map((p) => p.id);
    this.photoService.downloadSelectedPhotos(this.filmId, ids, payload.small).subscribe({
      next: (blob) => {
        DownloadHelper.triggerBlobDownload(blob, this.zipDownloadFileName(payload.small, true));
        this.downloadAllLoading = false;
      },
      error: () => {
        this.errorMessage = 'Error downloading selected photos archive.';
        this.downloadAllLoading = false;
      },
    });
  }

  private zipDownloadFileName(small: boolean, selected: boolean): string {
    const hasName = this.film?.name?.trim();
    const brand = this.film?.brand ? DownloadHelper.sanitizePathUnsafeChars(this.film.brand) : '';
    const titlePart = hasName
      ? `${DownloadHelper.sanitizePathUnsafeChars(this.film!.name!.trim())} - ${brand}`
      : brand;
    const isoPart = this.film?.iso ? ` - ISO ${DownloadHelper.sanitizeForFileName(this.film.iso)}` : '';
    const formattedDate = this.film?.formattedExposureDate
      ? DownloadHelper.sanitizePathUnsafeChars(this.film.formattedExposureDate)
      : '';
    const datePart = formattedDate ? ` - ${formattedDate}` : '';
    const sizeSuffix = small ? '-small' : '';
    const baseName = [titlePart || 'photos', isoPart, datePart].filter(Boolean).join('');
    const selectedPart = selected ? '-selected' : '';
    return `${baseName}${selectedPart}${sizeSuffix}.zip`;
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

  onBulkDeleteRequest(photos: PhotoDto[]): void {
    this.photosPendingBulkDelete = photos;
    this.isBulkDeleteModalOpen = true;
  }

  closeBulkDeleteModal(): void {
    this.isBulkDeleteModalOpen = false;
    this.photosPendingBulkDelete = [];
  }

  confirmBulkDelete(): void {
    const toDelete = [...this.photosPendingBulkDelete];
    if (toDelete.length === 0) {
      this.closeBulkDeleteModal();
      return;
    }
    const idSet = new Set(toDelete.map((p) => p.id));
    const requests = toDelete.map((p) =>
      this.photoService.deletePhoto(p.id).pipe(catchError(() => of(null)))
    );
    forkJoin(requests).subscribe({
      next: () => {
        this.photos = this.photos.filter((p) => !idSet.has(p.id));
        this.closeBulkDeleteModal();
        this.photosContent?.exitBulkSelectionMode();
      },
      error: () => {
        this.errorMessage = 'Error deleting photos.';
        this.closeBulkDeleteModal();
      },
    });
  }

  get bulkDeleteMessage(): string {
    const n = this.photosPendingBulkDelete.length;
    return `Delete ${n} selected photo${n === 1 ? '' : 's'}? This cannot be undone.`;
  }

  onWackyResultRequest(photos: PhotoDto[]): void {
    this.photosForWacky = photos;
    this.wackyModalSearch = '';
    this.ideaService.getAll().subscribe({
      next: (ideas) => {
        this.ideasForPicker = ideas;
        this.isWackyModalOpen = true;
      },
      error: () => {
        this.errorMessage = 'Error loading ideas.';
      },
    });
  }

  closeWackyModal(): void {
    this.isWackyModalOpen = false;
    this.ideasForPicker = [];
    this.photosForWacky = [];
    this.wackyModalSearch = '';
  }

  get ideasForWackyModal(): IdeaDto[] {
    return this.ideasForPicker.filter((idea) =>
      modalListMatches(this.wackyModalSearch, idea.title, idea.description)
    );
  }

  attachPhotosToIdea(idea: IdeaDto): void {
    const ids = this.photosForWacky.map((p) => p.id);
    if (ids.length === 0) return;
    this.ideaService.addPhotosToIdea(idea.id, ids).subscribe({
      next: () => {
        this.closeWackyModal();
        this.photosContent?.exitBulkSelectionMode();
      },
      error: () => {
        this.errorMessage = 'Error attaching photos to idea.';
      },
    });
  }
}
