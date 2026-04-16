import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PublicCollectionService } from '../../../services';
import { PublicCollectionPageDto, CollectionPublicCommentDto, PhotoDto } from '../../../DTOs';
import { PhotosContentComponent } from '../../films/photos-content/photos-content.component';
import { DownloadHelper } from '../../../helpers/download.helper';
import { ErrorHandlingHelper } from '../../../helpers/error-handling.helper';

@Component({
  selector: 'app-public-collection-page',
  templateUrl: './public-collection-page.component.html',
  styleUrl: './public-collection-page.component.css',
  standalone: false,
})
export class PublicCollectionPageComponent implements OnInit {
  @ViewChild(PhotosContentComponent) photosContent?: PhotosContentComponent;

  private route = inject(ActivatedRoute);
  private publicCollectionService = inject(PublicCollectionService);

  collectionId = '';
  page: PublicCollectionPageDto | null = null;
  loading = true;
  errorMessage: string | null = null;
  /** Wrong password or validation on the gate only (not global page load errors). */
  verifyError: string | null = null;
  passwordInput = '';
  verifyLoading = false;
  downloadAllLoading = false;

  commentAuthor = '';
  commentBody = '';
  commentSubmitLoading = false;
  commentError: string | null = null;

  ngOnInit(): void {
    this.collectionId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.collectionId) {
      this.errorMessage = 'Invalid link.';
      this.loading = false;
      return;
    }
    this.loadPage();
  }

  loadPage(): void {
    this.loading = true;
    this.errorMessage = null;
    this.verifyError = null;
    this.publicCollectionService.getPage(this.collectionId).subscribe({
      next: (p) => {
        this.page = {
          ...p,
          photos: p.photos ?? [],
          comments: p.comments ?? [],
        };
        this.loading = false;
        if (!p.requiresPassword) {
          this.passwordInput = '';
          this.verifyError = null;
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'loading collection');
      },
    });
  }

  verify(): void {
    const pwd = this.passwordInput.trim();
    if (!pwd) {
      this.verifyError = 'Enter the password.';
      return;
    }
    if (pwd.length > 32) {
      this.verifyError = 'Password must be at most 32 characters.';
      return;
    }
    this.verifyLoading = true;
    this.verifyError = null;
    this.errorMessage = null;
    this.publicCollectionService.verify(this.collectionId, pwd).subscribe({
      next: () => {
        this.verifyLoading = false;
        this.passwordInput = '';
        // Defer GET until after the verify response’s Set-Cookie is committed for this origin.
        setTimeout(() => this.loadPage(), 0);
      },
      error: (err) => {
        this.verifyLoading = false;
        this.verifyError = ErrorHandlingHelper.handleError(err, 'verifying password');
      },
    });
  }

  onDownloadAll(small: boolean): void {
    if (!this.page?.photos?.length) return;
    this.downloadAllLoading = true;
    this.publicCollectionService.downloadAll(this.collectionId, small).subscribe({
      next: (blob) => {
        DownloadHelper.triggerBlobDownload(
          blob,
          `${this.sanitizeName(this.page!.name ?? 'collection')}${small ? '-small' : ''}.zip`
        );
        this.downloadAllLoading = false;
      },
      error: (err) => {
        this.downloadAllLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'download');
      },
    });
  }

  onPreviewPhotoDownload(photo: PhotoDto): void {
    this.publicCollectionService.downloadPhoto(this.collectionId, photo.id).subscribe({
      next: (blob) => {
        const namePart = DownloadHelper.sanitizeForFileName(this.page?.name ?? 'collection');
        const idx = String(photo.collectionIndex ?? photo.index).padStart(3, '0');
        const ext = blob.type?.includes('png')
          ? 'png'
          : blob.type?.includes('webp')
            ? 'webp'
            : 'jpg';
        DownloadHelper.triggerBlobDownload(blob, `${idx}-${namePart}.${ext}`);
      },
      error: (err) => {
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'downloading photo');
      },
    });
  }

  onDownloadSelected(payload: { small: boolean; photos: PhotoDto[] }): void {
    const ids = payload.photos.map((p) => p.id);
    if (ids.length === 0) return;
    this.downloadAllLoading = true;
    const small = payload.small;
    this.publicCollectionService.downloadSelected(this.collectionId, ids, small).subscribe({
      next: (blob) => {
        DownloadHelper.triggerBlobDownload(blob, this.selectedArchiveFileName(small));
        this.downloadAllLoading = false;
      },
      error: (err) => {
        this.downloadAllLoading = false;
        this.errorMessage = ErrorHandlingHelper.handleError(err, 'download');
      },
    });
  }

  submitComment(): void {
    const name = this.commentAuthor.trim();
    const body = this.commentBody.trim();
    if (!name || !body) {
      this.commentError = 'Name and comment are required.';
      return;
    }
    this.commentError = null;
    this.commentSubmitLoading = true;
    this.publicCollectionService.postComment(this.collectionId, { authorName: name, body }).subscribe({
      next: (c: CollectionPublicCommentDto) => {
        this.commentSubmitLoading = false;
        this.commentBody = '';
        if (this.page && !this.page.requiresPassword) {
          const list = this.page.comments ?? [];
          this.page.comments = [...list, c];
        }
      },
      error: (err) => {
        this.commentSubmitLoading = false;
        this.commentError = ErrorHandlingHelper.handleError(err, 'posting comment');
      },
    });
  }

  formatRange(): string {
    const p = this.page;
    if (!p || p.requiresPassword) return '';
    const a = p.fromDate ? String(p.fromDate).slice(0, 10) : '';
    const b = p.toDate ? String(p.toDate).slice(0, 10) : '';
    if (!a && !b) return '';
    if (a && b) return `${a} – ${b}`;
    return a || b || '';
  }

  /** dd/mm/yy · HH:mm in local time (API sends UTC with Z). */
  formatCommentWhen(iso: string | undefined): string {
    if (!iso) return '';
    const d = new Date(iso);
    if (Number.isNaN(d.getTime())) return '';
    const dd = String(d.getDate()).padStart(2, '0');
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const yy = String(d.getFullYear()).slice(-2);
    const hh = String(d.getHours()).padStart(2, '0');
    const min = String(d.getMinutes()).padStart(2, '0');
    return `${dd}/${mm}/${yy} · ${hh}:${min}`;
  }

  private sanitizeName(name: string): string {
    return name.replace(/[^\w\- ]+/g, '').trim() || 'collection';
  }

  private selectedArchiveFileName(small: boolean): string {
    const base = this.sanitizeName(this.page?.name ?? 'collection');
    const stem = small ? `${base}-small` : base;
    return `${stem}-selected.zip`;
  }
}
