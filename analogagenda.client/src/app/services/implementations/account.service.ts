import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { IdentityDto } from '../../DTOs';
import { BaseService } from '../base.service';

@Injectable({
  providedIn: 'root'
})
export class AccountService extends BaseService {
  constructor() { super('Account') }

  login(email: string, password: string) {
    return this.post('login', {email, password});
  }

  changePassword(oldPassword: string, newPassword: string) {
    return this.post('changePassword', {oldPassword, newPassword});
  }

  isAuth() {
    return this.get('isAuth');
  }

  whoAmI(): Observable<IdentityDto> {
    return this.get<IdentityDto>('whoAmI');
  }

  logout() {
    return this.post('logout');
  }
}
