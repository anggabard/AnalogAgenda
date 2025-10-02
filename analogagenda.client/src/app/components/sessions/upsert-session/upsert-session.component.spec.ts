import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { UpsertSessionComponent } from './upsert-session.component';
import { SessionService, DevKitService, FilmService } from '../../../services';
import { SessionDto, DevKitDto, FilmDto } from '../../../DTOs';

describe('UpsertSessionComponent', () => {
  let component: UpsertSessionComponent;
  let fixture: ComponentFixture<UpsertSessionComponent>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockDevKits: DevKitDto[] = [
    {
      rowKey: 'devkit1',
      name: 'Bellini C41',
      type: 'C41',
      expired: false,
      imageUrl: '',
      imageBase64: '',
      filmsDeveloped: 5
    },
    {
      rowKey: 'devkit2',
      name: 'Bellini E6',
      type: 'E6',
      expired: true,
      imageUrl: '',
      imageBase64: '',
      filmsDeveloped: 2
    }
  ];

  const mockFilms: FilmDto[] = [
    {
      rowKey: 'film1',
      name: 'Test Film 1',
      type: 'Color Negative',
      iso: 400,
      developed: false,
      purchasedBy: 'Angel',
      imageUrl: '',
      imageBase64: ''
    },
    {
      rowKey: 'film2',
      name: 'Test Film 2',
      type: 'Color Negative',
      iso: 200,
      developed: false,
      purchasedBy: 'Tudor',
      imageUrl: '',
      imageBase64: ''
    }
  ];

  beforeEach(async () => {
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['add', 'update', 'getById']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getAll']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null)
        }
      }
    };

    await TestBed.configureTestingModule({
      declarations: [UpsertSessionComponent],
      imports: [ReactiveFormsModule],
      providers: [
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
    mockSessionService = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;
    mockDevKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Setup service returns
    mockDevKitService.getAll.and.returnValue(of(mockDevKits));
    mockFilmService.getAll.and.returnValue(of(mockFilms));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with default values for new session', () => {
    // Act
    component.ngOnInit();

    // Assert
    expect(component.formGroup.get('sessionDate')?.value).toBeTruthy();
    expect(component.formGroup.get('location')?.value).toBe('');
    expect(component.formGroup.get('participants')?.value).toEqual([]);
    expect(component.formGroup.get('usedSubstances')?.value).toEqual([]);
    expect(component.formGroup.get('developedFilms')?.value).toEqual([]);
  });

  it('should load available devkits and films on init', () => {
    // Act
    component.ngOnInit();

    // Assert
    expect(mockDevKitService.getAll).toHaveBeenCalled();
    expect(mockFilmService.getAll).toHaveBeenCalled();
    expect(component.availableDevKits).toEqual(mockDevKits);
    expect(component.availableFilms).toEqual(mockFilms.filter(f => !f.developed));
  });

  it('should filter expired devkits by default', () => {
    // Arrange
    component.ngOnInit();

    // Act
    const filteredDevKits = component.filteredDevKits;

    // Assert
    expect(filteredDevKits.length).toBe(1);
    expect(filteredDevKits[0].rowKey).toBe('devkit1');
    expect(filteredDevKits.some(dk => dk.expired)).toBe(false);
  });

  it('should show expired devkits when showExpiredDevKits is true', () => {
    // Arrange
    component.ngOnInit();
    component.showExpiredDevKits = true;

    // Act
    const filteredDevKits = component.filteredDevKits;

    // Assert
    expect(filteredDevKits.length).toBe(2);
    expect(filteredDevKits.some(dk => dk.expired)).toBe(true);
  });

  it('should handle participant selection', () => {
    // Arrange
    component.ngOnInit();
    const mockEvent = { target: { checked: true } } as any;

    // Act
    component.onParticipantChange('Angel', mockEvent);

    // Assert
    expect(component.selectedParticipants).toContain('Angel');
    expect(component.formGroup.get('participants')?.value).toContain('Angel');
  });

  it('should handle participant deselection', () => {
    // Arrange
    component.ngOnInit();
    component.selectedParticipants = ['Angel', 'Tudor'];
    const mockEvent = { target: { checked: false } } as any;

    // Act
    component.onParticipantChange('Angel', mockEvent);

    // Assert
    expect(component.selectedParticipants).not.toContain('Angel');
    expect(component.selectedParticipants).toContain('Tudor');
  });

  it('should toggle devkit selection', () => {
    // Arrange
    component.ngOnInit();

    // Act
    component.toggleDevKit('devkit1');

    // Assert
    expect(component.selectedDevKits).toContain('devkit1');
    expect(component.formGroup.get('usedSubstances')?.value).toContain('devkit1');

    // Act again to toggle off
    component.toggleDevKit('devkit1');

    // Assert
    expect(component.selectedDevKits).not.toContain('devkit1');
    expect(component.formGroup.get('usedSubstances')?.value).not.toContain('devkit1');
  });

  it('should toggle film selection', () => {
    // Arrange
    component.ngOnInit();

    // Act
    component.toggleFilm('film1');

    // Assert
    expect(component.selectedFilms).toContain('film1');
    expect(component.formGroup.get('developedFilms')?.value).toContain('film1');

    // Act again to toggle off
    component.toggleFilm('film1');

    // Assert
    expect(component.selectedFilms).not.toContain('film1');
    expect(component.formGroup.get('developedFilms')?.value).not.toContain('film1');
  });

  it('should submit form and create new session', () => {
    // Arrange
    component.ngOnInit();
    component.selectedParticipants = ['Angel', 'Tudor'];
    component.selectedDevKits = ['devkit1'];
    component.selectedFilms = ['film1'];
    
    component.formGroup.patchValue({
      sessionDate: '2025-10-02',
      location: 'Test Location',
      description: 'Test description'
    });

    const mockResponse: SessionDto = {
      rowKey: 'created-session',
      sessionDate: '2025-10-02',
      location: 'Test Location',
      participants: '["Angel","Tudor"]',
      description: 'Test description',
      usedSubstances: '["devkit1"]',
      developedFilms: '["film1"]',
      imageUrl: '',
      imageBase64: ''
    };

    mockSessionService.add.and.returnValue(of(mockResponse));

    // Act
    component.submit();

    // Assert
    expect(mockSessionService.add).toHaveBeenCalledWith(jasmine.objectContaining({
      sessionDate: '2025-10-02',
      location: 'Test Location',
      participants: '["Angel","Tudor"]',
      usedSubstances: '["devkit1"]',
      developedFilms: '["film1"]'
    }));
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/sessions', 'created-session']);
  });

  it('should not submit invalid form', () => {
    // Arrange
    component.ngOnInit();
    // Leave required fields empty

    // Act
    component.submit();

    // Assert
    expect(mockSessionService.add).not.toHaveBeenCalled();
    expect(mockSessionService.update).not.toHaveBeenCalled();
  });

  it('should validate required participants', () => {
    // Arrange
    component.ngOnInit();
    component.formGroup.patchValue({
      sessionDate: '2025-10-02',
      location: 'Test Location'
    });
    // No participants selected

    // Act & Assert
    expect(component.formGroup.invalid).toBe(true);
    expect(component.selectedParticipants.length).toBe(0);
  });
});
