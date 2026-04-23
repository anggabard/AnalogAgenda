import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PhotoDto } from '../../../DTOs';
import { toPhotosPreviewDisplayUrl } from '../../../helpers/photo-url.helper';
import { PhotoService } from '../../../services';

@Component({
  selector: 'app-photo-of-the-day-section',
  templateUrl: './photo-of-the-day-section.component.html',
  styleUrl: './photo-of-the-day-section.component.css',
  standalone: false,
})
export class PhotoOfTheDaySectionComponent implements OnInit {
  private photoService = inject(PhotoService);
  private router = inject(Router);

  loading = true;
  loadError: string | null = null;
  imageLoadError: string | null = null;
  /** Set when API returns 404 (no eligible photos). */
  noPhotoAvailable = false;
  photo: PhotoDto | null = null;

  ngOnInit(): void {
    this.photoService.getPhotoOfTheDay().subscribe({
      next: (dto) => {
        this.loading = false;
        if (!dto) {
          this.noPhotoAvailable = true;
          return;
        }
        this.photo = dto;
        this.imageLoadError = null;
      },
      error: () => {
        this.loading = false;
        this.loadError = 'Could not load Photo of the Day.';
      },
    });
  }

  /** Same cache-busting preview URL pattern as film photos grid. */
  previewImageUrl(photo: PhotoDto): string {
    return toPhotosPreviewDisplayUrl(photo.imageUrl, photo.updatedDate);
  }

  onPreviewImageError(): void {
    this.imageLoadError = 'Could not load the photo image.';
  }

  viewFilm(): void {
    if (this.photo?.filmId) {
      this.router.navigate(['/films', this.photo.filmId]);
    }
  }
}
