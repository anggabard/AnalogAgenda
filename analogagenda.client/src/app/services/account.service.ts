import { Injectable } from '@angular/core';
import { BaseService } from './base.service';

@Injectable({
  providedIn: 'root'
})
export class AccountService extends BaseService {
  constructor() { super('Account') }

  login(email: string, password: string) {
    return this.post('login', {email, password});
  }

  secret() {
    return this.get('secret', {responseType: 'text'});
  }

  whoAmI() {
    return this.get('whoAmI');
  }

  logout() {
    return this.post('logout');
  }
}
