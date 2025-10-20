import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FilmsComponent } from '../../components/films/films.component';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
import { FilmSearchComponent } from '../../components/films/film-search/film-search.component';
import { FilmService, AccountService } from '../../services';
import { FilmDto, IdentityDto, PagedResponseDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';
import { TestConfig } from '../test.config';

describe('FilmsComponent', () => {
  let component: FilmsComponent;
  let fixture: ComponentFixture<FilmsComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockAccountService: jasmine.SpyObj<AccountService>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockIdentity: IdentityDto = {
    username: 'Angel',
    email: 'angel@test.com'
  };

  beforeEach(async () => {
    const filmServiceSpy = TestConfig.createCrudServiceSpy('FilmService', [
      'getMyDevelopedFilmsPaged', 
      'getMyNotDevelopedFilmsPaged',
      'getDevelopedFilmsPaged',
      'getNotDevelopedFilmsPaged'
    ]);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const routerSpy = TestConfig.createRouterSpy();

    // Set up default return values using TestConfig helpers
    const emptyPagedResponse = TestConfig.createEmptyPagedResponse<FilmDto>();
    
    TestConfig.setupPaginatedServiceMocks(filmServiceSpy, [], {
      getMyDevelopedFilmsPaged: emptyPagedResponse,
      getMyNotDevelopedFilmsPaged: emptyPagedResponse,
      getDevelopedFilmsPaged: emptyPagedResponse,
      getNotDevelopedFilmsPaged: emptyPagedResponse
    });
    accountServiceSpy.whoAmI.and.returnValue(of(mockIdentity));

    await TestConfig.configureTestBed({
      declarations: [FilmsComponent, CardListComponent],
      imports: [FilmSearchComponent], // Add FilmSearchComponent to imports
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmsComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockAccountService = TestBed.inject(AccountService) as jasmine.SpyObj<AccountService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  afterEach(() => {
    fixture.destroy();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default pagination values', () => {
    expect(component.myDevelopedFilms).toEqual([]);
    expect(component.myNotDevelopedFilms).toEqual([]);
    expect(component.allDevelopedFilms).toEqual([]);
    expect(component.allNotDevelopedFilms).toEqual([]);
    expect(component.activeTab).toBe('my');
    expect(component.myDevelopedPage).toBe(1);
    expect(component.myNotDevelopedPage).toBe(1);
    expect(component.allDevelopedPage).toBe(1);
    expect(component.allNotDevelopedPage).toBe(1);
    expect(component.hasMoreMyDeveloped).toBe(false);
    expect(component.hasMoreMyNotDeveloped).toBe(false);
    expect(component.hasMoreAllDeveloped).toBe(false);
    expect(component.hasMoreAllNotDeveloped).toBe(false);
  });

  it('should load paginated films on initialization', () => {
    // Arrange
    const myDevelopedResponse = TestConfig.createPagedResponse(
      [createMockFilm('1', 'My Developed Film', UsernameType.Angel, true)]
    );

    const myNotDevelopedResponse = TestConfig.createPagedResponse(
      [createMockFilm('2', 'My Not Developed Film', UsernameType.Angel, false)]
    );

    mockFilmService.getMyDevelopedFilmsPaged.and.returnValue(of(myDevelopedResponse));
    mockFilmService.getMyNotDevelopedFilmsPaged.and.returnValue(of(myNotDevelopedResponse));

    // Act
    fixture.detectChanges();

    // Assert
    expect(mockFilmService.getMyDevelopedFilmsPaged).toHaveBeenCalledWith(1, 5, undefined);
    expect(mockFilmService.getMyNotDevelopedFilmsPaged).toHaveBeenCalledWith(1, 5, undefined);
    expect(component.myDevelopedFilms.length).toBe(1);
    expect(component.myNotDevelopedFilms.length).toBe(1);
    expect(component.hasMoreMyDeveloped).toBe(false);
    expect(component.hasMoreMyNotDeveloped).toBe(false);
  });

  it('should load more my developed films when loadMoreMyDevelopedFilms is called', () => {
    // Arrange
    const initialResponse = TestConfig.createPagedResponse(
      [createMockFilm('1', 'Film 1', UsernameType.Angel, true)],
      1, // currentPage
      1  // pageSize
    );
    // Manually adjust for specific test scenario
    initialResponse.totalCount = 3;
    initialResponse.totalPages = 3;
    initialResponse.hasNextPage = true;

    const nextPageResponse = TestConfig.createPagedResponse(
      [createMockFilm('2', 'Film 2', UsernameType.Angel, true)],
      2, // currentPage
      1  // pageSize  
    );
    // Manually adjust for specific test scenario
    nextPageResponse.totalCount = 3;
    nextPageResponse.totalPages = 3;
    nextPageResponse.hasNextPage = true;
    nextPageResponse.hasPreviousPage = true;

    mockFilmService.getMyDevelopedFilmsPaged.and.returnValues(of(initialResponse), of(nextPageResponse));
    
    fixture.detectChanges();

    // Act
    component.loadMoreMyDevelopedFilms();

    // Assert
    expect(mockFilmService.getMyDevelopedFilmsPaged).toHaveBeenCalledWith(2, 5, undefined);
    expect(component.myDevelopedFilms.length).toBe(2);
    expect(component.myDevelopedPage).toBe(3);
    expect(component.hasMoreMyDeveloped).toBe(true);
  });

  it('should handle pagination correctly for all films tab', () => {
    // Arrange
    const allDevelopedResponse = TestConfig.createPagedResponse([
        createMockFilm('1', 'Angel Film', UsernameType.Angel, true),
        createMockFilm('2', 'Tudor Film', UsernameType.Tudor, true)
      ],
      1, // currentPage
      2  // pageSize
    );
    // Manually adjust for specific test scenario
    allDevelopedResponse.totalCount = 5;
    allDevelopedResponse.totalPages = 3;
    allDevelopedResponse.hasNextPage = true;

    mockFilmService.getDevelopedFilmsPaged.and.returnValue(of(allDevelopedResponse));

    // Act
    fixture.detectChanges();

    // Assert
    expect(mockFilmService.getDevelopedFilmsPaged).toHaveBeenCalledWith(1, 5, undefined);
    expect(component.allDevelopedFilms.length).toBe(2);
    expect(component.hasMoreAllDeveloped).toBe(true);
  });

  it('should handle loading states correctly', () => {
    // Arrange
    fixture.detectChanges();

    // Act & Assert
    expect(component.loadingMyDeveloped).toBe(false);
    expect(component.loadingMyNotDeveloped).toBe(false);
    expect(component.loadingAllDeveloped).toBe(false);
    expect(component.loadingAllNotDeveloped).toBe(false);
  });

  it('should prevent multiple simultaneous loads', () => {
    // Arrange
    component.loadingMyDeveloped = true;
    const serviceCallCount = mockFilmService.getMyDevelopedFilmsPaged.calls.count();

    // Act
    component.loadMoreMyDevelopedFilms();

    // Assert
    expect(mockFilmService.getMyDevelopedFilmsPaged.calls.count()).toBe(serviceCallCount);
  });

  it('should navigate to new film page when onNewFilmClick is called', () => {
    // Act
    component.onNewFilmClick();

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/films/new']);
  });

  it('should navigate to film details when onFilmSelected is called', () => {
    // Arrange
    const rowKey = 'test-row-key';

    // Act
    component.onFilmSelected(rowKey);

    // Assert
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/films/' + rowKey]);
  });

  it('should set active tab correctly', () => {
    // Act & Assert
    component.setActiveTab('my');
    expect(component.activeTab).toBe('my');

    component.setActiveTab('all');
    expect(component.activeTab).toBe('all');
  });

  it('should handle error when loading my developed films', () => {
    // Arrange
    const consoleSpy = spyOn(console, 'error');
    mockFilmService.getMyDevelopedFilmsPaged.and.returnValue(throwError('Service error'));

    // Act
    fixture.detectChanges();

    // Assert
    expect(consoleSpy).toHaveBeenCalled();
    expect(component.loadingMyDeveloped).toBe(false);
  });

  it('should handle error when loading more films', () => {
    // Arrange
    const consoleSpy = spyOn(console, 'error');
    fixture.detectChanges();
    
    mockFilmService.getMyDevelopedFilmsPaged.and.returnValue(throwError('Load more error'));

    // Act
    component.loadMoreMyDevelopedFilms();

    // Assert
    expect(consoleSpy).toHaveBeenCalled();
    expect(component.loadingMyDeveloped).toBe(false);
  });

  // Helper function to create mock films
  function createMockFilm(
    rowKey: string, 
    name: string, 
    purchasedBy: UsernameType, 
    developed: boolean, 
    purchasedOn: string = '2023-01-01'
  ): FilmDto {
    return {
      rowKey,
      name,
      iso: '400',
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 12.50,
      purchasedBy,
      purchasedOn,
      description: 'Test film description',
      developed,
      imageUrl: 'test-image-url',
    };
  }

  describe('Search functionality', () => {
    it('should handle search for My Films tab', () => {
      spyOn(component, 'loadMyDevelopedFilms');
      spyOn(component, 'loadMyNotDevelopedFilms');
      
      const searchParams = { name: 'Test Film' };
      component.activeTab = 'my';
      
      component.onSearch(searchParams);
      
      expect(component.myFilmsIsSearching).toBe(true);
      expect(component.myFilmsSearchParams).toEqual(searchParams);
      expect(component.loadMyDevelopedFilms).toHaveBeenCalled();
      expect(component.loadMyNotDevelopedFilms).toHaveBeenCalled();
    });

    it('should handle search for All Films tab', () => {
      spyOn(component, 'loadAllDevelopedFilms');
      spyOn(component, 'loadAllNotDevelopedFilms');
      
      const searchParams = { name: 'Test Film' };
      component.activeTab = 'all';
      
      component.onSearch(searchParams);
      
      expect(component.allFilmsIsSearching).toBe(true);
      expect(component.allFilmsSearchParams).toEqual(searchParams);
      expect(component.loadAllDevelopedFilms).toHaveBeenCalled();
      expect(component.loadAllNotDevelopedFilms).toHaveBeenCalled();
    });

    it('should clear search state', () => {
      spyOn(component, 'loadMyDevelopedFilms');
      spyOn(component, 'loadMyNotDevelopedFilms');
      
      component.myFilmsIsSearching = true;
      component.myFilmsSearchParams = { name: 'Test' };
      component.activeTab = 'my';
      
      component.onClearFilters();
      
      expect(component.myFilmsIsSearching).toBe(false);
      expect(component.myFilmsSearchParams).toEqual({});
      expect(component.loadMyDevelopedFilms).toHaveBeenCalled();
      expect(component.loadMyNotDevelopedFilms).toHaveBeenCalled();
    });

    it('should reset pagination on search', () => {
      // Set initial page values
      component.allDevelopedPage = 3;
      component.allNotDevelopedPage = 2;
      component.myDevelopedPage = 4;
      component.myNotDevelopedPage = 1;
      
      // Verify initial state
      expect(component.allDevelopedPage).toBe(3);
      expect(component.allNotDevelopedPage).toBe(2);
      expect(component.myDevelopedPage).toBe(4);
      expect(component.myNotDevelopedPage).toBe(1);
      
      // Call onSearch - this will reset pagination to 1, then load methods will increment to 2
      component.onSearch({ name: 'Test' });
      
      // After onSearch completes, the load methods are called which increment the page numbers
      // So we expect them to be 2 (reset to 1, then incremented by load methods)
      expect(component.myDevelopedPage).toBe(2);
      expect(component.myNotDevelopedPage).toBe(2);
    });

    it('should clear results on search for active tab only', () => {
      const mockFilms = [createMockFilm('1', 'Film 1', UsernameType.Angel, true)];
      component.allDevelopedFilms = mockFilms;
      component.allNotDevelopedFilms = mockFilms;
      component.myDevelopedFilms = mockFilms;
      component.myNotDevelopedFilms = mockFilms;
      
      // Test My Films tab (default active tab)
      component.onSearch({ name: 'Test' });
      
      // Only My Films should be cleared
      expect(component.myDevelopedFilms).toEqual([]);
      expect(component.myNotDevelopedFilms).toEqual([]);
      // All Films should remain unchanged
      expect(component.allDevelopedFilms).toEqual(mockFilms);
      expect(component.allNotDevelopedFilms).toEqual(mockFilms);
    });
  });
});
