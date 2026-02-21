import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { SettingsSectionComponent } from '../../components/home/settings-section/settings-section.component';
import { UserSettingsService } from '../../services';
import { TestConfig } from '../test.config';
import { UserSettingsDto } from '../../DTOs';

describe('SettingsSectionComponent', () => {
  let component: SettingsSectionComponent;
  let fixture: ComponentFixture<SettingsSectionComponent>;
  let mockUserSettingsService: jasmine.SpyObj<UserSettingsService>;

  const mockUserSettings: UserSettingsDto = {
    userId: 'user1',
    isSubscribed: true,
    tableView: false,
    entitiesPerPage: 10,
    currentFilmId: 'film-1'
  };

  beforeEach(async () => {
    mockUserSettingsService = jasmine.createSpyObj('UserSettingsService', ['getUserSettings', 'updateUserSettings']);
    mockUserSettingsService.updateUserSettings.and.returnValue(of(undefined));

    await TestConfig.configureTestBed({
      declarations: [SettingsSectionComponent],
      providers: [{ provide: UserSettingsService, useValue: mockUserSettingsService }]
    }).compileComponents();

    fixture = TestBed.createComponent(SettingsSectionComponent);
    component = fixture.componentInstance;
    component.userSettings = { ...mockUserSettings };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display settings form when userSettings is set', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Settings');
    expect(compiled.textContent).toContain('Subscribed');
    expect(compiled.textContent).toContain('Table View');
    expect(compiled.textContent).toContain('Entities per Page');
  });

  it('onSettingsChange should call updateUserSettings with current settings', () => {
    component.onSettingsChange();
    expect(mockUserSettingsService.updateUserSettings).toHaveBeenCalledWith({
      userId: mockUserSettings.userId,
      isSubscribed: mockUserSettings.isSubscribed,
      tableView: mockUserSettings.tableView,
      entitiesPerPage: mockUserSettings.entitiesPerPage,
      currentFilmId: mockUserSettings.currentFilmId
    });
  });

  it('onSettingsChange should emit settingsUpdated on success', () => {
    spyOn(component.settingsUpdated, 'emit');
    component.onSettingsChange();
    expect(component.settingsUpdated.emit).toHaveBeenCalled();
  });

  it('onSettingsChange should NOT emit settingsUpdated on error', () => {
    mockUserSettingsService.updateUserSettings.and.returnValue(
      throwError(() => ({ status: 500, message: 'Server error' }))
    );
    spyOn(component.settingsUpdated, 'emit');
    spyOn(console, 'error');

    component.onSettingsChange();

    expect(component.settingsUpdated.emit).not.toHaveBeenCalled();
    expect(console.error).toHaveBeenCalledWith('Error updating settings:', jasmine.any(Object));
  });

  it('onSettingsChange should do nothing when userSettings is null', () => {
    component.userSettings = null;
    component.onSettingsChange();
    expect(mockUserSettingsService.updateUserSettings).not.toHaveBeenCalled();
  });
});
