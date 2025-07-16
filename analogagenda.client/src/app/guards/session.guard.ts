import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AccountService } from "../services/index";
import { catchError, map } from "rxjs";

export const sessionGuard: CanActivateFn = () => {
  const api = inject(AccountService);
  const router = inject(Router);
  return api.secret().pipe(
    map(() => true),
    catchError(async () => router.createUrlTree(['/login']))
  );
};