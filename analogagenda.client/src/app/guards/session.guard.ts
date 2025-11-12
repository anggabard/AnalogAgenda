import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { catchError, map, of } from "rxjs";
import { AccountService } from "../services/index";
import { HttpErrorResponse } from "@angular/common/http";

export const sessionGuard: CanActivateFn = () => {
  const api = inject(AccountService);
  const router = inject(Router);

  return api.isAuth().pipe(
    map(() => true),
    catchError((error: HttpErrorResponse) => {
      // Log the error to help diagnose why 401s are happening
      console.error('Session guard auth check failed:', {
        status: error.status,
        statusText: error.statusText,
        url: error.url,
        message: error.message,
        error: error.error
      });
      
      // Don't redirect to login on temporary server errors (503, 507, 500)
      // These are server issues, not authentication failures
      if (error.status === 503 || error.status === 507 || error.status === 500) {
        // Allow access even if auth check fails due to server issues
        console.warn(`Server error (${error.status}) during auth check - allowing access`);
        return of(true);
      }
      // Only redirect to login on actual authentication failures
      console.warn('Authentication failed - redirecting to login');
      return of(router.createUrlTree(['/login']));
    })
  );
};
