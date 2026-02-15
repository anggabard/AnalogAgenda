import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { By } from '@angular/platform-browser';
import { HomeComponent } from '../../components/home/home.component';
import { FilmService, UserSettingsService, IdeaService } from '../../services';
import { WackyIdeasSectionComponent } from '../../components/home/wacky-ideas-section/wacky-ideas-section.component';
import { UpsertIdeaComponent } from '../../components/home/upsert-idea/upsert-idea.component';
import { TestConfig } from '../test.config';
import { FilmDto, UserSettingsDto, IdeaDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockUserSettingsService: jasmine.SpyObj<UserSettingsService>;
  let mockIdeaService: jasmine.SpyObj<IdeaService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', [
      'getById', 'getNotDevelopedFilms', 'getExposureDates'
    ]);
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', [
      'getUserSettings', 'getSubscribedUsers', 'updateUserSettings'
    ]);
    const ideaServiceSpy = jasmine.createSpyObj('IdeaService', ['getAll', 'getById', 'add', 'update', 'deleteById']);
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
    ideaServiceSpy.getAll.and.returnValue(of([]));

    await TestConfig.configureTestBed({
      declarations: [HomeComponent, WackyIdeasSectionComponent, UpsertIdeaComponent],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy },
        { provide: IdeaService, useValue: ideaServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockUserSettingsService = TestBed.inject(UserSettingsService) as jasmine.SpyObj<UserSettingsService>;
    mockIdeaService = TestBed.inject(IdeaService) as jasmine.SpyObj<IdeaService>;
    mockRouter = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should open add idea modal when openAddIdeaModal is called', () => {
    expect(component.isUpsertIdeaModalOpen).toBe(false);
    expect(component.selectedIdea).toBeNull();

    component.openAddIdeaModal();

    expect(component.isUpsertIdeaModalOpen).toBe(true);
    expect(component.selectedIdea).toBeNull();
  });

  it('should open edit idea modal with selected idea when openEditIdeaModal is called', () => {
    const idea: IdeaDto = { id: 'abc', title: 'Test Idea', description: 'Test desc' };
    component.openEditIdeaModal(idea);

    expect(component.isUpsertIdeaModalOpen).toBe(true);
    expect(component.selectedIdea).toBe(idea);
  });

  it('should close modal and refresh wacky section on idea saved', () => {
    component.isUpsertIdeaModalOpen = true;
    component.selectedIdea = null;
    const sectionEl = fixture.debugElement.query(By.directive(WackyIdeasSectionComponent));
    const section = sectionEl?.componentInstance as WackyIdeasSectionComponent;
    spyOn(section, 'loadIdeas');

    component.onIdeaSaved();

    expect(component.isUpsertIdeaModalOpen).toBe(false);
    expect(component.selectedIdea).toBeNull();
    expect(section.loadIdeas).toHaveBeenCalled();
  });

  it('should close modal and refresh wacky section on idea deleted', () => {
    component.isUpsertIdeaModalOpen = true;
    component.selectedIdea = { id: 'abc', title: 'x', description: '' };
    const sectionEl = fixture.debugElement.query(By.directive(WackyIdeasSectionComponent));
    const section = sectionEl?.componentInstance as WackyIdeasSectionComponent;
    spyOn(section, 'loadIdeas');

    component.onIdeaDeleted();

    expect(component.isUpsertIdeaModalOpen).toBe(false);
    expect(component.selectedIdea).toBeNull();
    expect(section.loadIdeas).toHaveBeenCalled();
  });
});
