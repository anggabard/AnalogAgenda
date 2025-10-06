import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { UpsertSessionComponent } from '../../components/sessions/upsert-session/upsert-session.component';
import { SessionService, DevKitService, FilmService } from '../../services';
import { SessionDto, DevKitDto, FilmDto } from '../../DTOs';
import { DevKitType, FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertSessionComponent', () => {
  let component: UpsertSessionComponent;
  let fixture: ComponentFixture<UpsertSessionComponent>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  const mockDevKit: DevKitDto = {
    rowKey: 'devkit-1',
    name: 'Test DevKit',
    url: 'http://example.com',
    type: DevKitType.C41,
    purchasedBy: UsernameType.Angel,
    purchasedOn: '2023-01-01',
    mixedOn: '2023-01-01',
    validForWeeks: 6,
    validForFilms: 8,
    filmsDeveloped: 0,
    description: 'Test devkit',
    expired: false,
    imageUrl: 'test-url',
    imageBase64: ''
  };

  const mockFilm: FilmDto = {
    rowKey: 'film-1',
    name: 'Test Film',
    iso: 400,
    type: FilmType.ColorNegative,
    numberOfExposures: 36,
    cost: 12.50,
    purchasedBy: UsernameType.Angel,
    purchasedOn: '2023-01-01',
    description: 'Test film',
    developed: true,
    imageUrl: 'test-url',
    imageBase64: ''
  };

  const mockSession: SessionDto = {
    rowKey: 'session-1',
    sessionDate: '2023-10-01',
    location: 'Test Location',
    participants: '["Angel"]',
    imageUrl: 'test-session-url',
    imageBase64: '',
    description: 'Test session',
    usedSubstances: '[]',
    developedFilms: '[]',
    participantsList: ['Angel'],
    usedSubstancesList: [],
    developedFilmsList: []
  };

  beforeEach(async () => {
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'add', 'update', 'deleteById']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getAll']);
    const routerSpy = TestConfig.createRouterSpy();

    sessionServiceSpy.getById.and.returnValue(of(mockSession));
    devKitServiceSpy.getAll.and.returnValue(of([mockDevKit]));
    filmServiceSpy.getAll.and.returnValue(of([mockFilm]));

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: jasmine.createSpy('get').and.returnValue(null)
        },
        queryParams: {}
      }
    };

    await TestConfig.configureTestBed({
      declarations: [UpsertSessionComponent],
      imports: [ReactiveFormsModule],
      providers: [
        FormBuilder,
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    mockSessionService = sessionServiceSpy;
    mockDevKitService = devKitServiceSpy;
    mockFilmService = filmServiceSpy;
    mockRouter = routerSpy;

    fixture = TestBed.createComponent(UpsertSessionComponent);
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
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture.detectChanges();

    expect(component.isInsert).toBeTruthy();
    expect(component.rowKey).toBeNull();
    expect(component.isViewMode).toBeFalsy();
    expect(component.isEditMode).toBeFalsy();
  });

  it('should initialize in view mode when rowKey is provided without edit query param', () => {
    const testRowKey = 'test-session-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockActivatedRoute.snapshot.queryParams = {};

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.isInsert).toBeFalsy();
    expect(component.isViewMode).toBeTruthy();
    expect(component.isEditMode).toBeFalsy();
  });

  it('should initialize in edit mode when rowKey is provided with edit=true query param', () => {
    const testRowKey = 'test-session-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockActivatedRoute.snapshot.queryParams = { edit: 'true' };

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.isInsert).toBeFalsy();
    expect(component.isViewMode).toBeFalsy();
    expect(component.isEditMode).toBeTruthy();
  });

  it('should initialize form with default values in insert mode', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture.detectChanges();

    expect(component.form.get('location')?.value).toBe('');
    expect(component.form.get('participants')?.value).toEqual([]);
    expect(component.form.get('usedSubstances')?.value).toEqual([]);
    expect(component.form.get('developedFilms')?.value).toEqual([]);
  });

  it('should create new session when submitting in insert mode', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockSessionService.add.and.returnValue(of({}));
    fixture.detectChanges();

    component.form.patchValue({
      sessionDate: '2023-10-01',
      location: 'Test Location',
      participants: ['Angel'],
      description: 'Test session'
    });

    component.submit();

    expect(mockSessionService.add).toHaveBeenCalled();
    expect(component.loading).toBeFalsy();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/sessions']);
  });

  it('should update existing session when submitting in edit mode', () => {
    const testRowKey = 'test-session-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockActivatedRoute.snapshot.queryParams = { edit: 'true' };
    mockSessionService.update.and.returnValue(of({}));

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.patchValue({
      location: 'Updated Location'
    });

    component.submit();

    expect(mockSessionService.update).toHaveBeenCalledWith(testRowKey, jasmine.any(Object));
    expect(component.loading).toBeFalsy();
  });

  it('should not submit when form is invalid', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    fixture.detectChanges();

    component.form.patchValue({
      location: '',
      sessionDate: ''
    });

    component.submit();

    expect(mockSessionService.add).not.toHaveBeenCalled();
    expect(mockSessionService.update).not.toHaveBeenCalled();
  });

  it('should show error message on submission failure', () => {
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(null);
    mockSessionService.add.and.returnValue(throwError(() => 'Service error'));
    fixture.detectChanges();

    component.form.patchValue({
      sessionDate: '2023-10-01',
      location: 'Test Location'
    });

    component.submit();

    expect(component.errorMessage).toContain('Error');
    expect(component.loading).toBeFalsy();
  });

  it('should delete session when onDelete is called', () => {
    const testRowKey = 'test-session-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockSessionService.deleteById.and.returnValue(of({}));

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.rowKey = testRowKey;
    component.isInsert = false;

    component.onDelete();

    expect(mockSessionService.deleteById).toHaveBeenCalledWith(testRowKey);
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/sessions']);
  });

  it('should handle image selection', () => {
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

    component.onSessionImageSelected(mockEvent);
    mockReader.onload!();

    expect(mockReader.readAsDataURL).toHaveBeenCalledWith(mockFile);
    expect(component.form.get('imageBase64')?.value).toBe('data:image/jpeg;base64,mockbase64data');
  });

  it('should toggle devkit selection for modal', () => {
    fixture.detectChanges();
    const devKitRowKey = 'devkit-1';

    component.toggleDevKitSelection(devKitRowKey);
    expect(component.selectedDevKitsForModal).toContain(devKitRowKey);

    component.toggleDevKitSelection(devKitRowKey);
    expect(component.selectedDevKitsForModal).not.toContain(devKitRowKey);
  });

  it('should toggle film selection for modal', () => {
    fixture.detectChanges();
    const filmRowKey = 'film-1';

    component.toggleFilmSelection(filmRowKey);
    expect(component.selectedFilmsForModal).toContain(filmRowKey);

    component.toggleFilmSelection(filmRowKey);
    expect(component.selectedFilmsForModal).not.toContain(filmRowKey);
  });

  it('should add selected devkits from modal', () => {
    fixture.detectChanges();
    component.availableDevKits = [mockDevKit];
    component.selectedDevKitsForModal = ['devkit-1'];

    component.addSelectedDevKits();

    expect(component.sessionDevKits.length).toBe(1);
    expect(component.sessionDevKits[0].devKit.rowKey).toBe('devkit-1');
    expect(component.showAddDevKitModal).toBeFalsy();
    expect(component.selectedDevKitsForModal.length).toBe(0);
  });

  it('should add selected films from modal', () => {
    fixture.detectChanges();
    component.availableUnassignedFilms = [mockFilm];
    component.selectedFilmsForModal = ['film-1'];

    component.addSelectedFilms();

    expect(component.unassignedFilms.length).toBe(1);
    expect(component.unassignedFilms[0].rowKey).toBe('film-1');
    expect(component.showAddFilmModal).toBeFalsy();
    expect(component.selectedFilmsForModal.length).toBe(0);
  });

  it('should handle film drop to devkit', () => {
    fixture.detectChanges();
    const film: FilmDto = { ...mockFilm };
    component.unassignedFilms = [film];
    component.sessionDevKits = [{
      devKit: mockDevKit,
      assignedFilms: []
    }];

    const mockDropEvent = {
      previousContainer: {
        data: component.unassignedFilms
      },
      container: {
        data: component.sessionDevKits[0].assignedFilms
      },
      previousIndex: 0,
      currentIndex: 0,
      isPointerOverContainer: true
    } as any;

    component.onFilmDrop(mockDropEvent);

    expect(component.unassignedFilms.length).toBe(0);
    expect(component.sessionDevKits[0].assignedFilms.length).toBe(1);
  });

  it('should not process drop if pointer is not over container', () => {
    fixture.detectChanges();
    const film: FilmDto = { ...mockFilm };
    component.unassignedFilms = [film];
    component.sessionDevKits = [{
      devKit: mockDevKit,
      assignedFilms: []
    }];

    const mockDropEvent = {
      previousContainer: {
        data: component.unassignedFilms
      },
      container: {
        data: component.sessionDevKits[0].assignedFilms
      },
      previousIndex: 0,
      currentIndex: 0,
      isPointerOverContainer: false
    } as any;

    component.onFilmDrop(mockDropEvent);

    expect(component.unassignedFilms.length).toBe(1);
    expect(component.sessionDevKits[0].assignedFilms.length).toBe(0);
  });

  it('should remove devkit from session', () => {
    fixture.detectChanges();
    component.sessionDevKits = [{
      devKit: mockDevKit,
      assignedFilms: []
    }];

    component.removeDevKitFromSession('devkit-1');

    expect(component.sessionDevKits.length).toBe(0);
  });

  it('should remove film from session', () => {
    fixture.detectChanges();
    const film: FilmDto = { ...mockFilm };
    component.sessionDevKits = [{
      devKit: mockDevKit,
      assignedFilms: [film]
    }];
    component.availableUnassignedFilms = [];

    component.removeFilmFromSession('film-1');

    expect(component.sessionDevKits[0].assignedFilms.length).toBe(0);
    expect(component.availableUnassignedFilms.length).toBe(1);
  });

  it('should validate form fields correctly', () => {
    fixture.detectChanges();

    const locationControl = component.form.get('location');
    const sessionDateControl = component.form.get('sessionDate');

    locationControl?.setValue('');
    expect(locationControl?.invalid).toBeTruthy();

    locationControl?.setValue('Valid Location');
    expect(locationControl?.valid).toBeTruthy();

    sessionDateControl?.setValue('');
    expect(sessionDateControl?.invalid).toBeTruthy();

    sessionDateControl?.setValue('2023-10-01');
    expect(sessionDateControl?.valid).toBeTruthy();
  });

  it('should toggle edit mode correctly', () => {
    const testRowKey = 'test-session-key';
    mockActivatedRoute.snapshot.paramMap.get.and.returnValue(testRowKey);
    mockActivatedRoute.snapshot.queryParams = {};

    fixture = TestBed.createComponent(UpsertSessionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.isViewMode).toBeTruthy();
    expect(component.isEditMode).toBeFalsy();

    component.toggleEditMode();

    expect(component.isViewMode).toBeFalsy();
    expect(component.isEditMode).toBeTruthy();
    expect(mockRouter.navigate).toHaveBeenCalledWith(
      [],
      jasmine.objectContaining({
        queryParams: { edit: 'true' },
        queryParamsHandling: 'merge'
      })
    );
  });

  it('should filter expired devkits when showExpiredDevKits is false', () => {
    fixture.detectChanges();
    const expiredDevKit: DevKitDto = { ...mockDevKit, rowKey: 'devkit-2', expired: true };
    component.availableDevKits = [mockDevKit, expiredDevKit];
    component.showExpiredDevKits = false;

    const filtered = component.filteredAvailableDevKits;

    expect(filtered.length).toBe(1);
    expect(filtered[0].expired).toBeFalsy();
  });

  it('should show all devkits when showExpiredDevKits is true', () => {
    fixture.detectChanges();
    const expiredDevKit: DevKitDto = { ...mockDevKit, rowKey: 'devkit-2', expired: true };
    component.availableDevKits = [mockDevKit, expiredDevKit];
    component.showExpiredDevKits = true;

    const filtered = component.filteredAvailableDevKits;

    expect(filtered.length).toBe(2);
  });

  it('should handle no files in image selection', () => {
    const mockEvent = {
      target: {
        files: []
      }
    } as any;

    fixture.detectChanges();
    component.onSessionImageSelected(mockEvent);

    expect(component.form.get('imageBase64')?.value).toBe('');
  });

  it('should close add devkit modal', () => {
    fixture.detectChanges();
    component.showAddDevKitModal = true;
    component.selectedDevKitsForModal = ['devkit-1', 'devkit-2'];

    component.closeAddDevKitModal();

    expect(component.showAddDevKitModal).toBeFalsy();
    expect(component.selectedDevKitsForModal.length).toBe(0);
  });

  it('should close add film modal', () => {
    fixture.detectChanges();
    component.showAddFilmModal = true;
    component.selectedFilmsForModal = ['film-1', 'film-2'];

    component.closeAddFilmModal();

    expect(component.showAddFilmModal).toBeFalsy();
    expect(component.selectedFilmsForModal.length).toBe(0);
  });

  it('should check if devkit is selected for modal', () => {
    fixture.detectChanges();
    component.selectedDevKitsForModal = ['devkit-1'];

    expect(component.isDevKitSelectedForModal('devkit-1')).toBeTruthy();
    expect(component.isDevKitSelectedForModal('devkit-2')).toBeFalsy();
  });

  it('should check if film is selected for modal', () => {
    fixture.detectChanges();
    component.selectedFilmsForModal = ['film-1'];

    expect(component.isFilmSelectedForModal('film-1')).toBeTruthy();
    expect(component.isFilmSelectedForModal('film-2')).toBeFalsy();
  });
});
