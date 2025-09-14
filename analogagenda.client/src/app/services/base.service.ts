import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../environments/environment";

export class BaseService {
    #http = inject(HttpClient);
    baseUrl: string;
    defaultOptions = { withCredentials: true };

    constructor(scope: string) {
        this.baseUrl = environment.apiUrl + '/' + scope;
    }

    get<T>(path: string, options?: any): Observable<T> {
        return this.#http.get<T>(this.baseUrl + this.ensureLeadingSlash(path), {
            ...this.defaultOptions,
            ...options,
            observe: 'body'
        }) as Observable<T>;
    }

    post<T>(path: string, body: any = {}, options?: any): Observable<T> {
        return this.#http.post<T>(this.baseUrl + this.ensureLeadingSlash(path), body, {
            ...this.defaultOptions,
            ...options,
            observe: 'body'
        }) as Observable<T>;
    }

    put<T>(path: string, body: any = {}, options?: any): Observable<T> {
        return this.#http.put<T>(this.baseUrl + this.ensureLeadingSlash(path), body, {
            ...this.defaultOptions,
            ...options,
            observe: 'body'
        }) as Observable<T>;
    }

    delete<T>(path: string, options?: any): Observable<T> {
        return this.#http.delete<T>(this.baseUrl + this.ensureLeadingSlash(path), {
            ...this.defaultOptions,
            ...options,
            observe: 'body'
        }) as Observable<T>;
    }

    private ensureLeadingSlash(input: string): string {
        const trimmed = input.trim();
        if (trimmed.startsWith('/')) {
            return trimmed;
        }
        return '/' + trimmed;
    }
}
