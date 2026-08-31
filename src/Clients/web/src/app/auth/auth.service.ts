import { computed, inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';

import { JjdevhubApiClient, LoginRequest, RegisterRequest } from '../api/jjdevhub-api.client';

const TOKEN_KEY = 'jjdevhub.token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(JjdevhubApiClient);

  readonly token = signal<string | null>(readToken());
  readonly isAuthenticated = computed(() => !!this.token());

  login(email: string, password: string) {
    return this.api.login(new LoginRequest({ email, password })).pipe(
      tap((response) => this.setToken(response.token)),
    );
  }

  register(email: string, password: string) {
    return this.api.register(new RegisterRequest({ email, password })).pipe(
      tap((response) => this.setToken(response.token)),
    );
  }

  me() {
    return this.api.me();
  }

  health() {
    return this.api.health();
  }

  logout(): void {
    this.setToken(null);
  }

  private setToken(token: string | null): void {
    this.token.set(token);
    if (typeof localStorage === 'undefined') {
      return;
    }
    if (token) {
      localStorage.setItem(TOKEN_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_KEY);
    }
  }
}

function readToken(): string | null {
  if (typeof localStorage === 'undefined') {
    return null;
  }
  return localStorage.getItem(TOKEN_KEY);
}
