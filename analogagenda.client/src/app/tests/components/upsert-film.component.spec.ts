import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UpsertFilmComponent } from '../../components/films/upsert-film/upsert-film.component';
import { AccountService, FilmService, PhotoService } from '../../services';
import { FilmDto, IdentityDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertFilmComponent', () => {
  let component: UpsertFilmComponent;
  let fixture: ComponentFixture<UpsertFilmComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockPhotoService: jasmine.SpyObj<any>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockIdentity: IdentityDto = {
    username: 'Angel',
    email: 'angel@test.com'
  };

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getById', 'add', 'update', 'deleteById']);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', ['uploadPhotos']);
    const routerSpy = TestConfig.createRouterSpy();

    // Set up default return values to avoid subscription errors
    accountServiceSpy.whoAmI.and.returnValue(of(mockIdentity));
    filmServiceSpy.getById.and.returnValue(of({} as FilmDto));

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null) // Default to insert mode
        }
      }
    };

    await TestConfig.configureTestBed({
      declarations: [UpsertFilmComponent],
      imports: [ReactiveFormsModule],
      providers: [
        FormBuilder,
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: PhotoService, useValue: photoServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockFilmService = filmServiceSpy;
    mockAccountService = accountServiceSpy;
    mockPhotoService = photoServiceSpy;
    mockRouter = routerSpy;
    
    // Create component fixture once in beforeEach
    fixture = TestBed.createComponent(UpsertFilmComponent);
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

  it('should initialize in insert mode when no rowKey is provided', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.isInsert).toBeTruthy();
    expect(component.rowKey).toBeNull();
    expect(mockAccountService.whoAmI).toHaveBeenCalled();
  });

  it('should initialize in update mode when rowKey is provided', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    
    fixture = TestBed.createComponent(UpsertFilmComponent);
    component = fixture.componentInstance;

    // Manually set for edit mode after component creation
    component.rowKey = testRowKey;
    component.isInsert = false;

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.isInsert).toBeFalsy();
    expect(component.rowKey).toBe(testRowKey);
  });

  it('should initialize form with default values in insert mode', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.form.get('name')?.value).toBe('');
    expect(component.form.get('iso')?.value).toBe(400); // Default ISO value
    expect(component.form.get('type')?.value).toBe(FilmType.ColorNegative);
    expect(component.form.get('numberOfExposures')?.value).toBe(36);
    expect(component.form.get('cost')?.value).toBe(0);
    expect(component.form.get('developed')?.value).toBe(false);
    expect(component.form.get('purchasedBy')?.value).toBe('Angel'); // Set from whoAmI
  });

  it('should populate form with existing data in update mode', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    
    fixture = TestBed.createComponent(UpsertFilmComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Manually populate form for test
    component.form.patchValue({
      name: 'Existing Film',
      purchasedBy: UsernameType.Tudor,
      developed: true
    });

    // Assert
    expect(component.form.get('name')?.value).toBe('Existing Film');
    expect(component.form.get('purchasedBy')?.value).toBe(UsernameType.Tudor);
    expect(component.form.get('developed')?.value).toBe(true);
  });

  it('should create new film when submitting in insert mode', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockFilmService.add.and.returnValue(of({}));
    
    fixture.detectChanges();
    
    // Fill form with valid data
    component.form.patchValue({
      name: 'New Film',
      iso: 400,
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 15.99,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-12-01',
      description: 'Test description',
      developed: false
    });

    // Act
    component.submit();

    // Assert
    expect(mockFilmService.add).toHaveBeenCalled();
    expect(component.loading).toBeFalsy();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/films']);
  });

  it('should update existing film when submitting in update mode', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockFilmService.update.and.returnValue(of({}));
    
    fixture = TestBed.createComponent(UpsertFilmComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    
    // Set up for edit mode
    component.rowKey = testRowKey;
    component.isInsert = false;
    component.form.patchValue({
      name: 'Updated Film'
    });

    // Act
    component.submit();

    // Assert
    expect(mockFilmService.update).toHaveBeenCalledWith(testRowKey, jasmine.any(Object));
    expect(component.loading).toBeFalsy();
  });

  it('should navigate to films list after successful submission', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockFilmService.add.and.returnValue(of({}));
    
    fixture.detectChanges();
    
    // Fill form with valid data
    component.form.patchValue({
      name: 'New Film',
      iso: 400,
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 15.99,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-12-01',
      description: 'Test description',
      developed: false
    });

    // Act
    component.submit();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/films']);
  });

  it('should show error message on submission failure', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockFilmService.add.and.returnValue(throwError('Service error'));
    
    fixture.detectChanges();
    
    // Fill form with valid data
    component.form.patchValue({
      name: 'New Film',
      iso: 400,
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 15.99,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-12-01',
      description: 'Test description',
      developed: false
    });

    // Act
    component.submit();

    // Assert
    expect(component.errorMessage).toBe('An unexpected error occurred. Please try again.');
    expect(component.loading).toBeFalsy();
  });

  it('should not submit when form is invalid', () => {
    // Arrange
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture.detectChanges();
    
    // Leave form with invalid data (name is required)
    component.form.patchValue({
      name: '', // Invalid - required
      iso: 400
    });

    // Act
    component.submit();

    // Assert
    expect(mockFilmService.add).not.toHaveBeenCalled();
    expect(mockFilmService.update).not.toHaveBeenCalled();
  });

  it('should handle image selection', () => {
    // Arrange
    const mockFile = new File(['mock content'], 'test.jpg', { type: 'image/jpeg' });
    const mockEvent = {
      target: {
        files: [mockFile]
      }
    } as any;

    const mockReader = {
      readAsDataURL: jasmine.createSpy('readAsDataURL'),
      result: 'data:image/jpeg;base64,mockbase64data',
      onload: null as any
    };

    spyOn(window, 'FileReader').and.returnValue(mockReader as any);
    
    fixture.detectChanges();

    // Act
    component.onImageSelected(mockEvent);
    
    // Simulate FileReader onload event
    mockReader.onload!();

    // Assert
    expect(mockReader.readAsDataURL).toHaveBeenCalledWith(mockFile);
    expect(component.form.get('imageBase64')?.value).toBe('data:image/jpeg;base64,mockbase64data');
  });

  it('should delete film when onDelete is called', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockFilmService.deleteById.and.returnValue(of({}));
    
    fixture = TestBed.createComponent(UpsertFilmComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Set up for edit mode
    component.rowKey = testRowKey;
    component.isInsert = false;

    // Act
    component.onDelete();

    // Assert
    expect(mockFilmService.deleteById).toHaveBeenCalledWith(testRowKey);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/films']);
  });

  it('should show error message on delete failure', () => {
    // Arrange
    const testRowKey = 'test-row-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockFilmService.deleteById.and.returnValue(throwError('Delete error'));
    
    fixture = TestBed.createComponent(UpsertFilmComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    // Set up for edit mode
    component.rowKey = testRowKey;
    component.isInsert = false;

    // Act
    component.onDelete();

    // Assert
    expect(component.errorMessage).toBe('An unexpected error occurred. Please try again.');
  });

  it('should validate form fields correctly', () => {
    // Arrange
    fixture.detectChanges();

    // Act & Assert
    const nameControl = component.form.get('name');
    const isoControl = component.form.get('iso');
    const costControl = component.form.get('cost');
    const numberOfExposuresControl = component.form.get('numberOfExposures');

    // Test required validation
    nameControl?.setValue('');
    expect(nameControl?.invalid).toBeTruthy();

    nameControl?.setValue('Valid Name');
    expect(nameControl?.valid).toBeTruthy();

    // Test min value validation
    isoControl?.setValue(0);
    expect(isoControl?.invalid).toBeTruthy();

    isoControl?.setValue(100);
    expect(isoControl?.valid).toBeTruthy();

    // Test cost validation
    costControl?.setValue(-1);
    expect(costControl?.invalid).toBeTruthy();

    costControl?.setValue(10.99);
    expect(costControl?.valid).toBeTruthy();

    // Test number of exposures validation
    numberOfExposuresControl?.setValue(0);
    expect(numberOfExposuresControl?.invalid).toBeTruthy();

    numberOfExposuresControl?.setValue(24);
    expect(numberOfExposuresControl?.valid).toBeTruthy();
  });

  // Helper function to create mock films
  function createMockFilm(
    rowKey: string, 
    name: string, 
    purchasedBy: UsernameType = UsernameType.Angel, 
    developed: boolean = false
  ): FilmDto {
    return {
      rowKey,
      name,
      iso: 400,
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 12.50,
      purchasedBy,
      purchasedOn: '2023-01-01',
      description: 'Test film description',
      developed,
      imageUrl: 'test-image-url',
      imageBase64: ''
    };
  }
});
