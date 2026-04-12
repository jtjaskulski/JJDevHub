import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

/** Admin: Owner (Keycloak) albo dev bypass gdy IdP wyłączony. */
export const adminAuthGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.keycloakConfigured) {
    if (environment.allowAdminWithoutAuth) return true;
    void router.navigate(['/']);
    return false;
  }

  if (!auth.isLoggedIn()) {
    void router.navigate(['/']);
    return false;
  }

  if (auth.isOwner()) return true;

  void router.navigate(['/']);
  return false;
};
