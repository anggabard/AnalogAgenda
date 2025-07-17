import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AccountService } from "../services/index";
import { catchError, map, of } from "rxjs";

export const sessionGuard: CanActivateFn = () => {
  const api = inject(AccountService);
  const router = inject(Router);

  return api.secret().pipe(
    map(() => true),
    catchError(() => {
      return of(router.createUrlTree(['/login']));
    })
  );
};