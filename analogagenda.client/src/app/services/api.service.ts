import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";

@Injectable({ providedIn: 'root' })
export class ApiService {
  #http = inject(HttpClient);

  login(email:string, password: string) {
    return this.#http.post('/api/account/login', { email, password }, { withCredentials: true });
  }

  secret() {
    return this.#http.get('/api/home/secret', { withCredentials: true, responseType: 'text' });
  }

  whoAmI() {
    return this.#http.get('/api/account/whoAmI', { withCredentials: true });
  }

  logout() {
    return this.#http.post('/api/account/logout', {}, { withCredentials: true });
  }
}
