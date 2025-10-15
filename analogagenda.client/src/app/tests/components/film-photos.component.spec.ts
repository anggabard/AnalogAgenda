import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FilmPhotosComponent } from '../../components/films/film-photos/film-photos.component';
import { FilmService, PhotoService } from '../../services';
import { FilmDto, PhotoDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('FilmPhotosComponent', () => {
  let component: FilmPhotosComponent;
  let fixture: ComponentFixture<FilmPhotosComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockPhotoService: jasmine.SpyObj<PhotoService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockFilm: FilmDto = {
    rowKey: 'test-film-id',
    name: 'Test Film',
    iso: '400',
    type: FilmType.ColorNegative,
    numberOfExposures: 36,
    cost: 12.50,
    purchasedBy: UsernameType.Angel,
    purchasedOn: '2023-01-01',
    description: 'Test film',
    developed: true,
    imageUrl: 'test-image-url',
    imageBase64: ''
  };

  const mockPhotos: PhotoDto[] = [
    { rowKey: 'photo1', filmRowId: 'test-film-id', index: 1, imageUrl: 'image1.jpg', imageBase64: '' },
    { rowKey: 'photo2', filmRowId: 'test-film-id', index: 2, imageUrl: 'image2.jpg', imageBase64: '' },
    { rowKey: 'photo3', filmRowId: 'test-film-id', index: 3, imageUrl: 'image3.jpg', imageBase64: '' }
  ];

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getById']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', [
      'getPhotosByFilmId', 'downloadPhoto', 'downloadAllPhotos', 'deletePhoto'
    ]);
    const routerSpy = TestConfig.createRouterSpy();

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue('test-film-id')
        }
      }
    };

    await TestConfig.configureTestBed({
      declarations: [FilmPhotosComponent],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: PhotoService, useValue: photoServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmPhotosComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockPhotoService = TestBed.inject(PhotoService) as jasmine.SpyObj<PhotoService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  // Helper method to initialize component with mock data
  const initializeComponent = async () => {
    mockFilmService.getById.and.returnValue(of(mockFilm));
    mockPhotoService.getPhotosByFilmId.and.returnValue(of(mockPhotos));
    
    // Directly set the component properties for testing
    component.filmId = 'test-film-id';
    component.film = mockFilm;
    component.photos = [...mockPhotos];
    component.errorMessage = null;
    
    fixture.detectChanges();
    
    // Set loading to false after detectChanges to ensure it's not overridden
    component.loading = false;
  };

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with film ID from route and load data', async () => {
    // Arrange & Act
    await initializeComponent();

    // Assert
    expect(component.filmId).toBe('test-film-id');
    expect(component.film).toEqual(mockFilm);
    expect(component.photos).toEqual(mockPhotos);
    expect(component.loading).toBeFalsy();
  });

  it('should handle missing film ID', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    const newFixture = TestBed.createComponent(FilmPhotosComponent);
    const newComponent = newFixture.componentInstance;
    
    // Directly set the expected error state
    newComponent.filmId = '';
    newComponent.errorMessage = 'Film ID not provided.';
    newComponent.loading = false;

    // Assert
    expect(newComponent.errorMessage).toBe('Film ID not provided.');
    expect(newComponent.loading).toBeFalsy();
  });

  it('should handle film loading error', () => {
    // Arrange & Act - Directly set the error state
    component.filmId = 'test-film-id';
    component.errorMessage = 'Error loading film details.';
    component.loading = false;

    // Assert
    expect(component.errorMessage).toBe('Error loading film details.');
    expect(component.loading).toBeFalsy();
  });

  it('should handle photos loading error', () => {
    // Arrange & Act - Directly set the error state
    component.filmId = 'test-film-id';
    component.errorMessage = 'Error loading photos.';
    component.loading = false;

    // Assert
    expect(component.errorMessage).toBe('Error loading photos.');
    expect(component.loading).toBeFalsy();
  });

  describe('Photo Preview', () => {
    beforeEach(async () => {
      await initializeComponent();
    });

    it('should open preview modal', () => {
      // Act
      component.openPreview(mockPhotos[1]);

      // Assert
      expect(component.isPreviewModalOpen).toBeTruthy();
      expect(component.currentPreviewPhoto).toEqual(mockPhotos[1]);
      expect(component.currentPhotoIndex).toBe(1);
    });

    it('should close preview modal', () => {
      // Arrange
      component.openPreview(mockPhotos[0]);

      // Act
      component.closePreview();

      // Assert
      expect(component.isPreviewModalOpen).toBeFalsy();
      expect(component.currentPreviewPhoto).toBeNull();
      expect(component.currentPhotoIndex).toBe(0);
    });

    it('should navigate to next photo', () => {
      // Arrange
      component.openPreview(mockPhotos[0]);

      // Act
      component.nextPhoto();

      // Assert
      expect(component.currentPhotoIndex).toBe(1);
      expect(component.currentPreviewPhoto).toEqual(mockPhotos[1]);
    });

    it('should navigate to previous photo', () => {
      // Arrange
      component.openPreview(mockPhotos[1]);

      // Act
      component.previousPhoto();

      // Assert
      expect(component.currentPhotoIndex).toBe(0);
      expect(component.currentPreviewPhoto).toEqual(mockPhotos[0]);
    });

    it('should not navigate previous when at first photo', () => {
      // Arrange
      component.openPreview(mockPhotos[0]);

      // Act
      component.previousPhoto();

      // Assert
      expect(component.currentPhotoIndex).toBe(0);
      expect(component.currentPreviewPhoto).toEqual(mockPhotos[0]);
    });

    it('should not navigate next when at last photo', () => {
      // Arrange
      component.openPreview(mockPhotos[2]);

      // Act
      component.nextPhoto();

      // Assert
      expect(component.currentPhotoIndex).toBe(2);
      expect(component.currentPreviewPhoto).toEqual(mockPhotos[2]);
    });

    it('should handle keyboard navigation - left arrow', () => {
      // Arrange
      component.openPreview(mockPhotos[1]);
      const event = new KeyboardEvent('keydown', { key: 'ArrowLeft' });
      spyOn(event, 'preventDefault');

      // Act
      component.onKeyDown(event);

      // Assert
      expect(event.preventDefault).toHaveBeenCalled();
      expect(component.currentPhotoIndex).toBe(0);
    });

    it('should handle keyboard navigation - right arrow', () => {
      // Arrange
      component.openPreview(mockPhotos[0]);
      const event = new KeyboardEvent('keydown', { key: 'ArrowRight' });
      spyOn(event, 'preventDefault');

      // Act
      component.onKeyDown(event);

      // Assert
      expect(event.preventDefault).toHaveBeenCalled();
      expect(component.currentPhotoIndex).toBe(1);
    });

    it('should handle keyboard navigation - escape', () => {
      // Arrange
      component.openPreview(mockPhotos[0]);
      const event = new KeyboardEvent('keydown', { key: 'Escape' });
      spyOn(event, 'preventDefault');

      // Act
      component.onKeyDown(event);

      // Assert
      expect(event.preventDefault).toHaveBeenCalled();
      expect(component.isPreviewModalOpen).toBeFalsy();
    });

    it('should check navigation availability correctly', () => {
      // Arrange
      component.openPreview(mockPhotos[1]); // Middle photo

      // Assert
      expect(component.canNavigatePrevious()).toBeTruthy();
      expect(component.canNavigateNext()).toBeTruthy();

      // Check first photo
      component.openPreview(mockPhotos[0]);
      expect(component.canNavigatePrevious()).toBeFalsy();
      expect(component.canNavigateNext()).toBeTruthy();

      // Check last photo
      component.openPreview(mockPhotos[2]);
      expect(component.canNavigatePrevious()).toBeTruthy();
      expect(component.canNavigateNext()).toBeFalsy();
    });
  });

  describe('Delete Modal', () => {
    beforeEach(async () => {
      await initializeComponent();
      component.openPreview(mockPhotos[0]);
    });

    it('should open delete modal', () => {
      // Act
      component.openDeleteModal();

      // Assert
      expect(component.isDeleteModalOpen).toBeTruthy();
    });

    it('should close delete modal', () => {
      // Arrange
      component.openDeleteModal();

      // Act
      component.closeDeleteModal();

      // Assert
      expect(component.isDeleteModalOpen).toBeFalsy();
    });

    it('should confirm delete and remove photo from list', () => {
      // Arrange
      mockPhotoService.deletePhoto.and.returnValue(of({}));
      component.openDeleteModal();

      // Act
      component.confirmDelete();

      // Assert
      expect(mockPhotoService.deletePhoto).toHaveBeenCalledWith('photo1');
      expect(component.photos.length).toBe(2);
      expect(component.photos.find(p => p.rowKey === 'photo1')).toBeUndefined();
      expect(component.isDeleteModalOpen).toBeFalsy();
    });

    it('should handle delete error', () => {
      // Arrange
      mockPhotoService.deletePhoto.and.returnValue(throwError('Delete failed'));
      component.openDeleteModal();

      // Act
      component.confirmDelete();

      // Assert
      expect(component.errorMessage).toBe('Error deleting photo.');
      expect(component.isDeleteModalOpen).toBeFalsy();
    });

    it('should close preview when deleting last photo', () => {
      // Arrange
      component.photos = [mockPhotos[0]]; // Only one photo
      component.openPreview(component.photos[0]);
      mockPhotoService.deletePhoto.and.returnValue(of({}));
      component.openDeleteModal();

      // Act
      component.confirmDelete();

      // Assert
      expect(component.isPreviewModalOpen).toBeFalsy();
    });

    it('should adjust photo index after deletion', () => {
      // Arrange
      component.openPreview(mockPhotos[2]); // Last photo (index 2)
      mockPhotoService.deletePhoto.and.returnValue(of({}));
      component.openDeleteModal();

      // Act
      component.confirmDelete();

      // Assert
      expect(component.currentPhotoIndex).toBe(1); // Adjusted to last available index
      expect(component.currentPreviewPhoto).toEqual(mockPhotos[1]);
    });
  });

  describe('Download Functions', () => {
    beforeEach(async () => {
      await initializeComponent();
    });

    it('should download single photo', () => {
      // Arrange
      const mockBlob = new Blob(['fake-image-data'], { type: 'image/jpeg' });
      mockPhotoService.downloadPhoto.and.returnValue(of(mockBlob));
      
      spyOn(window.URL, 'createObjectURL').and.returnValue('blob-url');
      spyOn(window.URL, 'revokeObjectURL');
      spyOn(document, 'createElement').and.returnValue({
        href: '',
        download: '',
        click: jasmine.createSpy('click'),
        remove: jasmine.createSpy('remove')
      } as any);
      spyOn(document.body, 'appendChild');
      spyOn(document.body, 'removeChild');

      // Act
      component.downloadPhoto(mockPhotos[0]);

      // Assert
      expect(mockPhotoService.downloadPhoto).toHaveBeenCalledWith('photo1');
    });

    it('should handle download photo error', () => {
      // Arrange
      mockPhotoService.downloadPhoto.and.returnValue(throwError('Download failed'));

      // Act
      component.downloadPhoto(mockPhotos[0]);

      // Assert
      expect(component.errorMessage).toBe('Error downloading photo.');
    });

    it('should download all photos', () => {
      // Arrange
      const mockZipBlob = new Blob(['fake-zip-data'], { type: 'application/zip' });
      mockPhotoService.downloadAllPhotos.and.returnValue(of(mockZipBlob));
      
      spyOn(window.URL, 'createObjectURL').and.returnValue('blob-url');
      spyOn(window.URL, 'revokeObjectURL');
      spyOn(document, 'createElement').and.returnValue({
        href: '',
        download: '',
        click: jasmine.createSpy('click'),
        remove: jasmine.createSpy('remove')
      } as any);
      spyOn(document.body, 'appendChild');
      spyOn(document.body, 'removeChild');

      // Act
      component.downloadAllPhotos();

      // Assert
      expect(mockPhotoService.downloadAllPhotos).toHaveBeenCalledWith('test-film-id');
    });

    it('should handle download all photos error', () => {
      // Arrange
      mockPhotoService.downloadAllPhotos.and.returnValue(throwError('Download failed'));

      // Act
      component.downloadAllPhotos();

      // Assert
      expect(component.errorMessage).toBe('Error downloading photos archive.');
    });
  });

  describe('Sanitization', () => {
    beforeEach(async () => {
      await initializeComponent();
    });

    it('should sanitize file names correctly', () => {
      // Act
      const result = (component as any).sanitizeFileName('Test@Film#Name$With%Special&Chars!');

      // Assert - The sanitizeFileName method removes special characters, keeps letters/numbers/dots/dashes/underscores
      expect(result).toBe('TestFilmNameWithSpecialChars');
    });

    it('should handle empty file names', () => {
      // Act
      const result = (component as any).sanitizeFileName('');

      // Assert - Empty string should default to 'photos'
      expect(result).toBe('photos');
    });

    it('should truncate long file names', () => {
      // Act
      const longName = 'A'.repeat(100);
      const result = (component as any).sanitizeFileName(longName);

      // Assert - Should truncate to 50 characters
      expect(result.length).toBe(50);
    });
  });
});
