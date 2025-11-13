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
      // Only handle 401 (Unauthorized) and 403 (Forbidden) - redirect to login
      if (error.status === 401 || error.status === 403) {
        return of(router.createUrlTree(['/login']));
      }
      // For all other errors (including 5xx), allow access
      return of(true);
    })
  );
};
