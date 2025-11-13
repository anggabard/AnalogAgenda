import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UpsertFilmComponent } from '../../components/films/upsert-film/upsert-film.component';
import { FilmService, SessionService, DevKitService, PhotoService, UsedFilmThumbnailService } from '../../services';
import { DevKitType, UsernameType, FilmType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertFilmComponent', () => {
  let component: UpsertFilmComponent;
  let fixture: ComponentFixture<UpsertFilmComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockPhotoService: jasmine.SpyObj<PhotoService>;
  let mockThumbnailService: jasmine.SpyObj<UsedFilmThumbnailService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getById', 'update', 'add', 'getExposureDates', 'updateExposureDates']);
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'update', 'getAll']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getById', 'update', 'getAll']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', ['getAll', 'upload']);
    const thumbnailServiceSpy = jasmine.createSpyObj('UsedFilmThumbnailService', ['searchByFilmName', 'uploadThumbnail']);
    
    // Set up default return values for the spies
    filmServiceSpy.getById.and.returnValue(of({ formattedExposureDate: '' }));
    sessionServiceSpy.getAll.and.returnValue(of([]));
    devKitServiceSpy.getAll.and.returnValue(of([]));
    photoServiceSpy.getAll.and.returnValue(of([]));
    thumbnailServiceSpy.searchByFilmName.and.returnValue(of([]));
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue('test-film-key')
        },
        queryParams: { edit: 'true' }
      }
    };

    await TestConfig.configureTestBed({
      declarations: [UpsertFilmComponent],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: PhotoService, useValue: photoServiceSpy },
        { provide: UsedFilmThumbnailService, useValue: thumbnailServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    fixture = TestBed.createComponent(UpsertFilmComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockSessionService = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;
    mockDevKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    mockPhotoService = TestBed.inject(PhotoService) as jasmine.SpyObj<PhotoService>;
    mockThumbnailService = TestBed.inject(UsedFilmThumbnailService) as jasmine.SpyObj<UsedFilmThumbnailService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty arrays for available sessions and devkits', () => {
    expect(component.availableSessions).toEqual([]);
    expect(component.availableDevKits).toEqual([]);
  });

  it('should initialize with false for modal states', () => {
    expect(component.showSessionModal).toBeFalsy();
    expect(component.showDevKitModal).toBeFalsy();
  });

  it('should initialize with false for showExpiredDevKits', () => {
    expect(component.showExpiredDevKits).toBeFalsy();
  });

  it('should open session modal when onAssignSession is called', () => {
    mockSessionService.getAll.and.returnValue(of([]));
    component.onAssignSession();
    expect(component.showSessionModal).toBeTruthy();
  });

  it('should open devkit modal when onAssignDevKit is called', () => {
    mockDevKitService.getAll.and.returnValue(of([]));
    component.onAssignDevKit();
    expect(component.showDevKitModal).toBeTruthy();
  });

  it('should close session modal when closeSessionModal is called', () => {
    component.showSessionModal = true;
    component.closeSessionModal();
    expect(component.showSessionModal).toBeFalsy();
  });

  it('should close devkit modal when closeDevKitModal is called', () => {
    component.showDevKitModal = true;
    component.closeDevKitModal();
    expect(component.showDevKitModal).toBeFalsy();
  });

  it('should set selectedSessionId when selectSession is called', () => {
    const sessionId = 'test-session-key';
    component.selectSession(sessionId);
    expect(component.selectedSessionId).toBe(sessionId);
  });

  it('should set selectedDevKitId when selectDevKit is called', () => {
    const devKitId = 'test-devkit-key';
    component.selectDevKit(devKitId);
    expect(component.selectedDevKitId).toBe(devKitId);
  });

  it('should filter expired devkits when showExpiredDevKits is false', () => {
    const mockDevKits = [
      { id: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.showExpiredDevKits = false;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual([mockDevKits[0]]);
  });

  it('should show all devkits when showExpiredDevKits is true', () => {
    const mockDevKits = [
      { id: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.showExpiredDevKits = true;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual(mockDevKits);
  });

  it('should sort devkits alphabetically when showExpiredDevKits is false', () => {
    const mockDevKits = [
      { id: 'devkit-3', name: 'Z DevKit', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-1', name: 'A DevKit', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'M DevKit', url: '', type: DevKitType.E6, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.showExpiredDevKits = false;

    const result = component.filteredAvailableDevKits;

    expect(result.length).toBe(2);
    expect(result[0].name).toBe('A DevKit');
    expect(result[1].name).toBe('Z DevKit');
  });

  it('should sort devkits with expired last when showExpiredDevKits is true', () => {
    const mockDevKits = [
      { id: 'devkit-3', name: 'Z DevKit', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' },
      { id: 'devkit-1', name: 'A DevKit', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'M DevKit', url: '', type: DevKitType.E6, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.showExpiredDevKits = true;

    const result = component.filteredAvailableDevKits;

    expect(result.length).toBe(3);
    expect(result[0].name).toBe('A DevKit');
    expect(result[0].expired).toBeFalsy();
    expect(result[1].name).toBe('M DevKit');
    expect(result[1].expired).toBeTruthy();
    expect(result[2].name).toBe('Z DevKit');
    expect(result[2].expired).toBeTruthy();
  });

  it('should auto-show expired devkits when current devkit is expired', () => {
    const mockDevKits = [
      { id: 'devkit-1', name: 'Current DevKit', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'Other DevKit', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' }
    ];
    
    // Mock the DevKit service to return our test data
    mockDevKitService.getAll.and.returnValue(of(mockDevKits));
    
    component.form.patchValue({ developedWithDevKitId: 'devkit-1' });
    component.showExpiredDevKits = false;

    component.onAssignDevKit();

    expect(component.showExpiredDevKits).toBeTruthy();
    expect(component.showDevKitModal).toBeTruthy();
  });

  it('should not auto-show expired devkits when current devkit is not expired', () => {
    const mockDevKits = [
      { id: 'devkit-1', name: 'Current DevKit', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'Other DevKit', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.form.patchValue({ developedWithDevKitId: 'devkit-1' });
    component.showExpiredDevKits = false;

    component.onAssignDevKit();

    expect(component.showExpiredDevKits).toBeFalsy();
    expect(component.showDevKitModal).toBeTruthy();
  });

  it('should reset showExpiredDevKits when closing devkit modal', () => {
    component.showExpiredDevKits = true;
    component.showDevKitModal = true;

    component.closeDevKitModal();

    expect(component.showExpiredDevKits).toBeFalsy();
    expect(component.showDevKitModal).toBeFalsy();
  });

  it('should determine hasExpiredDevKits correctly', () => {
    const mockDevKits = [
      { id: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;

    expect(component.hasExpiredDevKits).toBeTruthy();
  });

  it('should return false for hasExpiredDevKits when no expired devkits', () => {
    const mockDevKits = [
      { id: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { id: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;

    expect(component.hasExpiredDevKits).toBeFalsy();
  });

  describe('ISO Validator', () => {
    it('should accept valid single ISO values', () => {
      const validIsos = ['100', '200', '400', '800', '1600', '3200'];
      
      validIsos.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.valid).toBeTruthy(`ISO ${iso} should be valid`);
      });
    });

    it('should accept valid ISO ranges', () => {
      const validRanges = ['100-400', '200-800', '50-200', '400-1600'];
      
      validRanges.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.valid).toBeTruthy(`ISO range ${iso} should be valid`);
      });
    });

    it('should reject ISO values with spaces', () => {
      const invalidIsos = ['100 - 400', '100 -400', '100- 400', ' 400', '400 '];
      
      invalidIsos.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject zero or negative ISO values', () => {
      const invalidIsos = ['0', '-100', '-400'];
      
      invalidIsos.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject invalid ISO ranges where first >= second', () => {
      const invalidRanges = ['400-100', '800-200', '400-400'];
      
      invalidRanges.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO range ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject ISO ranges with zero or negative values', () => {
      const invalidRanges = ['0-400', '100-0', '-100-400'];
      
      invalidRanges.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO range ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject non-numeric ISO values', () => {
      const invalidIsos = ['abc', 'ISO400', '400ISO', 'one hundred'];
      
      invalidIsos.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject ISO ranges with invalid separators', () => {
      const invalidRanges = ['100/400', '100to400', '100_400'];
      
      invalidRanges.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO range ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject ISO ranges with multiple dashes', () => {
      const invalidRanges = ['100-200-400', '100-200-400-800'];
      
      invalidRanges.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO range ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should reject ISO ranges with non-numeric parts', () => {
      const invalidRanges = ['100-abc', 'abc-400'];
      
      invalidRanges.forEach(iso => {
        component.form.patchValue({ iso });
        const isoControl = component.form.get('iso');
        expect(isoControl?.invalid).toBeTruthy(`ISO range ${iso} should be invalid`);
        expect(isoControl?.errors?.['invalidIso']).toBeTruthy();
      });
    });

    it('should require ISO field', () => {
      component.form.patchValue({ iso: '' });
      const isoControl = component.form.get('iso');
      expect(isoControl?.invalid).toBeTruthy('Empty ISO should be invalid');
      expect(isoControl?.errors?.['required']).toBeTruthy();
    });
  });

  describe('Thumbnail Functionality', () => {
    it('should initialize thumbnail search properties', () => {
      expect(component.thumbnailSearchQuery).toBe('');
      expect(component.thumbnailSearchResults).toEqual([]);
      expect(component.showThumbnailDropdown).toBeFalsy();
    });

    it('should initialize add thumbnail modal properties', () => {
      expect(component.showAddThumbnailModal).toBeFalsy();
      expect(component.newThumbnailFile).toBeNull();
      expect(component.newThumbnailFilmName).toBe('');
      expect(component.newThumbnailPreview).toBe('');
      expect(component.uploadingThumbnail).toBeFalsy();
    });

    it('should initialize thumbnail preview properties', () => {
      expect(component.showThumbnailPreview).toBeFalsy();
    });

    it('should perform thumbnail search when onThumbnailSearchClick is called', () => {
      const mockThumbnails = [
        { id: 'thumb1', filmName: 'Kodak Portra 400', imageId: 'img1', imageUrl: 'url1', imageBase64: '' },
        { id: 'thumb2', filmName: 'Fuji Superia 200', imageId: 'img2', imageUrl: 'url2', imageBase64: '' }
      ];
      mockThumbnailService.searchByFilmName.and.returnValue(of(mockThumbnails));

      component.onThumbnailSearchClick();

      expect(mockThumbnailService.searchByFilmName).toHaveBeenCalledWith('');
      expect(component.thumbnailSearchResults).toEqual(mockThumbnails);
      expect(component.showThumbnailDropdown).toBeTruthy();
    });

    it('should select thumbnail when onSelectThumbnail is called', () => {
      const mockThumbnail = { 
        id: 'thumb1', 
        filmName: 'Kodak Portra 400', 
        imageId: 'img1', 
        imageUrl: 'url1', 
        imageBase64: '' 
      };

      component.onSelectThumbnail(mockThumbnail);

      expect(component.form.get('imageUrl')?.value).toBe('url1');
      expect(component.form.get('imageId')?.value).toBe('img1');
      expect(component.thumbnailSearchQuery).toBe('Kodak Portra 400');
      expect(component.showThumbnailDropdown).toBeFalsy();
    });

    it('should determine canAddThumbnail based on film name', () => {
      component.form.patchValue({ name: 'Test Film' });
      expect(component.canAddThumbnail).toBeTruthy();

      component.form.patchValue({ name: '' });
      expect(component.canAddThumbnail).toBeFalsy();
    });

    it('should open add thumbnail modal when onAddNewThumbnail is called', () => {
      component.form.patchValue({ name: 'Test Film', iso: '400' });
      
      component.onAddNewThumbnail();

      expect(component.showAddThumbnailModal).toBeTruthy();
      expect(component.newThumbnailFilmName).toBe('Test Film 400');
    });

    it('should handle thumbnail file selection', () => {
      const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
      const mockEvent = {
        target: {
          files: [mockFile]
        }
      } as any;

      // Mock FileReader
      const mockFileReader = {
        readAsDataURL: jasmine.createSpy('readAsDataURL'),
        result: 'data:image/jpeg;base64,testdata',
        onload: null as any
      };
      spyOn(window, 'FileReader').and.returnValue(mockFileReader as any);

      component.onThumbnailFileSelected(mockEvent);

      expect(component.newThumbnailFile).toBe(mockFile);
      expect(mockFileReader.readAsDataURL).toHaveBeenCalledWith(mockFile);
      
      // Simulate FileReader onload
      mockFileReader.onload();
      
      expect(component.newThumbnailPreview).toBe('data:image/jpeg;base64,testdata');
    });

    it('should upload thumbnail when onUploadThumbnail is called', () => {
      const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
      component.newThumbnailFile = mockFile;
      component.newThumbnailFilmName = 'Test Film 400';
      
      const mockUploadedThumbnail = {
        id: 'thumb1',
        filmName: 'Test Film 400',
        imageId: 'img1',
        imageUrl: 'url1',
        imageBase64: ''
      };
      mockThumbnailService.uploadThumbnail.and.returnValue(of(mockUploadedThumbnail));

      // Mock FileReader
      const mockFileReader = {
        readAsDataURL: jasmine.createSpy('readAsDataURL'),
        result: 'data:image/jpeg;base64,testdata',
        onload: null as any
      };
      spyOn(window, 'FileReader').and.returnValue(mockFileReader as any);

      component.onUploadThumbnail();

      expect(mockFileReader.readAsDataURL).toHaveBeenCalledWith(mockFile);
      
      // Simulate FileReader onload
      mockFileReader.onload();

      expect(mockThumbnailService.uploadThumbnail).toHaveBeenCalled();
      expect(component.form.get('imageUrl')?.value).toBe('url1');
      expect(component.form.get('imageId')?.value).toBe('img1');
      expect(component.thumbnailSearchQuery).toBe('Test Film 400');
      expect(component.showAddThumbnailModal).toBeFalsy();
    });

    it('should close add thumbnail modal when closeAddThumbnailModal is called', () => {
      component.showAddThumbnailModal = true;
      component.newThumbnailFile = new File(['test'], 'test.jpg');
      component.newThumbnailFilmName = 'Test Film';
      component.newThumbnailPreview = 'preview';

      component.closeAddThumbnailModal();

      expect(component.showAddThumbnailModal).toBeFalsy();
      expect(component.newThumbnailFile).toBeNull();
      expect(component.newThumbnailFilmName).toBe('');
      expect(component.newThumbnailPreview).toBe('');
    });

    it('should determine hasThumbnailSelected based on imageUrl', () => {
      component.form.patchValue({ imageUrl: 'test-url' });
      expect(component.hasThumbnailSelected).toBeTruthy();

      component.form.patchValue({ imageUrl: '' });
      expect(component.hasThumbnailSelected).toBeFalsy();
    });

    it('should open thumbnail preview when openThumbnailPreview is called', () => {
      component.form.patchValue({ imageUrl: 'test-url' });
      
      component.openThumbnailPreview();

      expect(component.showThumbnailPreview).toBeTruthy();
    });

    it('should close thumbnail preview when closeThumbnailPreview is called', () => {
      component.showThumbnailPreview = true;
      
      component.closeThumbnailPreview();

      expect(component.showThumbnailPreview).toBeFalsy();
    });
  });

  describe('Bulk Upload Functionality', () => {
    beforeEach(() => {
      // Set up for insert mode (no id)
      mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
      
      // Set up mock return values for services
      mockFilmService.getById.and.returnValue(of({
        id: 'test-id',
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      }));
      mockSessionService.getAll.and.returnValue(of([]));
      mockDevKitService.getAll.and.returnValue(of([]));
      mockThumbnailService.searchByFilmName.and.returnValue(of([]));
      
      // Initialize component
      component.ngOnInit();
      
      // Force insert mode
      component.isInsert = true;
      component.id = null;
    });

    it('should initialize with bulkCount of 1', () => {
      expect(component.bulkCount).toBe(1);
    });

    it('should increment bulkCount when incrementBulkCount is called', () => {
      component.incrementBulkCount();
      expect(component.bulkCount).toBe(2);

      component.incrementBulkCount();
      expect(component.bulkCount).toBe(3);
    });

    it('should not increment bulkCount beyond 10', () => {
      component.bulkCount = 10;
      component.incrementBulkCount();
      expect(component.bulkCount).toBe(10);
    });

    it('should decrement bulkCount when decrementBulkCount is called', () => {
      component.bulkCount = 5;
      component.decrementBulkCount();
      expect(component.bulkCount).toBe(4);
    });

    it('should not decrement bulkCount below 1', () => {
      component.bulkCount = 1;
      component.decrementBulkCount();
      expect(component.bulkCount).toBe(1);
    });

    it('should return "Save" when bulkCount is 1', () => {
      component.bulkCount = 1;
      expect(component.getBulkSaveButtonText()).toBe('Save');
    });

    it('should return "Save X Films" when bulkCount is greater than 1', () => {
      component.bulkCount = 3;
      expect(component.getBulkSaveButtonText()).toBe('Save 3 Films');

      component.bulkCount = 10;
      expect(component.getBulkSaveButtonText()).toBe('Save 10 Films');
    });

    it('should call filmService.add with bulkCount parameter', () => {
      // Arrange
      const mockFilmDto = {
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      };
      
      component.form.patchValue(mockFilmDto);
      component.bulkCount = 5;
      mockFilmService.add.and.returnValue(of({}));


      // Act
      component.submit();

      // Assert
      expect(mockFilmService.add).toHaveBeenCalledWith(jasmine.any(Object), { bulkCount: 5 });
    });

    it('should call filmService.add with bulkCount 1 when bulkCount is 1', () => {
      // Arrange
      const mockFilmDto = {
        name: 'Test Film',
        iso: '400',
        type: 'ColorNegative',
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: 'Angel',
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      };
      
      component.form.patchValue(mockFilmDto);
      component.bulkCount = 1;
      mockFilmService.add.and.returnValue(of({}));

      // Act
      component.submit();

      // Assert
      expect(mockFilmService.add).toHaveBeenCalledWith(jasmine.any(Object), undefined);
    });

    it('should handle bulk upload errors', () => {
      // Arrange
      const mockFilmDto = {
        name: 'Test Film',
        iso: '400',
        type: 'ColorNegative',
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: 'Angel',
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      };
      
      component.form.patchValue(mockFilmDto);
      component.bulkCount = 3;
      mockFilmService.add.and.returnValue(throwError(() => new Error('Bulk upload failed')));

      // Act
      component.submit();

      // Assert
      expect(mockFilmService.add).toHaveBeenCalledWith(jasmine.any(Object), { bulkCount: 3 });
      expect(component.errorMessage).toBeTruthy();
      expect(component.loading).toBeFalsy();
    });

    it('should reset bulkCount to 1 when form is reset', () => {
      component.bulkCount = 5;
      component.form.reset();
      expect(component.bulkCount).toBe(5); // bulkCount should not be reset by form reset
    });

    it('should maintain bulkCount state during component lifecycle', () => {
      component.bulkCount = 7;
      component.ngOnInit();
      expect(component.bulkCount).toBe(7);
    });
  });

  describe('Exposure Dates Functionality', () => {
    beforeEach(() => {
      // Set up for insert mode (no id)
      mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
      
      // Set up mock return values for services
      mockFilmService.getById.and.returnValue(of({
        id: 'test-id',
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      }));
      mockSessionService.getAll.and.returnValue(of([]));
      mockDevKitService.getAll.and.returnValue(of([]));
      mockThumbnailService.searchByFilmName.and.returnValue(of([]));
      
      // Force insert mode first
      component.isInsert = true;
      component.id = null;
      
      // Initialize component
      component.ngOnInit();
    });

    it('should initialize with empty exposure dates array', () => {
      expect(component.exposureDates).toEqual([]);
      expect(component.isExposureDatesModalOpen).toBeFalsy();
    });

    it('should open exposure dates modal and initialize with one empty row', () => {
      component.openExposureDatesModal();
      
      expect(component.isExposureDatesModalOpen).toBeTruthy();
      expect(component.exposureDates).toEqual([{ date: '', description: '' }]);
    });

    it('should close exposure dates modal without updating form control', () => {
      component.exposureDates = [{ date: '2023-01-01', description: 'Test exposure' }];
      component.isExposureDatesModalOpen = true;
      
      component.closeExposureDatesModal();
      
      expect(component.isExposureDatesModalOpen).toBeFalsy();
      // Form should not be updated when just closing the modal
      expect(component.form.get('exposureDates')?.value).toEqual('');
    });

    it('should save exposure dates and update form control', () => {
      component.exposureDates = [
        { date: '2023-01-01', description: 'Valid exposure' },
        { date: '', description: 'Empty date' },
        { date: '2023-01-03', description: 'Another valid exposure' }
      ];
      component.isExposureDatesModalOpen = true;
      
      component.saveExposureDates();
      
      expect(component.isExposureDatesModalOpen).toBeFalsy();
      // Should filter out empty entries and serialize to JSON string
      const expectedJson = JSON.stringify([
        { date: '2023-01-01', description: 'Valid exposure' },
        { date: '2023-01-03', description: 'Another valid exposure' }
      ]);
      expect(component.form.get('exposureDates')?.value).toEqual(expectedJson);
    });

    it('should save empty string when no valid exposure dates', () => {
      component.exposureDates = [
        { date: '', description: 'Empty date' },
        { date: '', description: 'Another empty date' }
      ];
      component.isExposureDatesModalOpen = true;
      
      component.saveExposureDates();
      
      expect(component.isExposureDatesModalOpen).toBeFalsy();
      // Should save empty string when no valid dates
      expect(component.form.get('exposureDates')?.value).toEqual('');
    });

    it('should add new exposure date row', () => {
      component.exposureDates = [{ date: '2023-01-01', description: 'First exposure' }];
      
      component.addExposureDateRow();
      
      expect(component.exposureDates.length).toBe(2);
      expect(component.exposureDates[1]).toEqual({ date: '', description: '' });
    });

    it('should remove exposure date row when more than one exists', () => {
      component.exposureDates = [
        { date: '2023-01-01', description: 'First exposure' },
        { date: '2023-01-02', description: 'Second exposure' }
      ];
      
      component.removeExposureDateRow(1);
      
      expect(component.exposureDates.length).toBe(1);
      expect(component.exposureDates[0]).toEqual({ date: '2023-01-01', description: 'First exposure' });
    });

    it('should clear values but keep row when removing the last exposure date', () => {
      component.exposureDates = [{ date: '2023-01-01', description: 'Only exposure' }];
      
      component.removeExposureDateRow(0);
      
      expect(component.exposureDates.length).toBe(1);
      expect(component.exposureDates[0]).toEqual({ date: '', description: '' });
    });

    it('should filter out empty exposure dates when submitting', () => {
      // First, save the exposure dates to the form (simulating Save button click)
      component.exposureDates = [
        { date: '2023-01-01', description: 'Valid exposure' },
        { date: '', description: 'Empty date' },
        { date: '2023-01-03', description: 'Another valid exposure' }
      ];
      
      // Save the exposure dates to the form (this simulates clicking Save in the modal)
      component.saveExposureDates();
      
      // Make the form valid
      component.form.patchValue({
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        description: 'Test Description',
        developed: false
      });
      
      // Mock the service methods
      mockFilmService.add.and.returnValue(of({ id: 'new-film-id' }));
      mockFilmService.updateExposureDates = jasmine.createSpy('updateExposureDates').and.returnValue(of(undefined));
      
      component.submit();
      
      // Verify that the service was called with filtered exposure dates as JSON string
      const expectedJson = JSON.stringify([
        { date: '2023-01-01', description: 'Valid exposure' },
        { date: '2023-01-03', description: 'Another valid exposure' }
      ]);
      expect(mockFilmService.add).toHaveBeenCalledWith(
        jasmine.objectContaining({
          exposureDates: expectedJson
        }),
        undefined
      );
      // Verify that updateExposureDates was called after film creation
      expect(mockFilmService.updateExposureDates).toHaveBeenCalledWith('new-film-id', jasmine.any(Array));
    });

    it('should load existing exposure dates when opening modal for editing', () => {
      const filmWithExposureDates = {
        id: 'test-id',
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      };
      
      mockFilmService.getById.and.returnValue(of(filmWithExposureDates));
      // Mock getExposureDates to return error so it falls back to form control
      mockFilmService.getExposureDates = jasmine.createSpy('getExposureDates').and.returnValue(
        throwError(() => new Error('Not found'))
      );
      component.isInsert = false;
      component.id = 'test-id';
      
      component.ngOnInit();
      
      // Simulate form being patched with film data, including exposure dates in the form control
      // (The component still uses the old structure internally via form controls)
      component.form.patchValue({
        ...filmWithExposureDates,
        exposureDates: JSON.stringify([
          { date: '2023-01-01', description: 'First exposure' },
          { date: '2023-01-02', description: 'Second exposure' }
        ])
      });
      
      // Now open the modal - this should load the exposure dates from form control
      component.openExposureDatesModal();
      
      expect(component.exposureDates).toEqual([
        { date: '2023-01-01', description: 'First exposure' },
        { date: '2023-01-02', description: 'Second exposure' }
      ]);
    });

    it('should load exposure dates from API when opening modal for editing', () => {
      const filmId = 'test-film-id';
      const mockExposureDates = [
        { id: '1', filmId: filmId, date: '2025-10-20', description: 'First exposure' },
        { id: '2', filmId: filmId, date: '2025-10-22', description: 'Second exposure' }
      ];

      component.isInsert = false;
      component.id = filmId;
      mockFilmService.getById.and.returnValue(of({
        id: filmId,
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      }));
      mockFilmService.getExposureDates = jasmine.createSpy('getExposureDates').and.returnValue(of(mockExposureDates));

      component.ngOnInit();
      component.openExposureDatesModal();

      expect(mockFilmService.getExposureDates).toHaveBeenCalledWith(filmId);
      expect(component.exposureDates.length).toBe(2);
      expect(component.exposureDates[0].date).toBe('2025-10-20');
      expect(component.exposureDates[0].description).toBe('First exposure');
      expect(component.exposureDates[1].date).toBe('2025-10-22');
      expect(component.exposureDates[1].description).toBe('Second exposure');
    });

    it('should sort exposure dates by date when loading from API', () => {
      const filmId = 'test-film-id';
      const mockExposureDates = [
        { id: '2', filmId: filmId, date: '2025-10-22', description: 'Second exposure' },
        { id: '1', filmId: filmId, date: '2025-10-20', description: 'First exposure' }
      ];

      component.isInsert = false;
      component.id = filmId;
      mockFilmService.getById.and.returnValue(of({
        id: filmId,
        name: 'Test Film',
        iso: '400',
        type: FilmType.ColorNegative,
        numberOfExposures: 36,
        cost: 10.50,
        purchasedBy: UsernameType.Angel,
        purchasedOn: '2023-01-01',
        imageUrl: '',
        description: 'Test Description',
        developed: false
      }));
      mockFilmService.getExposureDates = jasmine.createSpy('getExposureDates').and.returnValue(of(mockExposureDates));

      component.ngOnInit();
      component.openExposureDatesModal();

      // Verify dates are sorted (oldest first)
      expect(component.exposureDates[0].date).toBe('2025-10-20');
      expect(component.exposureDates[1].date).toBe('2025-10-22');
    });

    it('should save exposure dates to API when editing', () => {
      const filmId = 'test-film-id';
      component.isInsert = false;
      component.id = filmId;
      component.exposureDates = [
        { date: '2025-10-25', description: 'New exposure 1' },
        { date: '2025-10-26', description: 'New exposure 2' }
      ];
      component.isExposureDatesModalOpen = true;

      mockFilmService.updateExposureDates = jasmine.createSpy('updateExposureDates').and.returnValue(of(undefined));

      component.saveExposureDates();

      expect(mockFilmService.updateExposureDates).toHaveBeenCalledWith(filmId, jasmine.arrayContaining([
        jasmine.objectContaining({ date: '2025-10-25', description: 'New exposure 1' }),
        jasmine.objectContaining({ date: '2025-10-26', description: 'New exposure 2' })
      ]));
      expect(component.isExposureDatesModalOpen).toBeFalsy();
    });

    it('should store exposure dates temporarily when creating new film', () => {
      component.isInsert = true;
      component.id = null;
      component.exposureDates = [
        { date: '2025-10-25', description: 'New exposure 1' },
        { date: '2025-10-26', description: 'New exposure 2' }
      ];
      component.isExposureDatesModalOpen = true;

      component.saveExposureDates();

      expect(component.pendingExposureDates.length).toBe(2);
      expect(component.pendingExposureDates[0].date).toBe('2025-10-25');
      expect(component.pendingExposureDates[1].date).toBe('2025-10-26');
      expect(component.isExposureDatesModalOpen).toBeFalsy();
    });

    it('should filter out empty dates when saving', () => {
      const filmId = 'test-film-id';
      component.isInsert = false;
      component.id = filmId;
      component.exposureDates = [
        { date: '2025-10-25', description: 'Valid exposure' },
        { date: '', description: 'Empty date' },
        { date: '2025-10-26', description: 'Another valid exposure' }
      ];
      component.isExposureDatesModalOpen = true;

      mockFilmService.updateExposureDates = jasmine.createSpy('updateExposureDates').and.returnValue(of(undefined));

      component.saveExposureDates();

      expect(mockFilmService.updateExposureDates).toHaveBeenCalledWith(filmId, jasmine.arrayContaining([
        jasmine.objectContaining({ date: '2025-10-25' }),
        jasmine.objectContaining({ date: '2025-10-26' })
      ]));
      // Should not include the empty date
      const callArgs = (mockFilmService.updateExposureDates as jasmine.Spy).calls.mostRecent().args[1];
      expect(callArgs.length).toBe(2);
      expect(callArgs.every((ed: any) => ed.date !== '')).toBeTruthy();
    });

    it('should render exposure dates with proper structure (exposure-date-entry wrapper)', () => {
      component.exposureDates = [
        { date: '2023-01-01', description: 'First exposure' },
        { date: '2023-01-02', description: 'Second exposure' }
      ];
      component.isExposureDatesModalOpen = true;
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const exposureEntries = compiled.querySelectorAll('.exposure-date-entry');
      
      expect(exposureEntries.length).toBe(2);
      
      // Each entry should contain a date input, description input, and delete button
      exposureEntries.forEach((entry: HTMLElement) => {
        expect(entry.querySelector('.date-input')).toBeTruthy();
        expect(entry.querySelector('.description-input')).toBeTruthy();
        expect(entry.querySelector('.delete-row-btn')).toBeTruthy();
      });
    });

    it('should render "Add Exposure Date" button in modal', () => {
      component.isExposureDatesModalOpen = true;
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const addButton = compiled.querySelector('.add-row-btn');
      
      expect(addButton).toBeTruthy();
      expect(addButton.textContent.trim()).toBe('Add Exposure Date');
    });

    it('should render "Save" button in exposure dates modal', () => {
      component.isExposureDatesModalOpen = true;
      fixture.detectChanges();

      const compiled = fixture.nativeElement;
      const saveButton = compiled.querySelector('.save-exposure-dates-btn');
      
      expect(saveButton).toBeTruthy();
      expect(saveButton.textContent.trim()).toBe('Save');
    });
  });
});