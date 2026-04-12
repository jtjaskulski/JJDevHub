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
      // Wygasły token — nie uznajemy sesji za ważną do czasu udanego odświeżenia (AuthRefreshSuccess).
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

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly injector = inject(Injector);
  private readonly keycloakEvent = inject(KEYCLOAK_EVENT_SIGNAL, { optional: true });

  /** Keycloak jest skonfigurowany w `environment.keycloak.url`. */
  readonly keycloakConfigured = environment.keycloak.url.length > 0;

  /**
   * Reaktywny stan sesji — oparty o `KEYCLOAK_EVENT_SIGNAL` z keycloak-angular,
   * więc UI aktualizuje się po asynchronicznym `init` (w przeciwieństwie do samego `kc.authenticated`).
   */
  readonly isLoggedIn = computed(() => {
    if (!this.keycloakConfigured) return false;
    const sig = this.keycloakEvent;
    if (!sig) return false;
    return authenticatedFromKeycloakEvent(sig());
  });

  /**
   * Instancja keycloak-js rejestrowana przez `provideKeycloak` pod tokenem klasy `Keycloak`.
   * Pobieranie przez `Injector.get` zapewnia ten sam mechanizm co wewnątrz biblioteki (interceptory).
   */
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
