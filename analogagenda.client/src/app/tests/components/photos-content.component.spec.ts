import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { PhotosContentComponent } from '../../components/films/photos-content/photos-content.component';
import { PhotoService } from '../../services';
import { FilmDto, PhotoDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('PhotosContentComponent', () => {
  let component: PhotosContentComponent;
  let fixture: ComponentFixture<PhotosContentComponent>;
  let mockRouter: jasmine.SpyObj<Router>;
  const mockFilm: FilmDto = {
    id: 'film-1',
    brand: 'Test Film',
    iso: '400',
    type: FilmType.ColorNegative,
    numberOfExposures: 36,
    cost: 10,
    purchasedBy: UsernameType.Angel,
    purchasedOn: '2024-01-01',
    description: '',
    developed: true,
    imageUrl: '',
  };
  const mockPhotos: PhotoDto[] = [
    { id: 'p1', filmId: 'film-1', index: 1, imageUrl: 'u1', imageBase64: '' },
    { id: 'p2', filmId: 'film-1', index: 2, imageUrl: 'u2', imageBase64: '' },
  ];

  beforeEach(async () => {
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', ['getPreviewUrl']);
    photoServiceSpy.getPreviewUrl.and.callFake((p: PhotoDto) => `url-${p.id}`);
    mockRouter = TestConfig.createRouterSpy();

    await TestConfig.configureTestBed({
      declarations: [PhotosContentComponent],
      providers: [
        { provide: PhotoService, useValue: photoServiceSpy },
        { provide: Router, useValue: mockRouter },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PhotosContentComponent);
    component = fixture.componentInstance;
    component.photos = [...mockPhotos];
    component.film = mockFilm;
    component.isOwner = true;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('preview', () => {
    it('should open preview and set current photo', () => {
      component.openPreview(component.photos[1]);
      expect(component.isPreviewModalOpen).toBeTrue();
      expect(component.currentPreviewPhoto).toEqual(component.photos[1]);
      expect(component.currentPhotoIndex).toBe(1);
    });

    it('should close preview', () => {
      component.openPreview(component.photos[0]);
      component.closePreview();
      expect(component.isPreviewModalOpen).toBeFalse();
      expect(component.currentPreviewPhoto).toBeNull();
      expect(component.currentPhotoIndex).toBe(0);
    });

    it('should navigate next photo', () => {
      component.openPreview(component.photos[0]);
      component.nextPhoto();
      expect(component.currentPhotoIndex).toBe(1);
      expect(component.currentPreviewPhoto).toEqual(component.photos[1]);
    });

    it('should navigate previous photo', () => {
      component.openPreview(component.photos[1]);
      component.previousPhoto();
      expect(component.currentPhotoIndex).toBe(0);
      expect(component.currentPreviewPhoto).toEqual(component.photos[0]);
    });

    it('should not go below 0 on previous', () => {
      component.openPreview(component.photos[0]);
      component.previousPhoto();
      expect(component.currentPhotoIndex).toBe(0);
    });

    it('should not go past last on next', () => {
      component.openPreview(component.photos[1]);
      component.nextPhoto();
      expect(component.currentPhotoIndex).toBe(1);
    });

    it('canNavigatePrevious and canNavigateNext should reflect position', () => {
      component.openPreview(component.photos[0]);
      expect(component.canNavigatePrevious()).toBeFalse();
      expect(component.canNavigateNext()).toBeTrue();
      component.openPreview(component.photos[1]);
      expect(component.canNavigatePrevious()).toBeTrue();
      expect(component.canNavigateNext()).toBeFalse();
    });

    it('should handle Escape to close preview', () => {
      component.openPreview(component.photos[0]);
      const ev = new KeyboardEvent('keydown', { key: 'Escape' });
      spyOn(ev, 'preventDefault');
      component.onKeyDown(ev);
      expect(ev.preventDefault).toHaveBeenCalled();
      expect(component.isPreviewModalOpen).toBeFalse();
    });

    it('should handle ArrowRight for next', () => {
      component.openPreview(component.photos[0]);
      const ev = new KeyboardEvent('keydown', { key: 'ArrowRight' });
      spyOn(ev, 'preventDefault');
      component.onKeyDown(ev);
      expect(component.currentPhotoIndex).toBe(1);
    });

    it('should handle ArrowLeft for previous', () => {
      component.openPreview(component.photos[1]);
      const ev = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
      spyOn(ev, 'preventDefault');
      component.onKeyDown(ev);
      expect(component.currentPhotoIndex).toBe(0);
    });
  });

  describe('delete modal', () => {
    it('should open and close delete modal', () => {
      component.openDeleteModal();
      expect(component.isDeleteModalOpen).toBeTrue();
      component.closeDeleteModal();
      expect(component.isDeleteModalOpen).toBeFalse();
    });

    it('confirmDelete should emit deletePhoto and close modal when preview open', () => {
      component.openPreview(component.photos[0]);
      component.openDeleteModal();
      let emitted: PhotoDto | undefined;
      component.deletePhoto.subscribe((p) => (emitted = p));
      component.confirmDelete();
      expect(emitted).toEqual(component.photos[0]);
      expect(component.isDeleteModalOpen).toBeFalse();
    });
  });

  describe('event emissions', () => {
    it('onDownload should emit download with photo', () => {
      let emitted: PhotoDto | undefined;
      component.download.subscribe((p) => (emitted = p));
      component.onDownload(mockPhotos[0]);
      expect(emitted).toEqual(mockPhotos[0]);
    });

    it('onDownloadAll should emit downloadAll with size flag', () => {
      let emitted: boolean | undefined;
      component.downloadAll.subscribe((s) => (emitted = s));
      component.onDownloadAll(true);
      expect(emitted).toBeTrue();
      component.onDownloadAll(false);
      expect(emitted).toBeFalse();
    });

    it('onRestrictToggle should emit restrictToggle when preview photo set', () => {
      component.openPreview(component.photos[1]);
      let emitted: PhotoDto | undefined;
      component.restrictToggle.subscribe((p) => (emitted = p));
      component.onRestrictToggle();
      expect(emitted).toEqual(component.photos[1]);
    });
  });

  describe('navigation', () => {
    it('navigateToEditFilm should call router with film id', () => {
      component.navigateToEditFilm();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/films', 'film-1']);
    });

    it('navigateToEditPhotos should call router with film id and photos', () => {
      component.navigateToEditPhotos();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/films', 'film-1', 'photos']);
    });
  });

  describe('bulk selection', () => {
    beforeEach(() => {
      component.mode = 'edit';
      component.bulkSelectionEnabled = true;
      component.allowedBulkPhotoIds = null;
      fixture.detectChanges();
    });

    it('startBulkSelection should enable bulk mode with empty selection', () => {
      component.startBulkSelection();
      expect(component.bulkSelectionMode).toBeTrue();
      expect(component.selectedBulkCount).toBe(0);
    });

    it('getEligibleBulkPhotos should return all photos when allowlist is null', () => {
      expect(component.getEligibleBulkPhotos().map((p) => p.id)).toEqual(['p1', 'p2']);
    });

    it('getEligibleBulkPhotos should filter by allowedBulkPhotoIds', () => {
      component.allowedBulkPhotoIds = ['p2'];
      fixture.detectChanges();
      expect(component.getEligibleBulkPhotos().map((p) => p.id)).toEqual(['p2']);
    });

    it('getEligibleBulkPhotos should return no photos when allowlist is empty array', () => {
      component.allowedBulkPhotoIds = [];
      fixture.detectChanges();
      expect(component.getEligibleBulkPhotos()).toEqual([]);
    });

    it('canToggleBulkForPhoto should be false for all photos when allowlist is empty array', () => {
      component.allowedBulkPhotoIds = [];
      fixture.detectChanges();
      expect(component.canToggleBulkForPhoto(mockPhotos[0])).toBeFalse();
      expect(component.canToggleBulkForPhoto(mockPhotos[1])).toBeFalse();
    });

    it('selectAllPhotos should select every eligible photo', () => {
      component.startBulkSelection();
      component.selectAllPhotos();
      expect(component.allEligibleBulkSelected).toBeTrue();
      expect(component.selectedBulkCount).toBe(2);
    });

    it('toggleSelectAllOrDeselectAll should clear when all eligible are selected', () => {
      component.startBulkSelection();
      component.selectAllPhotos();
      expect(component.allEligibleBulkSelected).toBeTrue();
      component.toggleSelectAllOrDeselectAll();
      expect(component.selectedBulkCount).toBe(0);
      expect(component.allEligibleBulkSelected).toBeFalse();
    });

    it('toggleSelectAllOrDeselectAll should select all when not fully selected', () => {
      component.startBulkSelection();
      component.togglePhotoBulkSelected(mockPhotos[0]);
      expect(component.allEligibleBulkSelected).toBeFalse();
      component.toggleSelectAllOrDeselectAll();
      expect(component.allEligibleBulkSelected).toBeTrue();
    });

    it('allEligibleBulkSelected should be false when there are no photos', () => {
      component.photos = [];
      component.startBulkSelection();
      expect(component.allEligibleBulkSelected).toBeFalse();
    });

    it('bulk toolbar should label Cancel as Cancel Selection and toggle Select All label', () => {
      component.startBulkSelection();
      fixture.detectChanges();
      const el = fixture.nativeElement as HTMLElement;
      const texts = Array.from(el.querySelectorAll('button .button-content')).map((n) => n.textContent?.trim());
      expect(texts).toContain('Cancel Selection');
      expect(texts).toContain('Select All');
      component.selectAllPhotos();
      fixture.detectChanges();
      const textsAfter = Array.from(el.querySelectorAll('button .button-content')).map((n) => n.textContent?.trim());
      expect(textsAfter).toContain('Deselect All');
    });
  });
});
