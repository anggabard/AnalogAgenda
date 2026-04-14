import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { PhotosContentComponent } from '../../components/films/photos-content/photos-content.component';
import { PhotoService } from '../../services';
import { CollectionOptionDto, FilmDto, PhotoDto } from '../../DTOs';
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

    it('onDownloadSelected should emit downloadSelected with photos and small flag', () => {
      component.mode = 'edit';
      component.bulkSelectionEnabled = true;
      component.startBulkSelection();
      component.togglePhotoBulkSelected(mockPhotos[0]);
      component.optionsDropdownOpen = true;
      let payload: { small: boolean; photos: PhotoDto[] } | undefined;
      component.downloadSelected.subscribe((p) => (payload = p));
      component.onDownloadSelected(false);
      expect(payload!.small).toBeFalse();
      expect(payload!.photos.map((p) => p.id)).toEqual(['p1']);
      expect(component.optionsDropdownOpen).toBeFalse();
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

    it('bulk selection works in view mode when user is not film owner', () => {
      component.mode = 'view';
      component.isOwner = false;
      component.startBulkSelection();
      component.onPhotoItemClick(mockPhotos[0]);
      expect(component.selectedBulkCount).toBe(1);
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

  describe('Add to collection', () => {
    const openCollections: CollectionOptionDto[] = [
      { id: 'col-1', name: 'Summer 24', imageUrl: '' },
      { id: 'col-2', name: 'Trips', imageUrl: '' },
    ];

    beforeEach(() => {
      component.mode = 'view';
      component.isOwner = true;
      component.bulkSelectionEnabled = true;
      component.openCollectionOptions = openCollections;
      component.addToCollectionBusy = false;
      component.startBulkSelection();
      component.togglePhotoBulkSelected(mockPhotos[0]);
      component.togglePhotoBulkSelected(mockPhotos[1]);
      fixture.detectChanges();
    });

    it('toggleCollectionSubmenu should flip collectionSubmenuOpen and stop propagation', () => {
      const ev = { stopPropagation: jasmine.createSpy('stopPropagation') } as unknown as Event;
      expect(component.collectionSubmenuOpen).toBeFalse();
      component.toggleCollectionSubmenu(ev);
      expect(ev.stopPropagation).toHaveBeenCalled();
      expect(component.collectionSubmenuOpen).toBeTrue();
      component.toggleCollectionSubmenu(ev);
      expect(component.collectionSubmenuOpen).toBeFalse();
    });

    it('toggleOptionsDropdown when opening should clear collectionSubmenuOpen', () => {
      component.collectionSubmenuOpen = true;
      component.optionsDropdownOpen = false;
      component.toggleOptionsDropdown();
      expect(component.optionsDropdownOpen).toBeTrue();
      expect(component.collectionSubmenuOpen).toBeFalse();
    });

    it('onAddToCollectionPick should emit addToCollectionRequest with collectionId and selected photoIds', () => {
      component.optionsDropdownOpen = true;
      component.collectionSubmenuOpen = true;
      let emitted: { collectionId: string; photoIds: string[] } | undefined;
      component.addToCollectionRequest.subscribe((x) => (emitted = x));
      component.onAddToCollectionPick('col-2');
      expect(emitted).toEqual({ collectionId: 'col-2', photoIds: ['p1', 'p2'] });
      expect(component.optionsDropdownOpen).toBeFalse();
      expect(component.collectionSubmenuOpen).toBeFalse();
    });

    it('onAddToCollectionPick should not emit when no photos are selected', () => {
      component.selectedPhotoIds = new Set();
      let count = 0;
      component.addToCollectionRequest.subscribe(() => count++);
      component.onAddToCollectionPick('col-1');
      expect(count).toBe(0);
    });

    it('collection pick buttons should be disabled when addToCollectionBusy is true', () => {
      component.addToCollectionBusy = true;
      component.optionsDropdownOpen = true;
      component.collectionSubmenuOpen = true;
      fixture.detectChanges();
      const btns = (fixture.nativeElement as HTMLElement).querySelectorAll(
        '.collection-pick-list .dropdown-item'
      );
      expect(btns.length).toBe(2);
      btns.forEach((b) => {
        expect((b as HTMLButtonElement).disabled).toBeTrue();
      });
    });

    it('collection pick buttons should be enabled when addToCollectionBusy is false', () => {
      component.addToCollectionBusy = false;
      component.optionsDropdownOpen = true;
      component.collectionSubmenuOpen = true;
      fixture.detectChanges();
      const btns = (fixture.nativeElement as HTMLElement).querySelectorAll(
        '.collection-pick-list .dropdown-item'
      );
      expect(btns.length).toBe(2);
      btns.forEach((b) => {
        expect((b as HTMLButtonElement).disabled).toBeFalse();
      });
    });
  });
});
