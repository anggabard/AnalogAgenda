import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseService } from '../base.service';
import { UserSettingsDto } from '../../DTOs/user-settings.dto';

@Injectable({
  providedIn: 'root'
})
export class UserSettingsService extends BaseService {
  constructor() {
    super('UserSettings');
  }

  getUserSettings(): Observable<UserSettingsDto> {
    return this.get<UserSettingsDto>('');
  }

  updateUserSettings(settings: Partial<UserSettingsDto>): Observable<void> {
    return this.patch<void>('', settings);
  }

  getSubscribedUsers(): Observable<Array<{ username: string }>> {
    return this.get<Array<{ username: string }>>('subscribed-users');
  }
}

