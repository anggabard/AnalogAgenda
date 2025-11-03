import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FilmSearchComponent } from '../../components/films/film-search/film-search.component';
import { DevKitService, SessionService, LocalStorageService } from '../../services';
import { DevKitDto, SessionDto } from '../../DTOs';
import { DevKitType, UsernameType } from '../../enums';
import { of } from 'rxjs';

describe('FilmSearchComponent State Persistence', () => {
  let component: FilmSearchComponent;
  let fixture: ComponentFixture<FilmSearchComponent>;
  let localStorageService: LocalStorageService;
  let devKitService: jasmine.SpyObj<DevKitService>;
  let sessionService: jasmine.SpyObj<SessionService>;

  const mockDevKits: DevKitDto[] = [
    { 
      id: '1', 
      name: 'Belini C41', 
      type: DevKitType.C41,
      url: '',
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 4,
      validForFilms: 10,
      filmsDeveloped: 0,
      imageUrl: '',
      description: '',
      expired: false
    },
    { 
      id: '2', 
      name: 'Belini E6', 
      type: DevKitType.E6,
      url: '',
      purchasedBy: UsernameType.Angel,
      purchasedOn: '2023-01-01',
      mixedOn: '2023-01-01',
      validForWeeks: 4,
      validForFilms: 10,
      filmsDeveloped: 0,
      imageUrl: '',
      description: '',
      expired: false
    }
  ];

  const mockSessions: SessionDto[] = [
    { 
      id: '1', 
      sessionDate: '2023-01-01', 
      location: 'Studio A',
      participants: '[]',
      description: '',
      usedSubstances: '[]',
      developedFilms: '[]',
      participantsList: [],
      usedSubstancesList: [],
      developedFilmsList: [],
      imageUrl: '',
      imageBase64: ''
    },
    { 
      id: '2', 
      sessionDate: '2023-01-02', 
      location: 'Studio B',
      participants: '[]',
      description: '',
      usedSubstances: '[]',
      developedFilms: '[]',
      participantsList: [],
      usedSubstancesList: [],
      developedFilmsList: [],
      imageUrl: '',
      imageBase64: ''
    }
  ];

  beforeEach(async () => {
    const devKitServiceSpy = jasmine.createSpyObj('DevKitService', ['getAll']);
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getAll']);

    await TestBed.configureTestingModule({
      imports: [FilmSearchComponent],
      providers: [
        { provide: DevKitService, useValue: devKitServiceSpy },
        { provide: SessionService, useValue: sessionServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmSearchComponent);
    component = fixture.componentInstance;
    localStorageService = TestBed.inject(LocalStorageService);
    devKitService = TestBed.inject(DevKitService) as jasmine.SpyObj<DevKitService>;
    sessionService = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;

    // Mock service responses
    devKitService.getAll.and.returnValue(of(mockDevKits));
    sessionService.getAll.and.returnValue(of(mockSessions));

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should restore search fields state for My Films tab', () => {
    component.isMyFilmsTab = true;
    const savedState = {
      searchFields: [
        { key: 'name', visible: true, value: 'test film' },
        { key: 'id', visible: false, value: '' },
        { key: 'type', visible: true, value: 'ColorNegative' }
      ]
    };

    spyOn(localStorageService, 'getState').and.returnValue(savedState);
    spyOn(localStorageService, 'saveState');

    component.ngOnInit();

    const nameField = component.searchFields.find(f => f.key === 'name');
    const idField = component.searchFields.find(f => f.key === 'id');
    const typeField = component.searchFields.find(f => f.key === 'type');

    expect(nameField?.visible).toBe(true);
    expect(nameField?.value).toBe('test film');
    expect(idField?.visible).toBe(false);
    expect(typeField?.visible).toBe(true);
    expect(typeField?.value).toBe('ColorNegative');
  });

  it('should restore search fields state for All Films tab', () => {
    component.isMyFilmsTab = false;
    const savedState = {
      searchFields: [
        { key: 'purchasedBy', visible: true, value: 'Angel' },
        { key: 'name', visible: false, value: '' }
      ]
    };

    spyOn(localStorageService, 'getState').and.returnValue(savedState);
    spyOn(localStorageService, 'saveState');

    component.ngOnInit();

    const purchasedByField = component.searchFields.find(f => f.key === 'purchasedBy');
    const nameField = component.searchFields.find(f => f.key === 'name');

    expect(purchasedByField?.visible).toBe(true);
    expect(purchasedByField?.value).toBe('Angel');
    expect(nameField?.visible).toBe(false);
  });

  it('should save state when field visibility changes', () => {
    component.isMyFilmsTab = true;
    spyOn(localStorageService, 'saveState');

    const nameField = component.searchFields.find(f => f.key === 'name');
    component.toggleFieldVisibility(nameField!);

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_myFilmsSearchFields',
      jasmine.objectContaining({
        searchFields: jasmine.arrayContaining([
          jasmine.objectContaining({ key: 'name', visible: false })
        ])
      })
    );
  });

  it('should save state when field values change', () => {
    component.isMyFilmsTab = true;
    spyOn(localStorageService, 'saveState');

    component.onFieldValueChange();

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_myFilmsSearchFields',
      jasmine.objectContaining({
        searchFields: jasmine.any(Array)
      })
    );
  });

  it('should save state when filters are cleared', () => {
    component.isMyFilmsTab = true;
    spyOn(localStorageService, 'saveState');

    component.onClearFilters();

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_myFilmsSearchFields',
      jasmine.objectContaining({
        searchFields: jasmine.any(Array)
      })
    );
  });

  it('should save state on component destroy', () => {
    component.isMyFilmsTab = true;
    spyOn(localStorageService, 'saveState');

    component.ngOnDestroy();

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_myFilmsSearchFields',
      jasmine.objectContaining({
        searchFields: jasmine.any(Array)
      })
    );
  });

  it('should use correct storage key for My Films tab', () => {
    component.isMyFilmsTab = true;
    spyOn(localStorageService, 'getState').and.returnValue(null);

    component.ngOnInit();

    expect(localStorageService.getState).toHaveBeenCalledWith('analogagenda_myFilmsSearchFields');
  });

  it('should use correct storage key for All Films tab', () => {
    component.isMyFilmsTab = false;
    spyOn(localStorageService, 'getState').and.returnValue(null);

    component.ngOnInit();

    expect(localStorageService.getState).toHaveBeenCalledWith('analogagenda_allFilmsSearchFields');
  });

  it('should handle missing state gracefully', () => {
    component.isMyFilmsTab = true;
    spyOn(localStorageService, 'getState').and.returnValue(null);

    component.ngOnInit();

    // Should not throw an error and should use default values
    expect(component.searchFields.every(field => 
      field.visible === field.defaultVisible && 
      (field.value === '' || field.value === null)
    )).toBe(true);
  });

  it('should handle partial state restoration', () => {
    component.isMyFilmsTab = true;
    const partialState = {
      searchFields: [
        { key: 'name', visible: true, value: 'test' }
        // Missing other fields
      ]
    };

    spyOn(localStorageService, 'getState').and.returnValue(partialState);

    component.ngOnInit();

    const nameField = component.searchFields.find(f => f.key === 'name');
    expect(nameField?.visible).toBe(true);
    expect(nameField?.value).toBe('test');
  });

  it('should apply initial search parameters correctly', () => {
    component.isMyFilmsTab = true;
    component.initialSearchParams = { name: 'initial test', type: 'ColorNegative' };

    component.ngOnInit();

    const nameField = component.searchFields.find(f => f.key === 'name');
    const typeField = component.searchFields.find(f => f.key === 'type');

    expect(nameField?.visible).toBe(true);
    expect(nameField?.value).toBe('initial test');
    expect(typeField?.visible).toBe(true);
    expect(typeField?.value).toBe('ColorNegative');
  });
});
