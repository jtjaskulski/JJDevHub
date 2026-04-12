import { Injectable, computed, inject, Injector } from '@angular/core';
import Keycloak from 'keycloak-js';
import {
  KEYCLOAK_EVENT_SIGNAL,
  KeycloakEventType,
  type KeycloakEvent,
} from 'keycloak-angular';
import { environment } from '../../environments/environment';

/** Stan zalogowania na podstawie sygnału zdarzeń keycloak-angular (spójny z `provideKeycloak`). */
function authenticatedFromKeycloakEvent(ev: KeycloakEvent): boolean {
  switch (ev.type) {
    case KeycloakEventType.Ready:
      return Boolean(ev.args as boolean);
    case KeycloakEventType.AuthSuccess:
    case KeycloakEventType.AuthRefreshSuccess:
      return true;
    case KeycloakEventType.TokenExpired:
      return false;
    case KeycloakEventType.AuthLogout:
    case KeycloakEventType.AuthError:
    case KeycloakEventType.AuthRefreshError:
    case KeycloakEventType.KeycloakAngularInit:
    case KeycloakEventType.KeycloakAngularNotInitialized:
    case KeycloakEventType.ActionUpdate:
      return false;
    default:
      return false;
  }
}

function realmRolesFromKeycloak(kc: Keycloak | null): string[] {
  if (!kc?.tokenParsed) return [];
  const tp = kc.tokenParsed as Record<string, unknown>;
  const ra = tp['realm_access'] as { roles?: string[] } | undefined;
  return ra?.roles ?? [];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly injector = inject(Injector);
  private readonly keycloakEvent = inject(KEYCLOAK_EVENT_SIGNAL, { optional: true });

  /** Keycloak jest skonfigurowany w `environment.keycloak.url`. */
  readonly keycloakConfigured = environment.keycloak.url.length > 0;

  readonly isLoggedIn = computed(() => {
    if (!this.keycloakConfigured) return false;
    const sig = this.keycloakEvent;
    if (!sig) return false;
    return authenticatedFromKeycloakEvent(sig());
  });

  /**
   * Role realm z JWT (odświeżane przy zdarzeniach Keycloak).
   */
  readonly userRoles = computed(() => {
    if (!this.keycloakConfigured) return [] as string[];
    const sig = this.keycloakEvent;
    void sig?.();
    return realmRolesFromKeycloak(this.getKeycloak());
  });

  readonly isOwner = computed(() => this.userRoles().includes('Owner'));

  private getKeycloak(): Keycloak | null {
    return this.injector.get(Keycloak, null, { optional: true });
  }

  login(): void {
    void this.getKeycloak()?.login();
  }

  logout(): void {
    void this.getKeycloak()?.logout();
  }
}
