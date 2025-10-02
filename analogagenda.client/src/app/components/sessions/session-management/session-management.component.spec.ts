import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { SessionManagementComponent } from './session-management.component';
import { SessionService, DevKitService, FilmService } from '../../../services';
import { SessionDto, DevKitDto, FilmDto } from '../../../DTOs';
import { CdkDragDrop } from '@angular/cdk/drag-drop';

describe('SessionManagementComponent', () => {
  let component: SessionManagementComponent;
  let fixture: ComponentFixture<SessionManagementComponent>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockSession: SessionDto = {
    rowKey: 'test-session',
    sessionDate: '2025-10-02',
    location: 'Test Location',
    participants: '["Angel", "Tudor"]',
    description: 'Test description',
    usedSubstances: '["devkit1"]',
    developedFilms: '["film1"]',
    imageUrl: '',
    imageBase64: ''
  };

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
      expired: false,
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
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'update', 'add']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getAll']);
    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue('test-session')
        },
        queryParams: { edit: 'true' }
      }
    };

    await TestBed.configureTestingModule({
      declarations: [SessionManagementComponent],
      providers: [
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SessionManagementComponent);
    component = fixture.componentInstance;
    mockSessionService = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;
    mockDevKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;

    // Setup service returns
    mockSessionService.getById.and.returnValue(of(mockSession));
    mockDevKitService.getAll.and.returnValue(of(mockDevKits));
    mockFilmService.getAll.and.returnValue(of(mockFilms));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize in edit mode when edit query parameter is true', () => {
    // Act
    component.ngOnInit();

    // Assert
    expect(component.isEditMode).toBe(true);
    expect(component.isViewMode).toBe(false);
  });

  it('should load session data on init', () => {
    // Act
    component.ngOnInit();

    // Assert
    expect(mockSessionService.getById).toHaveBeenCalledWith('test-session');
    expect(mockDevKitService.getAll).toHaveBeenCalled();
    expect(mockFilmService.getAll).toHaveBeenCalled();
  });

  it('should add devkit to session', () => {
    // Arrange
    component.ngOnInit();
    const devKitToAdd = mockDevKits[1]; // devkit2

    // Act
    component.addDevKitToSession(devKitToAdd);

    // Assert
    expect(component.sessionDevKits.length).toBe(2); // Should have devkit1 from session + devkit2 added
    expect(component.sessionDevKits.some(sdk => sdk.devKit.rowKey === 'devkit2')).toBe(true);
    expect(component.availableDevKits.some(dk => dk.rowKey === 'devkit2')).toBe(false);
  });

  it('should remove devkit from session', () => {
    // Arrange
    component.ngOnInit();
    component.sessionDevKits = [{
      devKit: mockDevKits[0],
      assignedFilms: [mockFilms[0]]
    }];
    component.availableDevKits = [mockDevKits[1]];

    // Act
    component.removeDevKitFromSession('devkit1');

    // Assert
    expect(component.sessionDevKits.length).toBe(0);
    expect(component.availableDevKits.length).toBe(2);
    expect(component.unassignedFilms.some(f => f.rowKey === 'film1')).toBe(true);
  });

  it('should add film to session', () => {
    // Arrange
    component.ngOnInit();
    const filmToAdd = mockFilms[1]; // film2

    // Act
    component.addFilmToSession(filmToAdd);

    // Assert
    expect(component.unassignedFilms.some(f => f.rowKey === 'film2')).toBe(true);
    expect(component.availableUnassignedFilms.some(f => f.rowKey === 'film2')).toBe(false);
  });

  it('should remove film from session', () => {
    // Arrange
    component.ngOnInit();
    component.unassignedFilms = [mockFilms[0]];
    component.availableUnassignedFilms = [mockFilms[1]];

    // Act
    component.removeFilmFromSession('film1');

    // Assert
    expect(component.unassignedFilms.some(f => f.rowKey === 'film1')).toBe(false);
    expect(component.availableUnassignedFilms.length).toBe(2);
    expect(component.availableUnassignedFilms.some(f => f.rowKey === 'film1')).toBe(true);
  });

  it('should toggle edit mode', () => {
    // Arrange
    component.ngOnInit();
    const initialEditMode = component.isEditMode;

    // Act
    component.toggleEditMode();

    // Assert
    expect(component.isEditMode).toBe(!initialEditMode);
    expect(component.isViewMode).toBe(initialEditMode);
    expect(mockRouter.navigate).toHaveBeenCalled();
  });

  it('should save session with correct data', () => {
    // Arrange
    component.ngOnInit();
    component.sessionDevKits = [{
      devKit: mockDevKits[0],
      assignedFilms: [mockFilms[0]]
    }];
    component.unassignedFilms = [mockFilms[1]];
    mockSessionService.update.and.returnValue(of({}));

    // Act
    component.saveSession();

    // Assert
    expect(mockSessionService.update).toHaveBeenCalledWith('test-session', jasmine.objectContaining({
      usedSubstances: '["devkit1"]',
      developedFilms: '["film2","film1"]'
    }));
  });

  it('should handle drag and drop between containers', () => {
    // Arrange
    component.ngOnInit();
    component.sessionDevKits = [{
      devKit: mockDevKits[0],
      assignedFilms: []
    }];
    component.unassignedFilms = [mockFilms[0]];

    const mockEvent = {
      previousContainer: { data: component.unassignedFilms },
      container: { data: component.sessionDevKits[0].assignedFilms },
      previousIndex: 0,
      currentIndex: 0
    } as CdkDragDrop<FilmDto[]>;

    // Act
    component.onFilmDrop(mockEvent);

    // Assert
    expect(component.unassignedFilms.length).toBe(0);
    expect(component.sessionDevKits[0].assignedFilms.length).toBe(1);
    expect(component.sessionDevKits[0].assignedFilms[0].rowKey).toBe('film1');
  });
});
