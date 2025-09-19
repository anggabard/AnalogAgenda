import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UpsertKitComponent } from '../../components/substances/upsert-kit/upsert-kit.component';
import { AccountService, DevKitService } from '../../services';
import { DevKitDto, IdentityDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';

describe('UpsertKitComponent', () => {
  let component: UpsertKitComponent;
  let fixture: ComponentFixture<UpsertKitComponent>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getKit', 'addNewKit', 'updateKit', 'deleteKit']);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    // Set up default return values to avoid subscription errors
    accountServiceSpy.whoAmI.and.returnValue(of({ username: 'testuser', email: 'test@example.com' }));
    devKitServiceSpy.getKit.and.returnValue(of({} as DevKitDto));

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null) // Default to insert mode
        }
      }
    };

    await TestBed.configureTestingModule({
      declarations: [UpsertKitComponent],
      imports: [ReactiveFormsModule],
      providers: [
        FormBuilder,
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;
    mockDevKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    mockAccountService = TestBed.inject(AccountService) as jasmine.SpyObj<AccountService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  afterEach(() => {
    fixture.destroy();
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
    fixture.detectChanges(); // This calls constructor

    // Assert
    expect(component.isInsert).toBeTrue();
    expect(component.rowKey).toBeNull();
    expect(mockAccountService.whoAmI).toHaveBeenCalled();
    expect(component.form.get('purchasedBy')?.value).toBe('testuser');
  });

  it('should initialize in edit mode and load kit when ID is provided', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    const mockKit: DevKitDto = {
      rowKey: testRowKey,
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
      imageBase64: ''
    };

    // Set up mocks BEFORE component creation
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockDevKitService.getKit.and.returnValue(of(mockKit));

    // Create new component instance with edit mode setup
    fixture = TestBed.createComponent(UpsertKitComponent);
    component = fixture.componentInstance;

    // Act - Trigger component initialization
    fixture.detectChanges();

    // Assert
    expect(component.isInsert).toBeFalse();
    expect(component.rowKey).toBe(testRowKey);
    expect(mockDevKitService.getKit).toHaveBeenCalledWith(testRowKey);
    expect(component.originalName).toBe('Test Kit');
  });


  it('should not submit when form is invalid', () => {
    // Arrange
    component.form.patchValue({ name: '', url: '' }); // Invalid form

    // Act
    component.submit();

    // Assert
    expect(mockDevKitService.addNewKit).not.toHaveBeenCalled();
    expect(mockDevKitService.updateKit).not.toHaveBeenCalled();
  });

  it('should add new kit when in insert mode', () => {
    // Arrange
    component.isInsert = true;
    component.form.patchValue({
      name: 'New Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.addNewKit.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse(); // Should be false after completion
    expect(mockDevKitService.addNewKit).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
    expect(component.errorMessage).toBeNull();
  });

  it('should update existing kit when in edit mode', () => {
    // Arrange
    component.isInsert = false;
    component.rowKey = 'existing-key';
    component.form.patchValue({
      name: 'Updated Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.updateKit.and.returnValue(of({}));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse();
    expect(mockDevKitService.updateKit).toHaveBeenCalledWith('existing-key', jasmine.any(Object));
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
    expect(component.errorMessage).toBeNull();
  });

  it('should handle add kit error', () => {
    // Arrange
    component.isInsert = true;
    component.form.patchValue({
      name: 'New Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.addNewKit.and.returnValue(throwError(() => 'Add error'));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe('There was an error saving the new Kit.');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should handle update kit error', () => {
    // Arrange
    component.isInsert = false;
    component.rowKey = 'existing-key';
    component.form.patchValue({
      name: 'Updated Kit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: 'testuser',
      purchasedOn: '2023-01-01'
    });
    mockDevKitService.updateKit.and.returnValue(throwError(() => 'Update error'));

    // Act
    component.submit();

    // Assert
    expect(component.loading).toBeFalse();
    expect(component.errorMessage).toBe('There was an error updating the Kit.');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('should handle image selection', () => {
    // Arrange
    const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
    const mockFileReader = {
      readAsDataURL: jasmine.createSpy('readAsDataURL'),
      result: 'data:image/jpeg;base64,testdata',
      onload: null as any
    };
    spyOn(window, 'FileReader').and.returnValue(mockFileReader as any);

    const mockEvent = {
      target: {
        files: [mockFile]
      }
    } as any;

    // Act
    component.onImageSelected(mockEvent);
    mockFileReader.onload(); // Simulate FileReader onload

    // Assert
    expect(mockFileReader.readAsDataURL).toHaveBeenCalledWith(mockFile);
    expect(component.form.get('imageBase64')?.value).toBe('data:image/jpeg;base64,testdata');
  });


  it('should delete kit successfully', () => {
    // Arrange
    component.rowKey = 'test-key';
    mockDevKitService.deleteKit.and.returnValue(of({}));

    // Act
    component.onDelete();

    // Assert
    expect(mockDevKitService.deleteKit).toHaveBeenCalledWith('test-key');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/substances']);
  });

  it('should handle delete error', () => {
    // Arrange
    component.rowKey = 'test-key';
    mockDevKitService.deleteKit.and.returnValue(throwError(() => 'Delete error'));

    // Act
    component.onDelete();

    // Assert
    expect(mockDevKitService.deleteKit).toHaveBeenCalledWith('test-key');
    expect(component.errorMessage).toBe('There was an error updating the Kit.');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });


});
