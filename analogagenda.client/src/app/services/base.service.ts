import { inject } from "@angular/core";
import { environment } from "../../environments/environment";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";

export class BaseService {
    #http = inject(HttpClient);
    baseUrl: string;
    defaultOptions = { withCredentials: true };

    constructor(scope: string) {
        this.baseUrl = environment.apiUrl + '/' + scope;
    }

    get(path: string, options?: any): Observable<Object> {
        return this.#http.get(this.baseUrl + this.ensureLeadingSlash(path), { ...this.defaultOptions, ...options });
    }

    post(path: string, body: any = {}, options?: any): Observable<Object> {
        return this.#http.post(this.baseUrl + this.ensureLeadingSlash(path), body, { ...this.defaultOptions, ...options });
    }

    private ensureLeadingSlash(input: string): string {
    const trimmed = input.trim();
    if (trimmed.startsWith('/')) {
        return trimmed;
    }
    return '/' + trimmed;
}
}