import { Component, OnInit, TemplateRef, ViewChild, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CollectionService, UserSettingsService } from '../../services';
import { CollectionDto, PagedResponseDto } from '../../DTOs';
import { ErrorHandlingHelper } from '../../helpers/error-handling.helper';
import { toPhotosPreviewDisplayUrl } from '../../helpers/photo-url.helper';
import { openRouteInNewTab } from '../../helpers/navigation.helper';

/** Single request loads all collections (not tied to user "entities per page"). */
const COLLECTIONS_LIST_PAGE_SIZE = 10_000;

@Component({
  selector: 'app-collections',
  templateUrl: './collections.component.html',
  styleUrl: './collections.component.css',
  standalone: false,
})
export class CollectionsComponent implements OnInit {
  private collectionService = inject(CollectionService);
  private userSettingsService = inject(UserSettingsService);
  private router = inject(Router);

  @ViewChild('collectionCardTemplate') collectionCardTemplate!: TemplateRef<any>;
  @ViewChild('collectionRowTemplate') collectionRowTemplate!: TemplateRef<any>;

  collectionTableHeaders = ['Name', 'Date From', 'Date To', 'Location', 'Finished', 'Preview'];

  collections: CollectionDto[] = [];
  loading = true;
  errorMessage: string | null = null;
  /** Set after user settings load so we can branch card grid vs table. */
  settingsLoaded = false;
  tableView = false;

  ngOnInit(): void {
    this.userSettingsService.getUserSettings().subscribe({
      next: (settings) => {
        this.tableView = settings.tableView ?? false;
        this.settingsLoaded = true;
        this.loadAllCollections();
      },
      error: () => {
        this.tableView = false;
        this.settingsLoaded = true;
        this.loadAllCollections();
      },
    });
  }

  private loadAllCollections(): void {
    this.loading = true;
    this.errorMessage = null;
    this.collections = [];
    this.collectionService.getMinePaged(1, COLLECTIONS_LIST_PAGE_SIZE).subscribe({
      next: (response: PagedResponseDto<CollectionDto>) => {
        this.collections = response?.data ?? [];
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading collections');
      },
    });
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

  /** Suppress browser middle-click autoscroll so auxclick can open in a new tab (same as app-card-list). */
  onCollectionGridMiddleMouseDown(event: MouseEvent): void {
    if (event.button === 1) {
      event.preventDefault();
    }
  }

  onCollectionGridAuxClick(event: MouseEvent, c: CollectionDto): void {
    if (event.button !== 1) return;
    event.preventDefault();
    this.onCollectionOpenInNewTab(c);
  }

  hasCardImage(c: CollectionDto): boolean {
    return !!c.imageUrl?.trim();
  }

  /** Blob URLs use full-size path; list/table use preview thumbnails only (shared with PhotoService). */
  cardImageUrl(c: CollectionDto): string {
    return toPhotosPreviewDisplayUrl(c.imageUrl, c.updatedDate);
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
