import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UpsertKitComponent } from '../../components/substances/upsert-kit/upsert-kit.component';
import { AccountService, DevKitService, UsedDevKitThumbnailService } from '../../services';
import { DevKitDto, IdentityDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertKitComponent', () => {
  let component: UpsertKitComponent;
  let fixture: ComponentFixture<UpsertKitComponent>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockThumbnailService: jasmine.SpyObj<UsedDevKitThumbnailService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getById', 'add', 'update', 'deleteById']);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const thumbnailServiceSpy = jasmine.createSpyObj('UsedDevKitThumbnailService', ['searchByDevKitName', 'uploadThumbnail']);
    const routerSpy = TestConfig.createRouterSpy();

    // Set up default return values to avoid subscription errors
    accountServiceSpy.whoAmI.and.returnValue(of({ username: 'testuser', email: 'test@example.com' }));
    devKitServiceSpy.getById.and.returnValue(of({} as DevKitDto));

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null) // Default to insert mode
        }
      }
    };

    await TestConfig.configureTestBed({
      declarations: [UpsertKitComponent],
      imports: [ReactiveFormsModule],
      providers: [
        FormBuilder,
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: UsedDevKitThumbnailService, useValue: thumbnailServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockDevKitService = devKitServiceSpy;
    mockAccountService = accountServiceSpy;
    mockThumbnailService = thumbnailServiceSpy;
    mockRouter = routerSpy;
    
    // Override the component to ensure proper dependency injection
    TestBed.overrideComponent(UpsertKitComponent, {
      set: {
        providers: []
      }
    });

    // Create fixture in beforeEach but allow tests to control the route setup before detectChanges
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    if (fixture) {
      fixture.destroy();
    }
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize in insert mode when no ID is provided', () => {
    // Arrange
    const mockIdentity: IdentityDto = { username: 'testuser', email: 'test@example.com' };
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges(); // This calls ngOnInit

    // Assert
    expect(component.isInsert).toBeTrue();
    expect(component.id).toBeNull();
    expect(mockAccountService.whoAmI).toHaveBeenCalled();
    expect(component.form.get('purchasedBy')?.value).toBe('testuser');
  });

  it('should initialize in edit mode and load kit when ID is provided', () => {
    // Arrange
    const testId = 'test-row-key';
    const mockKit: DevKitDto = {
      id: testId,
      name: 'Test Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Test kit description',
      expired: false,
      imageUrl: 'test-url',
    };

    // Set up mocks without route parameter to avoid constructor issues
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    
    // Create component first without route parameter
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;

    // Manually set up for edit mode after component creation
    component.id = testId;
    component.isInsert = false;

    // Act - Trigger component initialization
    fixture.detectChanges();

    // Assert
    expect(component.isInsert).toBeFalse();
    expect(component.id).toBe(testId);
  });


  it('should default mixedOn to empty for new kits', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture.detectChanges();
    expect(component.form.get('mixedOn')?.value).toBe('');
  });

  it('should send null mixedOn when saving unmixed substance', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    component.form.patchValue({
      name: 'Unmixed Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01',
      mixedOn: ''
    });
    mockDevKitService.add.and.returnValue(of({}));
    component.submit();
    const payload = mockDevKitService.add.calls.mostRecent().args[0] as DevKitDto;
    expect(payload.mixedOn).toBeNull();
  });

  it('should return expiration date tooltip in day - month - year when mixedOn and validForWeeks set', () => {
    fixture.detectChanges();
    component.form.patchValue({ mixedOn: '2026-01-01', validForWeeks: 4 });
    const tooltip = component.expirationDateTooltip;
    expect(tooltip).toBeTruthy();
    expect(tooltip).toMatch(/^\d+ - \d+ - 2026$/);
  });

  it('should return null expiration date tooltip when mixedOn empty', () => {
    fixture.detectChanges();
    component.form.patchValue({ mixedOn: '', validForWeeks: 6 });
    expect(component.expirationDateTooltip).toBeNull();
  });

  it('should not submit when form is invalid', () => {
    // Arrange
    component.form.patchValue({ name: '', url: '' }); // Invalid form

    // Act
    component.submit();

    // Assert
    expect(mockDevKitService.add).not.toHaveBeenCalled();
    expect(mockDevKitService.update).not.toHaveBeenCalled();
  });

  it('should add new kit when in insert mode', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // Initialize component in insert mode
    component.form.patchValue({
      name: 'New Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.add.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse(); // Should be false after completion
    expect(mockDevKitService.add).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
    expect(component.errorMessage).toBeNull();
  });

  it('should update existing kit when in edit mode', () => {
    // Arrange
    const testId = 'existing-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    // Set up for edit mode
    component.id = testId;
    component.isInsert = false;
    component.form.patchValue({
      name: 'Updated Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.update.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse();
    expect(mockDevKitService.update).toHaveBeenCalledWith(testId, jasmine.any(Object));
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
    expect(component.errorMessage).toBeNull();
  });

  it('should handle add kit error', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges(); // Initialize component in insert mode
    component.form.patchValue({
      name: 'New Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.add.and.returnValue(throwError(() => 'Add error'));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe('An unexpected error occurred. Please try again.');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should handle update kit error', () => {
    // Arrange
    const testId = 'existing-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    // Set up for edit mode
    component.id = testId;
    component.isInsert = false;
    component.form.patchValue({
      name: 'Updated Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.update.and.returnValue(throwError(() => 'Update error'));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe('An unexpected error occurred. Please try again.');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });



  it('should delete kit successfully', () => {
    // Arrange
    const testId = 'test-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    // Set up for edit mode
    component.id = testId;
    component.isInsert = false;
    mockDevKitService.deleteById.and.returnValue(of({}));

    // Act
    component.onDelete();

    // Assert
    expect(mockDevKitService.deleteById).toHaveBeenCalledWith(testId);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
  });

  it('should handle delete error', () => {
    // Arrange
    const testId = 'test-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    // Set up for edit mode
    component.id = testId;
    component.isInsert = false;
    mockDevKitService.deleteById.and.returnValue(throwError(() => 'Delete error'));

    // Act
    component.onDelete();

    // Assert
    expect(mockDevKitService.deleteById).toHaveBeenCalledWith(testId);
    expect(component.errorMessage).toBe('An unexpected error occurred. Please try again.');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
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
      expect(component.newThumbnailDevKitName).toBe('');
      expect(component.newThumbnailPreview).toBe('');
      expect(component.uploadingThumbnail).toBeFalsy();
    });

    it('should initialize thumbnail preview properties', () => {
      expect(component.showThumbnailPreview).toBeFalsy();
    });

    it('should perform thumbnail search when onThumbnailSearchClick is called', () => {
      const mockThumbnails = [
        { id: 'thumb1', devKitName: 'Bellini E6', imageId: 'img1', imageUrl: 'url1', imageBase64: '' },
        { id: 'thumb2', devKitName: 'Bellini C41', imageId: 'img2', imageUrl: 'url2', imageBase64: '' }
      ];
      mockThumbnailService.searchByDevKitName.and.returnValue(of(mockThumbnails));

      component.onThumbnailSearchClick();

      expect(mockThumbnailService.searchByDevKitName).toHaveBeenCalledWith('');
      expect(component.thumbnailSearchResults).toEqual(mockThumbnails);
      expect(component.showThumbnailDropdown).toBeTruthy();
    });

    it('should select thumbnail when onSelectThumbnail is called', () => {
      const mockThumbnail = { 
        id: 'thumb1', 
        devKitName: 'Bellini E6', 
        imageId: 'img1', 
        imageUrl: 'url1', 
      };

      component.onSelectThumbnail(mockThumbnail);

      expect(component.form.get('imageUrl')?.value).toBe('url1');
      expect(component.form.get('imageId')?.value).toBe('img1');
      expect(component.thumbnailSearchQuery).toBe('Bellini E6');
      expect(component.showThumbnailDropdown).toBeFalsy();
    });

    it('should determine canAddThumbnail based on devkit name', () => {
      component.form.patchValue({ name: 'Test DevKit' });
      expect(component.canAddThumbnail).toBeTruthy();

      component.form.patchValue({ name: '' });
      expect(component.canAddThumbnail).toBeFalsy();
    });

    it('should open add thumbnail modal when onAddNewThumbnail is called', () => {
      component.form.patchValue({ name: 'Test DevKit', type: DevKitType.E6 });
      
      component.onAddNewThumbnail();

      expect(component.showAddThumbnailModal).toBeTruthy();
      expect(component.newThumbnailDevKitName).toBe('Test DevKit E6');
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
      component.newThumbnailDevKitName = 'Test DevKit E6';
      
      const mockUploadedThumbnail = {
        id: 'thumb1',
        devKitName: 'Test DevKit E6',
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
      expect(component.thumbnailSearchQuery).toBe('Test DevKit E6');
      expect(component.showAddThumbnailModal).toBeFalsy();
    });

    it('should close add thumbnail modal when closeAddThumbnailModal is called', () => {
      component.showAddThumbnailModal = true;
      component.newThumbnailFile = new File(['test'], 'test.jpg');
      component.newThumbnailDevKitName = 'Test DevKit';
      component.newThumbnailPreview = 'preview';

      component.closeAddThumbnailModal();

      expect(component.showAddThumbnailModal).toBeFalsy();
      expect(component.newThumbnailFile).toBeNull();
      expect(component.newThumbnailDevKitName).toBe('');
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
