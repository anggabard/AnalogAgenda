import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { ApiService } from "../services/api.service";
import { catchError, map } from "rxjs";

export const sessionGuard: CanActivateFn = () => {
  const api = inject(ApiService);
  const router = inject(Router);
  return api.secret().pipe(
    map(() => true),
    catchError(async () => router.createUrlTree(['/login']))
  );
};