import { Component, HostListener, OnDestroy, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CollectionService, UserSettingsService } from '../../services';
import { CollectionDto, PagedResponseDto } from '../../DTOs';
import { ErrorHandlingHelper } from '../../helpers/error-handling.helper';
import { openRouteInNewTab } from '../../helpers/navigation.helper';

@Component({
  selector: 'app-collections',
  templateUrl: './collections.component.html',
  styleUrl: './collections.component.css',
  standalone: false,
})
export class CollectionsComponent implements OnInit, OnDestroy {
  private collectionService = inject(CollectionService);
  private userSettingsService = inject(UserSettingsService);
  private router = inject(Router);

  @ViewChild('collectionCardTemplate') collectionCardTemplate!: TemplateRef<any>;
  @ViewChild('collectionRowTemplate') collectionRowTemplate!: TemplateRef<any>;

  collectionTableHeaders = ['Name', 'Date From', 'Date To', 'Location', 'Finished', 'Preview'];

  collections: CollectionDto[] = [];
  loading = true;
  errorMessage: string | null = null;
  hasMore = false;

  private page = 1;
  pageSize = 20;

  private scrollLoadDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly scrollDebounceMs = 150;

  ngOnInit(): void {
    this.userSettingsService.getUserSettings().subscribe({
      next: (settings) => {
        this.pageSize = Math.max(1, settings.entitiesPerPage ?? 20);
        this.resetAndLoadFirstPage();
      },
      error: () => {
        this.pageSize = 20;
        this.resetAndLoadFirstPage();
      },
    });
  }

  ngOnDestroy(): void {
    if (this.scrollLoadDebounceTimer != null) {
      clearTimeout(this.scrollLoadDebounceTimer);
      this.scrollLoadDebounceTimer = null;
    }
  }

  private resetAndLoadFirstPage(): void {
    this.page = 1;
    this.collections = [];
    this.hasMore = false;
    // Must clear before loadNextPage(): it no-ops while loading is true, and the component
    // starts with loading=true so the first fetch would never run.
    this.loading = false;
    this.loadNextPage();
  }

  /** Loads the next page and appends (same pattern as films lists). */
  loadNextPage(): void {
    if (this.loading) return;

    this.loading = true;
    this.errorMessage = null;
    this.collectionService.getMinePaged(this.page, this.pageSize).subscribe({
      next: (response: PagedResponseDto<CollectionDto>) => {
        const rows = response?.data ?? [];
        this.collections.push(...rows);
        this.hasMore = !!response?.hasNextPage;
        this.page++;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading collections');
      },
    });
  }

  loadMoreCollections(): void {
    this.loadNextPage();
  }

  @HostListener('window:scroll', [])
  onWindowScroll(): void {
    if (this.scrollLoadDebounceTimer != null) {
      clearTimeout(this.scrollLoadDebounceTimer);
    }
    this.scrollLoadDebounceTimer = setTimeout(() => {
      this.scrollLoadDebounceTimer = null;
      this.maybeLoadMoreOnScroll();
    }, this.scrollDebounceMs);
  }

  private maybeLoadMoreOnScroll(): void {
    const threshold = 300;
    const pos = window.innerHeight + window.scrollY;
    const max = document.body.offsetHeight - threshold;
    if (pos < max) return;
    if (this.hasMore && !this.loading) {
      this.loadMoreCollections();
    }
  }

  onNewClick(): void {
    this.router.navigate(['/collections/new']);
  }

  onCollectionClick(c: CollectionDto): void {
    this.router.navigate(['/collections', c.id]);
  }

  onCollectionOpenInNewTab(c: CollectionDto): void {
    openRouteInNewTab(this.router, ['/collections', c.id]);
  }

  hasCardImage(c: CollectionDto): boolean {
    return !!c.imageUrl?.trim();
  }

  /** Blob URLs use full-size path; list/table use preview thumbnails only (same rule as PhotoService). */
  cardImageUrl(c: CollectionDto): string {
    const u = c.imageUrl?.trim();
    if (!u) return '';
    if (u.includes('photos/preview/')) return u;
    return u.replace('photos/', 'photos/preview/');
  }

  formatRange(c: CollectionDto): string {
    const a = c.fromDate;
    const b = c.toDate;
    if (!a && !b) return '';
    if (a && b) return `${a} – ${b}`;
    return a ?? b ?? '';
  }

  /** Table cells: ISO or date string, max yyyy-MM-dd */
  formatDateColumn(value: string | null | undefined): string {
    if (value == null || String(value).trim() === '') return '';
    return String(value).slice(0, 10);
  }
}
