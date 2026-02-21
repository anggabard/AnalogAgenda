import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CurrentFilmSectionComponent } from '../../components/home/current-film-section/current-film-section.component';
import { TestConfig } from '../test.config';
import { FilmDto, UserSettingsDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';

describe('CurrentFilmSectionComponent', () => {
  let component: CurrentFilmSectionComponent;
  let fixture: ComponentFixture<CurrentFilmSectionComponent>;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockUserSettings: UserSettingsDto = {
    userId: 'user1',
    isSubscribed: true,
    tableView: false,
    entitiesPerPage: 10,
    currentFilmId: 'film-1'
  };

  const mockFilm: FilmDto = {
    id: 'film-1',
    name: 'Test Film',
    brand: 'Kodak',
    iso: '400',
    type: FilmType.ColorNegative,
    numberOfExposures: 36,
    cost: 10,
    purchasedBy: UsernameType.Angel,
    purchasedOn: '2024-01-01',
    imageUrl: '/img.jpg',
    description: '',
    developed: false
  };

  beforeEach(async () => {
    routerSpy = TestConfig.createRouterSpy();

    await TestConfig.configureTestBed({
      declarations: [CurrentFilmSectionComponent],
      providers: [{ provide: Router, useValue: routerSpy }]
    }).compileComponents();

    fixture = TestBed.createComponent(CurrentFilmSectionComponent);
    component = fixture.componentInstance;
    component.userSettings = mockUserSettings;
    component.currentFilm = mockFilm;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display current film when currentFilm is set', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Test Film');
    expect(compiled.textContent).toContain('400');
    expect(compiled.textContent).toContain('Color Negative');
    expect(compiled.textContent).toContain('36');
  });

  it('editCurrentFilm should navigate to film edit when currentFilm is set', () => {
    component.editCurrentFilm();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/films', 'film-1']);
  });

  it('editCurrentFilm should not navigate when currentFilm is null', () => {
    component.currentFilm = null;
    fixture.detectChanges();
    component.editCurrentFilm();
    expect(routerSpy.navigate).not.toHaveBeenCalled();
  });

  it('onChangeCurrentFilm should emit changeCurrentFilmRequested', () => {
    spyOn(component.changeCurrentFilmRequested, 'emit');
    component.onChangeCurrentFilm();
    expect(component.changeCurrentFilmRequested.emit).toHaveBeenCalled();
  });

  it('Edit Film button should call editCurrentFilm', () => {
    spyOn(component, 'editCurrentFilm');
    const compiled = fixture.nativeElement as HTMLElement;
    const editButton = Array.from(compiled.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Edit Film'
    );
    editButton?.click();
    expect(component.editCurrentFilm).toHaveBeenCalled();
  });

  it('Change Current Film button should call onChangeCurrentFilm', () => {
    spyOn(component, 'onChangeCurrentFilm');
    const compiled = fixture.nativeElement as HTMLElement;
    const changeButton = Array.from(compiled.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Change Current Film'
    );
    changeButton?.click();
    expect(component.onChangeCurrentFilm).toHaveBeenCalled();
  });

  it('should show no current film message and Select button when currentFilm is null', () => {
    component.currentFilm = null;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No current film selected');
    const selectButton = Array.from(compiled.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Select Current Film'
    );
    expect(selectButton).toBeTruthy();
  });
});
