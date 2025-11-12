import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { catchError, map, of, timer, throwError } from "rxjs";
import { retryWhen, mergeMap } from "rxjs/operators";
import { AccountService } from "../services/index";
import { HttpErrorResponse } from "@angular/common/http";

export const sessionGuard: CanActivateFn = () => {
  const api = inject(AccountService);
  const router = inject(Router);

  return api.isAuth().pipe(
    // Retry once with a short delay if we get a 401 - might be a temporary backend error
    // This prevents logout on transient backend errors after uploads
    retryWhen(errors =>
      errors.pipe(
        mergeMap((error: HttpErrorResponse, retryCount: number) => {
          // Only retry once on 401 errors - might be temporary backend issue
          if (error.status === 401 && retryCount === 0) {
            console.warn('Auth check returned 401, retrying once...');
            return timer(500); // 500ms delay before retry
          }
          // Don't retry other errors or after first retry
          return throwError(() => error);
        })
      )
    ),
    map(() => true),
    catchError((error: HttpErrorResponse) => {
      // Don't redirect to login on temporary server errors (503, 507, 500)
      // These are server issues, not authentication failures
      if (error.status === 503 || error.status === 507 || error.status === 500) {
        // Allow access even if auth check fails due to server issues
        console.warn(`Server error (${error.status}) during auth check - allowing access`);
        return of(true);
      }
      // Only redirect to login on actual authentication failures (401 after retry, or other auth errors)
      console.warn('Authentication failed - redirecting to login');
      return of(router.createUrlTree(['/login']));
    })
  );
};
