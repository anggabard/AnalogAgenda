import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { FilmsComponent } from '../../components/films/films.component';
import { FilmService, AccountService } from '../../services';
import { FilmDto, IdentityDto } from '../../DTOs';
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
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getAllFilms']);
    const accountServiceSpy = jasmine.createSpyObj('AccountService', ['whoAmI']);
    const routerSpy = TestConfig.createRouterSpy();

    // Set up default return values to avoid subscription errors
    filmServiceSpy.getAllFilms.and.returnValue(of([]));
    accountServiceSpy.whoAmI.and.returnValue(of(mockIdentity));

    await TestConfig.configureTestBed({
      declarations: [FilmsComponent],
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

  it('should initialize with default values', () => {
    expect(component.allFilms).toEqual([]);
    expect(component.myFilms).toEqual([]);
    expect(component.myDevelopedFilms).toEqual([]);
    expect(component.myNotDevelopedFilms).toEqual([]);
    expect(component.allDevelopedFilms).toEqual([]);
    expect(component.allNotDevelopedFilms).toEqual([]);
    expect(component.activeTab).toBe('my');
    expect(component.currentUsername).toBe('');
  });

  it('should load user identity and films on initialization', () => {
    // Arrange
    const mockFilms: FilmDto[] = [
      createMockFilm('1', 'Test Film 1', UsernameType.Angel, false),
      createMockFilm('2', 'Test Film 2', UsernameType.Tudor, true)
    ];

    mockFilmService.getAllFilms.and.returnValue(of(mockFilms));
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges();

    // Assert
    expect(mockAccountService.whoAmI).toHaveBeenCalled();
    expect(mockFilmService.getAllFilms).toHaveBeenCalled();
    expect(component.currentUsername).toBe('Angel');
    expect(component.allFilms.length).toBe(2);
  });

  it('should filter my films correctly', () => {
    // Arrange
    const mockFilms: FilmDto[] = [
      createMockFilm('1', 'My Film 1', UsernameType.Angel, false),
      createMockFilm('2', 'Other Film', UsernameType.Tudor, false),
      createMockFilm('3', 'My Film 2', UsernameType.Angel, true),
      createMockFilm('4', 'Another Other Film', UsernameType.Cristiana, true)
    ];

    mockFilmService.getAllFilms.and.returnValue(of(mockFilms));
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.myFilms.length).toBe(2);
    expect(component.myFilms.every(film => film.purchasedBy === UsernameType.Angel)).toBeTruthy();
  });

  it('should split films into developed and not developed correctly', () => {
    // Arrange
    const mockFilms: FilmDto[] = [
      createMockFilm('1', 'My Not Developed', UsernameType.Angel, false),
      createMockFilm('2', 'My Developed', UsernameType.Angel, true),
      createMockFilm('3', 'Other Not Developed', UsernameType.Tudor, false),
      createMockFilm('4', 'Other Developed', UsernameType.Tudor, true)
    ];

    mockFilmService.getAllFilms.and.returnValue(of(mockFilms));
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.myDevelopedFilms.length).toBe(1);
    expect(component.myNotDevelopedFilms.length).toBe(1);
    expect(component.allDevelopedFilms.length).toBe(2);
    expect(component.allNotDevelopedFilms.length).toBe(2);
    
    expect(component.myDevelopedFilms[0].developed).toBeTruthy();
    expect(component.myNotDevelopedFilms[0].developed).toBeFalsy();
  });

  it('should sort all films by owner then by newest date first', () => {
    // Arrange
    const mockFilms: FilmDto[] = [
      createMockFilm('1', 'Angel Old Film', UsernameType.Angel, false, '2023-01-01'),
      createMockFilm('2', 'Tudor New Film', UsernameType.Tudor, false, '2023-12-01'),
      createMockFilm('3', 'Angel New Film', UsernameType.Angel, false, '2023-06-01'),
      createMockFilm('4', 'Tudor Old Film', UsernameType.Tudor, false, '2023-03-01')
    ];

    mockFilmService.getAllFilms.and.returnValue(of(mockFilms));
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.allNotDevelopedFilms.length).toBe(4);
    
    // Should be sorted by owner first (Angel comes before Tudor alphabetically)
    expect(component.allNotDevelopedFilms[0].purchasedBy).toBe(UsernameType.Angel);
    expect(component.allNotDevelopedFilms[1].purchasedBy).toBe(UsernameType.Angel);
    expect(component.allNotDevelopedFilms[2].purchasedBy).toBe(UsernameType.Tudor);
    expect(component.allNotDevelopedFilms[3].purchasedBy).toBe(UsernameType.Tudor);
    
    // Within each owner group, newest films first
    expect(component.allNotDevelopedFilms[0].purchasedOn).toBe('2023-06-01'); // Angel's newer film
    expect(component.allNotDevelopedFilms[1].purchasedOn).toBe('2023-01-01'); // Angel's older film
    expect(component.allNotDevelopedFilms[2].purchasedOn).toBe('2023-12-01'); // Tudor's newer film
    expect(component.allNotDevelopedFilms[3].purchasedOn).toBe('2023-03-01'); // Tudor's older film
  });

  it('should sort my films by newest date first', () => {
    // Arrange
    const mockFilms: FilmDto[] = [
      createMockFilm('1', 'Old Film', UsernameType.Angel, false, '2023-01-01'),
      createMockFilm('2', 'New Film', UsernameType.Angel, false, '2023-12-01'),
      createMockFilm('3', 'Middle Film', UsernameType.Angel, false, '2023-06-01')
    ];

    mockFilmService.getAllFilms.and.returnValue(of(mockFilms));
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.myNotDevelopedFilms.length).toBe(3);
    
    // Should be sorted by newest date first
    expect(component.myNotDevelopedFilms[0].purchasedOn).toBe('2023-12-01');
    expect(component.myNotDevelopedFilms[1].purchasedOn).toBe('2023-06-01');
    expect(component.myNotDevelopedFilms[2].purchasedOn).toBe('2023-01-01');
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

  it('should handle error when loading films', () => {
    // Arrange
    const consoleSpy = spyOn(console, 'error');
    mockFilmService.getAllFilms.and.returnValue(throwError('Service error'));
    mockAccountService.whoAmI.and.returnValue(of(mockIdentity));

    // Act
    fixture.detectChanges();

    // Assert
    expect(consoleSpy).toHaveBeenCalled();
  });

  it('should handle error when loading user identity', () => {
    // Arrange
    const consoleSpy = spyOn(console, 'error');
    mockAccountService.whoAmI.and.returnValue(throwError('Identity error'));
    mockFilmService.getAllFilms.and.returnValue(of([]));

    // Act
    fixture.detectChanges();

    // Assert
    expect(consoleSpy).toHaveBeenCalled();
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
