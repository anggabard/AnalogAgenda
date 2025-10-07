import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import { UpsertFilmComponent } from '../../components/films/upsert-film/upsert-film.component';
import { FilmService, SessionService, DevKitService, PhotoService } from '../../services';
import { DevKitType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('UpsertFilmComponent', () => {
  let component: UpsertFilmComponent;
  let fixture: ComponentFixture<UpsertFilmComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockSessionService: jasmine.SpyObj<SessionService>;
  let mockDevKitService: jasmine.SpyObj<DevKitService>;
  let mockPhotoService: jasmine.SpyObj<PhotoService>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getById', 'update', 'create']);
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getById', 'update', 'getAll']);
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getById', 'update', 'getAll']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', ['getAll', 'upload']);
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
});