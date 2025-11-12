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
      // Don't redirect to login on temporary server errors (503, 507)
      // These are server overload issues, not authentication failures
      if (error.status === 503 || error.status === 507) {
        // Allow access even if auth check fails due to server overload
        return of(true);
      }
      // Only redirect to login on actual authentication failures
      return of(router.createUrlTree(['/login']));
    })
  );
};
