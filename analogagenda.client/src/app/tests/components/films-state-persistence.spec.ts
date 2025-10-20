import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { FilmsComponent } from '../../components/films/films.component';
import { FilmService, AccountService, LocalStorageService } from '../../services';
import { IdentityDto } from '../../DTOs';

describe('FilmsComponent State Persistence', () => {
  let component: FilmsComponent;
  let fixture: ComponentFixture<FilmsComponent>;
  let localStorageService: LocalStorageService;
  let filmService: jasmine.SpyObj<FilmService>;
  let accountService: jasmine.SpyObj<AccountService>;

  const mockIdentity: IdentityDto = {
    username: 'testuser',
    email: 'test@example.com'
  };

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', [
      'getMyDevelopedFilmsPaged',
      'getMyNotDevelopedFilmsPaged',
      'getDevelopedFilmsPaged',
      'getNotDevelopedFilmsPaged'
    ]);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);

    await TestBed.configureTestingModule({
      declarations: [FilmsComponent],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: jasmine.createSpyObj('Router', ['navigate']) }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmsComponent);
    component = fixture.componentInstance;
    localStorageService = TestBed.inject(LocalStorageService);
    filmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    accountService = TestBed.inject(AccountService) as jasmine.SpyObj<AccountService>;

    // Mock service responses
    accountService.whoAmI.and.returnValue(of(mockIdentity));
    filmService.getMyDevelopedFilmsPaged.and.returnValue(of({ 
      data: [], 
      hasNextPage: false,
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      hasPreviousPage: false,
      totalPages: 0
    }));
    filmService.getMyNotDevelopedFilmsPaged.and.returnValue(of({ 
      data: [], 
      hasNextPage: false,
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      hasPreviousPage: false,
      totalPages: 0
    }));
    filmService.getDevelopedFilmsPaged.and.returnValue(of({ 
      data: [], 
      hasNextPage: false,
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      hasPreviousPage: false,
      totalPages: 0
    }));
    filmService.getNotDevelopedFilmsPaged.and.returnValue(of({ 
      data: [], 
      hasNextPage: false,
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      hasPreviousPage: false,
      totalPages: 0
    }));

    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should restore state on initialization', () => {
    const savedState = {
      activeTab: 'all',
      myFilmsSearchParams: { name: 'test' },
      allFilmsSearchParams: { type: 'ColorNegative' },
      myFilmsIsSearching: true,
      allFilmsIsSearching: false
    };

    spyOn(localStorageService, 'getState').and.returnValue(savedState);
    spyOn(localStorageService, 'saveState');

    component.ngOnInit();

    expect(component.activeTab).toBe('all');
    expect(component.myFilmsSearchParams).toEqual({ name: 'test' });
    expect(component.allFilmsSearchParams).toEqual({ type: 'ColorNegative' });
    expect(component.myFilmsIsSearching).toBe(true);
    expect(component.allFilmsIsSearching).toBe(false);
  });

  it('should save state when tab changes', () => {
    spyOn(localStorageService, 'saveState');

    component.setActiveTab('all');

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_films_page_state',
      jasmine.objectContaining({
        activeTab: 'all',
        myFilmsSearchParams: {},
        allFilmsSearchParams: {},
        myFilmsIsSearching: false,
        allFilmsIsSearching: false
      })
    );
  });

  it('should save state when search parameters change', () => {
    spyOn(localStorageService, 'saveState');

    const searchParams = { name: 'test film' };
    component.onSearch(searchParams);

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_films_page_state',
      jasmine.objectContaining({
        activeTab: 'my',
        myFilmsSearchParams: searchParams,
        allFilmsSearchParams: {},
        myFilmsIsSearching: true,
        allFilmsIsSearching: false
      })
    );
  });

  it('should save state when filters are cleared', () => {
    spyOn(localStorageService, 'saveState');

    component.onClearFilters();

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_films_page_state',
      jasmine.objectContaining({
        activeTab: 'my',
        myFilmsSearchParams: {},
        allFilmsSearchParams: {},
        myFilmsIsSearching: false,
        allFilmsIsSearching: false
      })
    );
  });

  it('should save state on component destroy', () => {
    spyOn(localStorageService, 'saveState');

    component.ngOnDestroy();

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_films_page_state',
      jasmine.objectContaining({
        activeTab: 'my',
        myFilmsSearchParams: {},
        allFilmsSearchParams: {},
        myFilmsIsSearching: false,
        allFilmsIsSearching: false
      })
    );
  });

  it('should handle missing state gracefully', () => {
    spyOn(localStorageService, 'getState').and.returnValue(null);

    component.ngOnInit();

    expect(component.activeTab).toBe('my');
    expect(component.myFilmsSearchParams).toEqual({});
    expect(component.allFilmsSearchParams).toEqual({});
    expect(component.myFilmsIsSearching).toBe(false);
    expect(component.allFilmsIsSearching).toBe(false);
  });

  it('should handle partial state restoration', () => {
    const partialState = {
      activeTab: 'all',
      // Missing other properties
    };

    spyOn(localStorageService, 'getState').and.returnValue(partialState);

    component.ngOnInit();

    expect(component.activeTab).toBe('all');
    expect(component.myFilmsSearchParams).toEqual({});
    expect(component.allFilmsSearchParams).toEqual({});
    expect(component.myFilmsIsSearching).toBe(false);
    expect(component.allFilmsIsSearching).toBe(false);
  });

  it('should save state when searching on All Films tab', () => {
    spyOn(localStorageService, 'saveState');
    component.setActiveTab('all');

    const searchParams = { type: 'ColorNegative' };
    component.onSearch(searchParams);

    expect(localStorageService.saveState).toHaveBeenCalledWith(
      'analogagenda_films_page_state',
      jasmine.objectContaining({
        activeTab: 'all',
        myFilmsSearchParams: {},
        allFilmsSearchParams: searchParams,
        myFilmsIsSearching: false,
        allFilmsIsSearching: true
      })
    );
  });

  it('should clear only My Films when searching on My Films tab', () => {
    // Set up some initial data
    component.myDevelopedFilms = [{ rowKey: '1', name: 'My Film' } as any];
    component.allDevelopedFilms = [{ rowKey: '2', name: 'All Film' } as any];

    const searchParams = { name: 'test' };
    component.onSearch(searchParams);

    // My Films should be cleared and reloaded
    expect(component.myFilmsIsSearching).toBe(true);
    expect(component.myFilmsSearchParams).toEqual(searchParams);
    
    // All Films should remain unchanged
    expect(component.allFilmsIsSearching).toBe(false);
    expect(component.allFilmsSearchParams).toEqual({});
  });

  it('should clear only All Films when searching on All Films tab', () => {
    // Set up some initial data
    component.myDevelopedFilms = [{ rowKey: '1', name: 'My Film' } as any];
    component.allDevelopedFilms = [{ rowKey: '2', name: 'All Film' } as any];
    component.setActiveTab('all');

    const searchParams = { type: 'ColorNegative' };
    component.onSearch(searchParams);

    // All Films should be cleared and reloaded
    expect(component.allFilmsIsSearching).toBe(true);
    expect(component.allFilmsSearchParams).toEqual(searchParams);
    
    // My Films should remain unchanged
    expect(component.myFilmsIsSearching).toBe(false);
    expect(component.myFilmsSearchParams).toEqual({});
  });
});
