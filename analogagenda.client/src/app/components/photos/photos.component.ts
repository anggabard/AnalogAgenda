import { Component, inject, OnInit } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { FilmService, PhotoService, AccountService } from '../../services';
import { FilmDto, PhotoDto, IdentityDto } from '../../DTOs';
import { DownloadHelper } from '../../helpers/download.helper';

interface FilmWithPhotos {
  film: FilmDto;
  photos: PhotoDto[];
}

@Component({
  selector: 'app-photos',
  templateUrl: './photos.component.html',
  styleUrl: './photos.component.css',
  standalone: false,
})
export class PhotosComponent implements OnInit {
  private filmService = inject(FilmService);
  private photoService = inject(PhotoService);
  private accountService = inject(AccountService);

  activeTab: 'my' | 'all' = 'my';
  currentUsername = '';
  /** Full list of films with photos (from API, no paging). */
  private allFilms: FilmDto[] = [];
  /** Sections revealed so far; photos loaded in batches as user scrolls. */
  filmSections: FilmWithPhotos[] = [];
  /** How many film sections we have revealed (with photos loaded). */
  private revealedCount = 0;
  private readonly batchSize = 10;
  /** Loading the full film list from API. */
  loadingFilms = false;
  /** Loading photos for the next batch of sections. */
  loadingPhotos = false;
  /** User-visible error message (batch load, download, restrict, delete). */
  errorMessage: string | null = null;

  ngOnInit() {
    this.accountService.whoAmI().subscribe({
      next: (identity: IdentityDto) => {
        this.currentUsername = identity.username;
        this.loadInitial();
      },
      error: () => this.loadInitial(),
    });
  }

  private loadInitial() {
    this.filmSections = [];
    this.allFilms = [];
    this.revealedCount = 0;
    this.loadingFilms = true;
    this.loadingPhotos = false;
    this.errorMessage = null;
    const request$ =
      this.activeTab === 'my'
        ? this.filmService.getMyDevelopedFilmsAll()
        : this.filmService.getDevelopedFilmsAll();
    request$.subscribe({
      next: (films: FilmDto[]) => {
        this.allFilms = films.filter((f) => (f.photoCount ?? 0) > 0);
        this.loadingFilms = false;
        this.revealNextBatch();
      },
      error: () => {
        this.loadingFilms = false;
      },
    });
  }

  setActiveTab(tab: 'my' | 'all') {
    if (this.activeTab === tab) return;
    this.activeTab = tab;
    this.loadInitial();
  }

  isOwner(film: FilmDto): boolean {
    return !!(this.currentUsername && film.purchasedBy === this.currentUsername);
  }

  /** Load photos for the next batch of films and append to filmSections. On failure, leave revealedCount unchanged so user can retry. */
  private revealNextBatch() {
    if (this.loadingPhotos || this.revealedCount >= this.allFilms.length) return;
    const batch = this.allFilms.slice(this.revealedCount, this.revealedCount + this.batchSize);
    if (batch.length === 0) return;
    this.loadingPhotos = true;
    this.errorMessage = null;
    const batchStart = this.revealedCount;
    const promises = batch.map((film) =>
      lastValueFrom(this.photoService.getPhotosByFilmId(film.id)).then((photos) => ({ film, photos: photos || [] }))
    );
    Promise.all(promises)
      .then((results) => {
        results.forEach((r) => this.filmSections.push({ film: r.film, photos: r.photos }));
        this.revealedCount = batchStart + batch.length;
        this.loadingPhotos = false;
      })
      .catch((err) => {
        console.error('Error loading photos for batch:', err);
        this.loadingPhotos = false;
        this.errorMessage = 'Failed to load some photos. You can try "Load more" again.';
      });
  }

  onLoadMore() {
    this.revealNextBatch();
  }

  get loading(): boolean {
    return this.loadingFilms || this.loadingPhotos;
  }

  get hasMore(): boolean {
    return this.revealedCount < this.allFilms.length;
  }

  downloadAllLoadingSectionId: string | null = null;

  onDownloadPhoto(section: FilmWithPhotos, photo: PhotoDto) {
    this.errorMessage = null;
    this.photoService.downloadPhoto(photo.id).subscribe({
      next: (blob) => {
        const displayName = (section.film.name?.trim() || section.film.brand) || 'photo';
        const filename = `${photo.index.toString().padStart(3, '0')}-${DownloadHelper.sanitizeForFileName(displayName)}.jpg`;
        DownloadHelper.triggerBlobDownload(blob, filename);
      },
      error: () => {
        this.errorMessage = 'Failed to download photo.';
      },
    });
  }

  onDownloadAll(section: FilmWithPhotos, small: boolean) {
    this.errorMessage = null;
    this.downloadAllLoadingSectionId = section.film.id;
    this.photoService.downloadAllPhotos(section.film.id, small).subscribe({
      next: (blob) => {
        const name = section.film.name?.trim();
        const brand = section.film.brand ? DownloadHelper.sanitizePathUnsafeChars(section.film.brand) : '';
        const titlePart = name ? `${DownloadHelper.sanitizePathUnsafeChars(name)} - ${brand}` : brand;
        const isoPart = section.film.iso ? ` - ISO ${DownloadHelper.sanitizeForFileName(section.film.iso)}` : '';
        const datePart = section.film.formattedExposureDate
          ? ` - ${DownloadHelper.sanitizePathUnsafeChars(section.film.formattedExposureDate)}`
          : '';
        const sizeSuffix = small ? '-small' : '';
        const baseName = [titlePart || 'photos', isoPart, datePart].filter(Boolean).join('');
        DownloadHelper.triggerBlobDownload(blob, `${baseName}${sizeSuffix}.zip`);
        this.downloadAllLoadingSectionId = null;
      },
      error: () => {
        this.downloadAllLoadingSectionId = null;
        this.errorMessage = 'Failed to download photos archive.';
      },
    });
  }

  onRestrictToggle(section: FilmWithPhotos, photo: PhotoDto) {
    this.errorMessage = null;
    const newRestricted = !photo.restricted;
    this.photoService.setRestricted(photo.id, newRestricted).subscribe({
      next: (updated) => {
        const idx = section.photos.findIndex((p) => p.id === updated.id);
        if (idx >= 0) section.photos[idx].restricted = updated.restricted;
      },
      error: () => {
        this.errorMessage = 'Failed to update photo access.';
      },
    });
  }

  onDeletePhoto(section: FilmWithPhotos, photo: PhotoDto) {
    this.errorMessage = null;
    this.photoService.deletePhoto(photo.id).subscribe({
      next: () => {
        section.photos = section.photos.filter((p) => p.id !== photo.id);
      },
      error: () => {
        this.errorMessage = 'Failed to delete photo.';
      },
    });
  }
}
