import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { HomeComponent } from '../../components/home/home.component';
import { FilmService, UserSettingsService } from '../../services';
import { TestConfig } from '../test.config';
import { FilmDto, UserSettingsDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockUserSettingsService: jasmine.SpyObj<UserSettingsService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', [
      'getById', 'getNotDevelopedFilms', 'getExposureDates'
    ]);
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', [
      'getUserSettings', 'getSubscribedUsers', 'updateUserSettings'
    ]);
    const routerSpy = TestConfig.createRouterSpy();

    // Set up default return values
    userSettingsServiceSpy.getUserSettings.and.returnValue(of({
      userId: 'test-user',
      isSubscribed: true,
      tableView: false,
      entitiesPerPage: 5,
      currentFilmId: null
    } as UserSettingsDto));
    userSettingsServiceSpy.getSubscribedUsers.and.returnValue(of([]));
    userSettingsServiceSpy.updateUserSettings.and.returnValue(of({} as UserSettingsDto));
    filmServiceSpy.getNotDevelopedFilms.and.returnValue(of([]));
    filmServiceSpy.getExposureDates.and.returnValue(of([]));

    await TestConfig.configureTestBed({
      declarations: [HomeComponent],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockUserSettingsService = TestBed.inject(UserSettingsService) as jasmine.SpyObj<UserSettingsService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

});
