import { inject, Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { BaseService } from './base.service';

@Injectable({
  providedIn: 'root'
})
export class AccountService extends BaseService {
  constructor() { super('Account') }

  #http = inject(HttpClient);

  login(email: string, password: string) {
    return this.#http.post(this.baseUrl + '/login', { email, password }, { withCredentials: true });
  }

  secret() {
    return this.#http.get(this.baseUrl + '/secret', { withCredentials: true, responseType: 'text' });
  }

  whoAmI() {
    console.log(this.baseUrl);
    return this.#http.get(this.baseUrl + '/whoAmI', { withCredentials: true });
  }

  logout() {
    return this.#http.post(this.baseUrl + '/logout', {}, { withCredentials: true });
  }
}
