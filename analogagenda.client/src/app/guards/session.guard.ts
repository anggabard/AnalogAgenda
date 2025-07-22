import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { catchError, map, of } from "rxjs";
import { AccountService } from "../services/index";

export const sessionGuard: CanActivateFn = () => {
  const api = inject(AccountService);
  const router = inject(Router);

  return api.isAuth().pipe(
    map(() => true),
    catchError(() => {
      return of(router.createUrlTree(['/login']));
    })
  );
};
