import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FilmsComponent } from '../../components/films/films.component';
import { CardListComponent } from '../../components/common/card-list/card-list.component';
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
    const filmServiceSpy = jasmine.createSpyObj('FilmService', [
      'getAllFilms', 
      'getMyDevelopedFilmsPaged', 
      'getMyNotDevelopedFilmsPaged',
      'getDevelopedFilmsPaged',
      'getNotDevelopedFilmsPaged'
    ]);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const routerSpy = TestConfig.createRouterSpy();

    // Set up default return values to avoid subscription errors
    const emptyPagedResponse: PagedResponseDto<FilmDto> = {
      data: [],
      totalCount: 0,
      pageSize: 5,
      currentPage: 1,
      totalPages: 0,
      hasNextPage: false,
      hasPreviousPage: false
    };
    
    filmServiceSpy.getAllFilms.and.returnValue(of([]));
    filmServiceSpy.getMyDevelopedFilmsPaged.and.returnValue(of(emptyPagedResponse));
    filmServiceSpy.getMyNotDevelopedFilmsPaged.and.returnValue(of(emptyPagedResponse));
    filmServiceSpy.getDevelopedFilmsPaged.and.returnValue(of(emptyPagedResponse));
    filmServiceSpy.getNotDevelopedFilmsPaged.and.returnValue(of(emptyPagedResponse));
    accountServiceSpy.whoAmI.and.returnValue(of(mockIdentity));

    await TestConfig.configureTestBed({
      declarations: [FilmsComponent, CardListComponent],
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
    const myDevelopedResponse: PagedResponseDto<FilmDto> = {
      data: [createMockFilm('1', 'My Developed Film', UsernameType.Angel, true)],
      totalCount: 1,
      pageSize: 5,
      currentPage: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false
    };

    const myNotDevelopedResponse: PagedResponseDto<FilmDto> = {
      data: [createMockFilm('2', 'My Not Developed Film', UsernameType.Angel, false)],
      totalCount: 1,
      pageSize: 5,
      currentPage: 1,
      totalPages: 1,
      hasNextPage: false,
      hasPreviousPage: false
    };

    mockFilmService.getMyDevelopedFilmsPaged.and.returnValue(of(myDevelopedResponse));
    mockFilmService.getMyNotDevelopedFilmsPaged.and.returnValue(of(myNotDevelopedResponse));

    // Act
    fixture.detectChanges();

    // Assert
    expect(mockFilmService.getMyDevelopedFilmsPaged).toHaveBeenCalledWith(1, 5);
    expect(mockFilmService.getMyNotDevelopedFilmsPaged).toHaveBeenCalledWith(1, 5);
    expect(component.myDevelopedFilms.length).toBe(1);
    expect(component.myNotDevelopedFilms.length).toBe(1);
    expect(component.hasMoreMyDeveloped).toBe(false);
    expect(component.hasMoreMyNotDeveloped).toBe(false);
  });

  it('should load more my developed films when loadMoreMyDevelopedFilms is called', () => {
    // Arrange
    const initialResponse: PagedResponseDto<FilmDto> = {
      data: [createMockFilm('1', 'Film 1', UsernameType.Angel, true)],
      totalCount: 3,
      pageSize: 1,
      currentPage: 1,
      totalPages: 3,
      hasNextPage: true,
      hasPreviousPage: false
    };

    const nextPageResponse: PagedResponseDto<FilmDto> = {
      data: [createMockFilm('2', 'Film 2', UsernameType.Angel, true)],
      totalCount: 3,
      pageSize: 1,
      currentPage: 2,
      totalPages: 3,
      hasNextPage: true,
      hasPreviousPage: true
    };

    mockFilmService.getMyDevelopedFilmsPaged.and.returnValues(of(initialResponse), of(nextPageResponse));
    
    fixture.detectChanges();

    // Act
    component.loadMoreMyDevelopedFilms();

    // Assert
    expect(mockFilmService.getMyDevelopedFilmsPaged).toHaveBeenCalledWith(2, 5);
    expect(component.myDevelopedFilms.length).toBe(2);
    expect(component.myDevelopedPage).toBe(3);
    expect(component.hasMoreMyDeveloped).toBe(true);
  });

  it('should handle pagination correctly for all films tab', () => {
    // Arrange
    const allDevelopedResponse: PagedResponseDto<FilmDto> = {
      data: [
        createMockFilm('1', 'Angel Film', UsernameType.Angel, true),
        createMockFilm('2', 'Tudor Film', UsernameType.Tudor, true)
      ],
      totalCount: 5,
      pageSize: 2,
      currentPage: 1,
      totalPages: 3,
      hasNextPage: true,
      hasPreviousPage: false
    };

    mockFilmService.getDevelopedFilmsPaged.and.returnValue(of(allDevelopedResponse));

    // Act
    fixture.detectChanges();

    // Assert
    expect(mockFilmService.getDevelopedFilmsPaged).toHaveBeenCalledWith(1, 5);
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
      iso: 400,
      type: FilmType.ColorNegative,
      numberOfExposures: 36,
      cost: 12.50,
      purchasedBy,
      purchasedOn,
      description: 'Test film description',
      developed,
      imageUrl: 'test-image-url',
      imageBase64: ''
    };
  }
});
