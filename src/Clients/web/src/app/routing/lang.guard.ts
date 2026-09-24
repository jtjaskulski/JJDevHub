import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { ContentService } from '../content/content.service';

/** Invalid `:lang` segments redirect to `/pl`. */
export const langGuard: CanActivateFn = (route) => {
  const lang = route.paramMap.get('lang');
  if (ContentService.isLocaleCode(lang)) {
    return true;
  }
  return inject(Router).createUrlTree(['/pl']);
};
