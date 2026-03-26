import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AccountService, FilmService, IdeaService } from '../../../services';
import { FilmDto, IdeaDto, IdeaSessionSummaryDto, PhotoDto } from '../../../DTOs';
import { PhotosContentComponent } from '../../films/photos-content/photos-content.component';

@Component({
  selector: 'app-idea-results',
  templateUrl: './idea-results.component.html',
  styleUrl: './idea-results.component.css',
  standalone: false,
})
export class IdeaResultsComponent implements OnInit {
  @ViewChild(PhotosContentComponent) photosContent?: PhotosContentComponent;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private ideaService = inject(IdeaService);
  private filmService = inject(FilmService);
  private accountService = inject(AccountService);

  ideaId = '';
  idea: IdeaDto | null = null;
  photos: PhotoDto[] = [];
  allowedBulkPhotoIds: string[] = [];
  loading = true;
  errorMessage: string | null = null;
  isRemoveConfirmOpen = false;
  photosPendingRemove: PhotoDto[] = [];

  ngOnInit(): void {
    this.ideaId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.ideaId) {
      this.router.navigate(['/home']);
      return;
    }
    this.load();
  }

  private load(): void {
    this.loading = true;
    this.errorMessage = null;
    forkJoin({
      idea: this.ideaService.getById(this.ideaId).pipe(catchError(() => of(null))),
      photos: this.ideaService.getPhotosForIdea(this.ideaId).pipe(catchError(() => of([] as PhotoDto[]))),
      who: this.accountService.whoAmI().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ idea, photos, who }) => {
        if (!idea) {
          this.errorMessage = 'Idea not found.';
          this.loading = false;
          return;
        }
        this.idea = idea;
        this.photos = photos ?? [];
        const username = who?.username ?? '';
        const filmIds = [...new Set(this.photos.map((p) => p.filmId))];
        if (filmIds.length === 0 || !username) {
          this.allowedBulkPhotoIds = [];
          this.loading = false;
          return;
        }
        forkJoin(
          filmIds.map((fid) => this.filmService.getById(fid).pipe(catchError(() => of(null))))
        ).subscribe({
          next: (films) => {
            const filmById = new Map<string, FilmDto>();
            films.forEach((f, i) => {
              if (f) {
                filmById.set(filmIds[i], f);
              }
            });
            this.allowedBulkPhotoIds = this.photos
              .filter((p) => filmById.get(p.filmId)?.purchasedBy === username)
              .map((p) => p.id);
            this.loading = false;
          },
          error: () => {
            this.allowedBulkPhotoIds = [];
            this.loading = false;
          },
        });
      },
      error: () => {
        this.errorMessage = 'Error loading idea.';
        this.loading = false;
      },
    });
  }

  onRemoveLinkedRequest(photos: PhotoDto[]): void {
    this.photosPendingRemove = photos;
    this.isRemoveConfirmOpen = true;
  }

  closeRemoveConfirm(): void {
    this.isRemoveConfirmOpen = false;
    this.photosPendingRemove = [];
  }

  get removeConfirmMessage(): string {
    const n = this.photosPendingRemove.length;
    return `Remove ${n} photo${n === 1 ? '' : 's'} from this idea? The photos will stay on their films.`;
  }

  confirmRemoveLinked(): void {
    const toRemove = [...this.photosPendingRemove];
    if (toRemove.length === 0) {
      this.closeRemoveConfirm();
      return;
    }
    const idSet = new Set(toRemove.map((p) => p.id));
    const requests = toRemove.map((p) =>
      this.ideaService.removePhotoFromIdea(this.ideaId, p.id).pipe(catchError(() => of(null)))
    );
    forkJoin(requests).subscribe({
      next: () => {
        this.photos = this.photos.filter((p) => !idSet.has(p.id));
        this.closeRemoveConfirm();
        this.photosContent?.exitBulkSelectionMode();
        this.allowedBulkPhotoIds = this.allowedBulkPhotoIds.filter((id) => !idSet.has(id));
      },
      error: () => {
        this.errorMessage = 'Error removing photos from idea.';
        this.closeRemoveConfirm();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/home']);
  }

  ideaTriedSessionsIntro(sessionCount: number): string {
    return sessionCount > 0 ? 'Tried in ' : '';
  }

  /** Full label for the link; [routerLink] uses session id — Index is display-only in the label text. */
  sessionLinkText(s: IdeaSessionSummaryDto): string {
    const t = s.displayLabel?.trim();
    return t || 'Session';
  }
}
