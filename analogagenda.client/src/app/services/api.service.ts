import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";

@Injectable({ providedIn: 'root' })
export class ApiService {
  #http = inject(HttpClient);

  login(email:string, password: string) {
    return this.#http.post('https://localhost:7125/account/login', { password }, { withCredentials: true });
  }

  secret() {
    return this.#http.get('https://localhost:7125/home/secret', { withCredentials: true, responseType: 'text' });
  }
}