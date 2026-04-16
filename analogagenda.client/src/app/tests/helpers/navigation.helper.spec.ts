import { UrlTree, Router } from '@angular/router';
import { openRouteInNewTab } from '../../helpers/navigation.helper';

describe('openRouteInNewTab', () => {
  let windowOpenSpy: jasmine.Spy<(url?: string | URL, target?: string, features?: string) => Window | null>;

  beforeEach(() => {
    windowOpenSpy = spyOn(window, 'open').and.returnValue(null);
  });

  it('should build URL via createUrlTree and serializeUrl, open with blank target and noopener/noreferrer, and not navigate in-app', () => {
    const fakeTree = {} as UrlTree;
    const navigateSpy = jasmine.createSpy('navigate');
    const createUrlTreeSpy = jasmine.createSpy('createUrlTree').and.returnValue(fakeTree);
    const serializeUrlSpy = jasmine.createSpy('serializeUrl').and.returnValue('/films/x1');

    const router = {
      createUrlTree: createUrlTreeSpy,
      serializeUrl: serializeUrlSpy,
      navigate: navigateSpy,
    } as unknown as Router;

    const commands = ['/films', 'x1'];

    openRouteInNewTab(router, commands);

    expect(createUrlTreeSpy).toHaveBeenCalledWith(commands);
    expect(serializeUrlSpy).toHaveBeenCalledWith(fakeTree);
    expect(windowOpenSpy).toHaveBeenCalledWith('/films/x1', '_blank', 'noopener,noreferrer');
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
