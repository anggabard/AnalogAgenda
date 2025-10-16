import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { UpsertFilmComponent } from '../../components/films/upsert-film/upsert-film.component';
import { FilmService, SessionService, DevKitService, PhotoService, UsedFilmThumbnailService } from '../../services';
import { DevKitType, UsernameType } from '../../enums';
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
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getById', 'update', 'create']);
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'update', 'getAll']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getById', 'update', 'getAll']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', ['getAll', 'upload']);
    const thumbnailServiceSpy = jasmine.createSpyObj('UsedFilmThumbnailService', ['searchByFilmName', 'uploadThumbnail']);
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

  it('should set selectedSessionRowKey when selectSession is called', () => {
    const sessionRowKey = 'test-session-key';
    component.selectSession(sessionRowKey);
    expect(component.selectedSessionRowKey).toBe(sessionRowKey);
  });

  it('should set selectedDevKitRowKey when selectDevKit is called', () => {
    const devKitRowKey = 'test-devkit-key';
    component.selectDevKit(devKitRowKey);
    expect(component.selectedDevKitRowKey).toBe(devKitRowKey);
  });

  it('should filter expired devkits when showExpiredDevKits is false', () => {
    const mockDevKits = [
      { rowKey: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { rowKey: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.showExpiredDevKits = false;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual([mockDevKits[0]]);
  });

  it('should show all devkits when showExpiredDevKits is true', () => {
    const mockDevKits = [
      { rowKey: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { rowKey: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;
    component.showExpiredDevKits = true;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual(mockDevKits);
  });

  it('should determine hasExpiredDevKits correctly', () => {
    const mockDevKits = [
      { rowKey: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { rowKey: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: true, imageBase64: '', imageUrl: '' }
    ];
    component.availableDevKits = mockDevKits;

    expect(component.hasExpiredDevKits).toBeTruthy();
  });

  it('should return false for hasExpiredDevKits when no expired devkits', () => {
    const mockDevKits = [
      { rowKey: 'devkit-1', name: 'DevKit 1', url: '', type: DevKitType.C41, purchasedBy: UsernameType.Angel, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' },
      { rowKey: 'devkit-2', name: 'DevKit 2', url: '', type: DevKitType.BW, purchasedBy: UsernameType.Tudor, purchasedOn: '', mixedOn: '', validForWeeks: 4, validForFilms: 10, filmsDeveloped: 0, description: '', expired: false, imageBase64: '', imageUrl: '' }
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
        { rowKey: 'thumb1', filmName: 'Kodak Portra 400', imageId: 'img1', imageUrl: 'url1', imageBase64: '' },
        { rowKey: 'thumb2', filmName: 'Fuji Superia 200', imageId: 'img2', imageUrl: 'url2', imageBase64: '' }
      ];
      mockThumbnailService.searchByFilmName.and.returnValue(of(mockThumbnails));

      component.onThumbnailSearchClick();

      expect(mockThumbnailService.searchByFilmName).toHaveBeenCalledWith('');
      expect(component.thumbnailSearchResults).toEqual(mockThumbnails);
      expect(component.showThumbnailDropdown).toBeTruthy();
    });

    it('should select thumbnail when onSelectThumbnail is called', () => {
      const mockThumbnail = { 
        rowKey: 'thumb1', 
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
        rowKey: 'thumb1',
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
});