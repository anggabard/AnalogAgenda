import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { catchError, map, of } from "rxjs";
import { AccountService } from "../services/index";

export const loginGuard: CanActivateFn = () => {
    const api = inject(AccountService);
    const router = inject(Router);

    return api.isAuth().pipe(
        map(() => {
            router.navigate(['/home']);
            return false;
        }),
        catchError(() => {
            return of(true);
        })
    );
}
