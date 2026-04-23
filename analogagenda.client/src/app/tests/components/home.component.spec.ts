import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { By } from '@angular/platform-browser';
import { HomeComponent } from '../../components/home/home.component';
import { FilmService, UserSettingsService, IdeaService, PhotoService } from '../../services';
import { WackyIdeasSectionComponent } from '../../components/home/wacky-ideas-section/wacky-ideas-section.component';
import { UpsertIdeaComponent } from '../../components/home/wacky-ideas-section/upsert-idea/upsert-idea.component';
import { FilmCheckSectionComponent } from '../../components/home/film-check-section/film-check-section.component';
import { FilmCheckUserComponent } from '../../components/home/film-check-section/film-check-user/film-check-user.component';
import { CurrentFilmSectionComponent } from '../../components/home/current-film-section/current-film-section.component';
import { SettingsSectionComponent } from '../../components/home/settings-section/settings-section.component';
import { PhotoOfTheDaySectionComponent } from '../../components/home/photo-of-the-day-section/photo-of-the-day-section.component';
import { TestConfig } from '../test.config';
import { UserSettingsDto, IdeaDto } from '../../DTOs';
import { CdkDragDrop } from '@angular/cdk/drag-drop';
import { DEFAULT_HOME_SECTION_ORDER, normalizeHomeSectionOrder } from '../../helpers/home-section-order.helper';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockUserSettingsService: jasmine.SpyObj<UserSettingsService>;
  let mockIdeaService: jasmine.SpyObj<IdeaService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', [
      'getById', 'getMyNotDevelopedFilmsAll', 'getExposureDates'
    ]);
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', [
      'getUserSettings', 'getSubscribedUsers', 'updateUserSettings'
    ]);
    const ideaServiceSpy = jasmine.createSpyObj('IdeaService', ['getAll', 'getById', 'add', 'update', 'deleteById']);
    const photoServiceSpy = jasmine.createSpyObj('PhotoService', ['getPhotoOfTheDay']);
    const routerSpy = TestConfig.createRouterSpy();

    photoServiceSpy.getPhotoOfTheDay.and.returnValue(of(null));

    // Set up default return values
    userSettingsServiceSpy.getUserSettings.and.returnValue(of({
      userId: 'test-user',
      isSubscribed: true,
      tableView: false,
      entitiesPerPage: 5,
      currentFilmId: null
    } as UserSettingsDto));
    userSettingsServiceSpy.getSubscribedUsers.and.returnValue(of([]));
    userSettingsServiceSpy.updateUserSettings.and.returnValue(of(undefined));
    filmServiceSpy.getMyNotDevelopedFilmsAll.and.returnValue(of([]));
    filmServiceSpy.getExposureDates.and.returnValue(of([]));
    ideaServiceSpy.getAll.and.returnValue(of([]));

    await TestConfig.configureTestBed({
      declarations: [
        HomeComponent,
        WackyIdeasSectionComponent,
        UpsertIdeaComponent,
        FilmCheckSectionComponent,
        FilmCheckUserComponent,
        CurrentFilmSectionComponent,
        PhotoOfTheDaySectionComponent,
        SettingsSectionComponent
      ],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy },
        { provide: IdeaService, useValue: ideaServiceSpy },
        { provide: PhotoService, useValue: photoServiceSpy },
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

  it('should use default section order when settings omit homeSectionOrder', () => {
    expect(component.homeSectionOrder).toEqual([...DEFAULT_HOME_SECTION_ORDER]);
  });

  it('toggleEditHomeLayout should toggle isEditingHomeLayout', () => {
    expect(component.isEditingHomeLayout).toBe(false);
    component.toggleEditHomeLayout();
    expect(component.isEditingHomeLayout).toBe(true);
    component.toggleEditHomeLayout();
    expect(component.isEditingHomeLayout).toBe(false);
  });

  it('onHomeSectionDrop should reorder and call updateUserSettings', () => {
    component.userSettings = {
      userId: 'test-user',
      isSubscribed: true,
      tableView: false,
      entitiesPerPage: 5,
      currentFilmId: null,
    };
    component.homeSectionOrder = normalizeHomeSectionOrder(undefined);
    mockUserSettingsService.updateUserSettings.calls.reset();

    component.onHomeSectionDrop({
      previousIndex: 0,
      currentIndex: 1,
    } as unknown as CdkDragDrop<string[]>);

    expect(mockUserSettingsService.updateUserSettings).toHaveBeenCalled();
    const arg = mockUserSettingsService.updateUserSettings.calls.mostRecent().args[0] as UserSettingsDto;
    expect(arg.homeSectionOrder?.[0]).toBe(DEFAULT_HOME_SECTION_ORDER[1]);
    expect(arg.homeSectionOrder?.[1]).toBe(DEFAULT_HOME_SECTION_ORDER[0]);
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
