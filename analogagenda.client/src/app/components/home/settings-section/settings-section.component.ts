import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { UserSettingsDto } from '../../../DTOs';
import { UserSettingsService } from '../../../services';

@Component({
  selector: 'app-settings-section',
  templateUrl: './settings-section.component.html',
  styleUrl: './settings-section.component.css',
  standalone: false
})
export class SettingsSectionComponent {
  @Input() userSettings: UserSettingsDto | null = null;
  @Output() settingsUpdated = new EventEmitter<void>();

  private userSettingsService = inject(UserSettingsService);

  onSettingsChange(): void {
    if (!this.userSettings) return;

    const updatedSettings = {
      userId: this.userSettings.userId,
      isSubscribed: this.userSettings.isSubscribed,
      tableView: this.userSettings.tableView,
      entitiesPerPage: this.userSettings.entitiesPerPage,
      currentFilmId: this.userSettings.currentFilmId
    };

    this.userSettingsService.updateUserSettings(updatedSettings).subscribe({
      next: () => {
        this.settingsUpdated.emit();
      },
      error: (error) => {
        console.error('Error updating settings:', error);
        this.settingsUpdated.emit();
      }
    });
  }
}
