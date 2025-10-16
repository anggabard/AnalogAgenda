import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { UpsertSessionComponent } from '../../components/sessions/upsert-session/upsert-session.component';
import { SessionService, DevKitService, FilmService } from '../../services';
import { DevKitDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertSessionComponent', () => {
  let component: UpsertSessionComponent;
  let fixture: ComponentFixture<UpsertSessionComponent>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'add', 'update', 'deleteById']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getAll']);
    const routerSpy = TestConfig.createRouterSpy();

    sessionServiceSpy.getById.and.returnValue(of({}));
    devKitServiceSpy.getAll.and.returnValue(of([]));
    filmServiceSpy.getAll.and.returnValue(of([]));

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
    component.ngOnInit();

    expect(component.isInsert).toBeTruthy();
    expect(component.rowKey).toBeNull();
  });


  it('should toggle devkit selection for modal', () => {
    component.selectedDevKitsForModal = [];
    const devKitRowKey = 'devkit-1';

    component.toggleDevKitSelection(devKitRowKey);

    expect(component.selectedDevKitsForModal).toContain(devKitRowKey);

    component.toggleDevKitSelection(devKitRowKey);

    expect(component.selectedDevKitsForModal).not.toContain(devKitRowKey);
  });

  it('should toggle film selection for modal', () => {
    component.selectedFilmsForModal = [];
    const filmRowKey = 'film-1';

    component.toggleFilmSelection(filmRowKey);

    expect(component.selectedFilmsForModal).toContain(filmRowKey);

    component.toggleFilmSelection(filmRowKey);

    expect(component.selectedFilmsForModal).not.toContain(filmRowKey);
  });

  it('should check if devkit is selected for modal', () => {
    component.selectedDevKitsForModal = ['devkit-1'];

    expect(component.isDevKitSelectedForModal('devkit-1')).toBeTruthy();
    expect(component.isDevKitSelectedForModal('devkit-2')).toBeFalsy();
  });

  it('should check if film is selected for modal', () => {
    component.selectedFilmsForModal = ['film-1'];

    expect(component.isFilmSelectedForModal('film-1')).toBeTruthy();
    expect(component.isFilmSelectedForModal('film-2')).toBeFalsy();
  });

  it('should close add devkit modal', () => {
    component.showAddDevKitModal = true;

    component.closeAddDevKitModal();

    expect(component.showAddDevKitModal).toBeFalsy();
    expect(component.selectedDevKitsForModal).toEqual([]);
  });

  it('should close add film modal', () => {
    component.showAddFilmModal = true;

    component.closeAddFilmModal();

    expect(component.showAddFilmModal).toBeFalsy();
    expect(component.selectedFilmsForModal).toEqual([]);
  });

  it('should filter expired devkits when showExpiredDevKits is false', () => {
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
    };
    const expiredDevKit: DevKitDto = {
      rowKey: 'devkit-2',
      name: 'Expired DevKit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Expired devkit',
      expired: true,
      imageUrl: 'test-url',
    };
    component.availableDevKits = [mockDevKit, expiredDevKit];
    component.showExpiredDevKits = false;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual([mockDevKit]);
  });

  it('should show all devkits when showExpiredDevKits is true', () => {
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
    };
    const expiredDevKit: DevKitDto = {
      rowKey: 'devkit-2',
      name: 'Expired DevKit',
      url: 'http://example.com',
      type: DevKitType.C41,
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 6,
      validForFilms: 8,
      filmsDeveloped: 0,
      description: 'Expired devkit',
      expired: true,
      imageUrl: 'test-url',
    };
    component.availableDevKits = [mockDevKit, expiredDevKit];
    component.showExpiredDevKits = true;

    const result = component.filteredAvailableDevKits;

    expect(result).toEqual([mockDevKit, expiredDevKit]);
  });
});