import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

/** Dostęp do tras admin: zalogowany (Keycloak) albo dev bypass gdy IdP wyłączony. */
export const adminAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  if (!auth.keycloakConfigured && environment.allowAdminWithoutAuth) return true;

  void router.navigate(['/']);
  return false;
};
