import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";

@Injectable({ providedIn: 'root' })
export class ApiService {
  #http = inject(HttpClient);

  login(email:string, password: string) {
    return this.#http.post('/account/login', { email, password }, { withCredentials: true });
  }

  secret() {
    return this.#http.get('/home/secret', { withCredentials: true, responseType: 'text' });
  }

  whoAmI() {
    return this.#http.get('/account/whoAmI', { withCredentials: true });
  }

  logout() {
    return this.#http.post('/account/logout', {}, { withCredentials: true });
  }
}
