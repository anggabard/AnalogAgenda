import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { FilmCheckSectionComponent } from '../../components/home/film-check-section/film-check-section.component';
import { FilmCheckUserComponent } from '../../components/home/film-check-section/film-check-user/film-check-user.component';
import { FilmService, UserSettingsService } from '../../services';
import { TestConfig } from '../test.config';
import { FilmDto } from '../../DTOs';
import { FilmType, UsernameType } from '../../enums';

describe('FilmCheckSectionComponent', () => {
  let component: FilmCheckSectionComponent;
  let fixture: ComponentFixture<FilmCheckSectionComponent>;
  let mockFilmService: jasmine.SpyObj<FilmService>;
  let mockUserSettingsService: jasmine.SpyObj<UserSettingsService>;

  beforeEach(async () => {
    const filmServiceSpy = jasmine.createSpyObj('FilmService', ['getNotDevelopedFilms']);
    const userSettingsServiceSpy = jasmine.createSpyObj('UserSettingsService', ['getSubscribedUsers']);

    filmServiceSpy.getNotDevelopedFilms.and.returnValue(of([]));
    userSettingsServiceSpy.getSubscribedUsers.and.returnValue(of([
      { username: UsernameType.Angel },
      { username: UsernameType.Tudor }
    ]));

    await TestConfig.configureTestBed({
      declarations: [FilmCheckSectionComponent, FilmCheckUserComponent],
      providers: [
        { provide: FilmService, useValue: filmServiceSpy },
        { provide: UserSettingsService, useValue: userSettingsServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(FilmCheckSectionComponent);
    component = fixture.componentInstance;
    mockFilmService = TestBed.inject(FilmService) as jasmine.SpyObj<FilmService>;
    mockUserSettingsService = TestBed.inject(UserSettingsService) as jasmine.SpyObj<UserSettingsService>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load user stats on init', () => {
    expect(mockUserSettingsService.getSubscribedUsers).toHaveBeenCalled();
    expect(mockFilmService.getNotDevelopedFilms).toHaveBeenCalled();
  });

  it('should display rows for subscribed users plus Total when no films', () => {
    expect(component.displayRows.length).toBe(3);
    const users = component.displayRows.map(r => r.user);
    expect(users).toContain('Angel');
    expect(users).toContain('Tudor');
    expect(users).toContain('Total');
    component.displayRows.forEach(row => {
      expect(row.count).toBe(0);
      expect(row.countsByType[FilmType.ColorNegative]).toBe(0);
      expect(row.countsByType[FilmType.ColorPositive]).toBe(0);
      expect(row.countsByType[FilmType.BlackAndWhite]).toBe(0);
    });
  });

  it('should set popoverOpen when onPopoverShow is called', () => {
    expect(component.popoverOpen).toBe(false);
    component.onPopoverShow();
    expect(component.popoverOpen).toBe(true);
  });

  it('should clear popoverOpen when onPopoverHide is called', () => {
    component.popoverOpen = true;
    component.onPopoverHide();
    expect(component.popoverOpen).toBe(false);
  });

  it('should aggregate counts by user and include Total row when films exist', () => {
    const films: FilmDto[] = [
      { id: '1', brand: 'Fuji', purchasedBy: UsernameType.Angel, type: FilmType.ColorNegative } as FilmDto,
      { id: '2', brand: 'Kodak', purchasedBy: UsernameType.Angel, type: FilmType.BlackAndWhite } as FilmDto,
      { id: '3', brand: 'Ilford', purchasedBy: UsernameType.Tudor, type: FilmType.ColorNegative } as FilmDto
    ];
    mockFilmService.getNotDevelopedFilms.and.returnValue(of(films));
    mockUserSettingsService.getSubscribedUsers.and.returnValue(of([
      { username: UsernameType.Angel },
      { username: UsernameType.Tudor }
    ]));

    fixture = TestBed.createComponent(FilmCheckSectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    expect(component.displayRows.length).toBe(3);
    const angelRow = component.displayRows.find(r => r.user === 'Angel');
    const tudorRow = component.displayRows.find(r => r.user === 'Tudor');
    const totalRow = component.displayRows.find(r => r.user === 'Total');
    expect(angelRow?.count).toBe(2);
    expect(tudorRow?.count).toBe(1);
    expect(totalRow?.count).toBe(3);
    expect(totalRow?.countsByType[FilmType.ColorNegative]).toBe(2);
    expect(totalRow?.countsByType[FilmType.BlackAndWhite]).toBe(1);
  });

  it('should show no rows when no subscribed users', () => {
    mockUserSettingsService.getSubscribedUsers.and.returnValue(of([]));
    fixture = TestBed.createComponent(FilmCheckSectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    expect(component.displayRows).toEqual([]);
  });
});
