import { Router } from '@angular/router';

export function openRouteInNewTab(router: Router, commands: unknown[]): void {
  const url = router.serializeUrl(router.createUrlTree(commands));
  window.open(url, '_blank', 'noopener,noreferrer');
}
