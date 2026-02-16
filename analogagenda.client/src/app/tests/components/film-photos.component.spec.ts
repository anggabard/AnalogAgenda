import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FilmPhotosComponent } from '../../components/films/film-photos/film-photos.component';
import { FilmService, PhotoService, AccountService } from '../../services';
import { FilmDto, PhotoDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';
import { DownloadHelper } from '../../helpers/download.helper';

describe('FilmPhotosComponent', () => {
  let component: FilmPhotosComponent;
  let fixture: ComponentFixture<FilmPhotosComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockPhotoService: jasmine.SpyObj<PhotoService>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockFilm: FilmDto = {
    id: 'test-film-id',
    brand: 'Test Film',
    iso: '400',
    type: FilmType.ColorNegative,
    numberOfExposures: 36,
    cost: 12.50,
    purchasedBy: UsernameType.Angel,
    purchasedOn: '2023-01-01',
    description: 'Test film',
    developed: true,
    imageUrl: 'test-image-url',
  };

  const mockPhotos: PhotoDto[] = [
    { id: 'photo1', filmId: 'test-film-id', index: 1, imageUrl: 'image1.jpg', imageBase64: '' },
    { id: 'photo2', filmId: 'test-film-id', index: 2, imageUrl: 'image2.jpg', imageBase64: '' },
    { id: 'photo3', filmId: 'test-film-id', index: 3, imageUrl: 'image3.jpg', imageBase64: '' }
  ];

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getById']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', [
      'getPhotosByFilmId', 'downloadPhoto', 'downloadAllPhotos', 'deletePhoto', 'uploadMultiplePhotos', 'setRestricted', 'getPreviewUrl'
    ]);
    photoServiceSpy.getPreviewUrl.and.callFake((photo: PhotoDto) => `preview-${photo.id}`);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    accountServiceSpy.whoAmI.and.returnValue(of({ username: 'Angel', email: 'angel@test.com' }));
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
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmPhotosComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockPhotoService = TestBed.inject(PhotoService) as jasmine.SpyObj<PhotoService>;
    mockAccountService = TestBed.inject(AccountService) as jasmine.SpyObj<AccountService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  const initializeComponent = async () => {
    mockFilmService.getById.and.returnValue(of(mockFilm));
    mockPhotoService.getPhotosByFilmId.and.returnValue(of(mockPhotos));
    mockAccountService.whoAmI.and.returnValue(of({ username: 'Angel', email: 'angel@test.com' }));

    component.filmId = 'test-film-id';
    component.film = mockFilm;
    component.photos = JSON.parse(JSON.stringify(mockPhotos));
    component.errorMessage = null;
    component.uploadLoading = false;
    component.uploadProgress = { current: 0, total: 0 };
    component.downloadAllLoading = false;

    fixture.detectChanges();
    component.loading = false;
  };

  const getFreshPhotos = () => JSON.parse(JSON.stringify(mockPhotos));

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with film ID from route and load data', async () => {
    mockFilmService.getById.and.returnValue(of(mockFilm));
    mockPhotoService.getPhotosByFilmId.and.returnValue(of(mockPhotos));
    mockAccountService.whoAmI.and.returnValue(of({ username: 'Angel', email: 'angel@test.com' }));
    fixture.detectChanges();

    await new Promise((r) => setTimeout(r, 0));

    expect(component.filmId).toBe('test-film-id');
    expect(component.film).toEqual(mockFilm);
    expect(component.photos.length).toBe(3);
    expect(component.loading).toBeFalsy();
  });

  it('should navigate to /films when film ID is missing', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    const newFixture = TestBed.createComponent(FilmPhotosComponent);
    newFixture.detectChanges();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/films']);
  });

  it('should set errorMessage when film/photos loading fails', async () => {
    mockFilmService.getById.and.returnValue(of(mockFilm));
    mockPhotoService.getPhotosByFilmId.and.returnValue(throwError(() => new Error('fail')));
    mockAccountService.whoAmI.and.returnValue(of({ username: 'Angel', email: 'a@b.com' }));
    fixture.detectChanges();

    await new Promise((r) => setTimeout(r, 50));

    expect(component.errorMessage).toBe('Error loading film photos.');
    expect(component.loading).toBeFalsy();
  });

  describe('Download Functions', () => {
    beforeEach(async () => {
      await initializeComponent();
    });

    it('should download single photo via onDownloadPhoto', () => {
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

      component.onDownloadPhoto(mockPhotos[0]);

      expect(mockPhotoService.downloadPhoto).toHaveBeenCalledWith('photo1');
    });

    it('should set errorMessage when download photo fails', () => {
      mockPhotoService.downloadPhoto.and.returnValue(throwError(() => new Error('Download failed')));

      component.onDownloadPhoto(mockPhotos[0]);

      expect(component.errorMessage).toBe('Error downloading photo.');
    });

    it('should download all photos via onDownloadAllPhotos', () => {
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

      component.onDownloadAllPhotos(false);

      expect(mockPhotoService.downloadAllPhotos).toHaveBeenCalledWith('test-film-id', false);
    });

    it('should set errorMessage when download all photos fails', () => {
      mockPhotoService.downloadAllPhotos.and.returnValue(throwError(() => new Error('Download failed')));

      component.onDownloadAllPhotos(false);

      expect(component.errorMessage).toBe('Error downloading photos archive.');
    });
  });

  describe('onDeletePhoto', () => {
    beforeEach(async () => {
      await initializeComponent();
    });

    it('should remove photo from list on success', (done) => {
      mockPhotoService.deletePhoto.and.returnValue(of(undefined));
      component.photos = getFreshPhotos();

      component.onDeletePhoto(component.photos[0]);

      setTimeout(() => {
        expect(mockPhotoService.deletePhoto).toHaveBeenCalledWith('photo1');
        expect(component.photos.length).toBe(2);
        expect(component.photos.find((p) => p.id === 'photo1')).toBeUndefined();
        done();
      }, 50);
    });

    it('should set errorMessage when delete fails', () => {
      mockPhotoService.deletePhoto.and.returnValue(throwError(() => new Error('Delete failed')));

      component.onDeletePhoto(mockPhotos[0]);

      expect(component.errorMessage).toBe('Error deleting photo.');
    });
  });

  describe('Photo Upload', () => {
    beforeEach(async () => {
      await initializeComponent();
      component.photos = getFreshPhotos();
      (mockPhotoService as any).uploadMultiplePhotos = jasmine.createSpy('uploadMultiplePhotos').and.returnValue(Promise.resolve([]));
    });

    it('should upload photos and update loading state', async () => {
      const file1 = new File(['test1'], '1.jpg', { type: 'image/jpeg' });
      const files = [file1] as any;

      (mockPhotoService.uploadMultiplePhotos as jasmine.Spy).and.returnValue(Promise.resolve([{ success: true, photo: mockPhotos[0] }]));

      await (component as any).processPhotoUploads(files);

      expect(mockPhotoService.uploadMultiplePhotos).toHaveBeenCalled();
      expect(component.uploadLoading).toBe(false);
    });

    it('should handle upload errors', async () => {
      const file1 = new File(['test1'], '1.jpg', { type: 'image/jpeg' });
      const files = [file1] as any;

      (mockPhotoService.uploadMultiplePhotos as jasmine.Spy).and.returnValue(Promise.resolve([{ success: false, error: 'Upload failed' }]));

      await (component as any).processPhotoUploads(files);

      expect(component.uploadLoading).toBe(false);
      expect(component.errorMessage).toContain('failed');
    });

    it('should set uploadLoading during upload', async () => {
      const file1 = new File(['test1'], '1.jpg', { type: 'image/jpeg' });
      const files = [file1] as any;

      let resolveUpload: (value: Array<{ success: boolean; photo?: PhotoDto; error?: string }>) => void;
      const uploadPromise = new Promise<Array<{ success: boolean; photo?: PhotoDto; error?: string }>>((resolve) => {
        resolveUpload = resolve;
      });
      (mockPhotoService.uploadMultiplePhotos as jasmine.Spy).and.returnValue(uploadPromise);

      const uploadProcess = (component as any).processPhotoUploads(files);

      expect(component.uploadLoading).toBe(true);

      resolveUpload!([{ success: true, photo: mockPhotos[0] }]);
      await uploadProcess;

      expect(component.uploadLoading).toBe(false);
    });
  });

  describe('Sanitization', () => {
    it('should use DownloadHelper for sanitizing file names', () => {
      expect(DownloadHelper.sanitizeForFileName('Test@Film#Name$With%Special&Chars!')).toBe('TestFilmNameWithSpecialChars');
    });

    it('should handle empty file names via DownloadHelper', () => {
      expect(DownloadHelper.sanitizeForFileName('')).toBe('file');
    });

    it('should truncate long file names via DownloadHelper', () => {
      const longName = 'A'.repeat(100);
      expect(DownloadHelper.sanitizeForFileName(longName).length).toBe(50);
    });
  });
});
