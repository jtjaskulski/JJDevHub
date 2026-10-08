import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';

import { describeApiError, readApiError, readToken } from './api-error';

const TOKEN_KEY = 'jjdevhub.token';

type AuthValue = {
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthValue | null>(null);

function readStoredToken(): string | null {
  if (typeof localStorage === 'undefined') {
    return null;
  }
  return localStorage.getItem(TOKEN_KEY);
}

function storeToken(token: string | null): void {
  if (typeof localStorage === 'undefined') {
    return;
  }
  if (token) {
    localStorage.setItem(TOKEN_KEY, token);
  } else {
    localStorage.removeItem(TOKEN_KEY);
  }
}

async function postAuth(path: 'login' | 'register', email: string, password: string): Promise<string> {
  const response = await fetch(`/api/auth/${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!response.ok) {
    const payload: unknown = await response.json().catch(() => null);
    throw readApiError(response.status, payload);
  }
  const payload: unknown = await response.json();
  const token = readToken(payload);
  if (!token) {
    throw readApiError(response.status, { message: 'Request failed.' });
  }
  return token;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(readStoredToken);

  const value = useMemo<AuthValue>(
    () => ({
      isAuthenticated: !!token,
      async login(email, password) {
        const next = await postAuth('login', email, password);
        storeToken(next);
        setToken(next);
      },
      async register(email, password) {
        const next = await postAuth('register', email, password);
        storeToken(next);
        setToken(next);
      },
      logout() {
        storeToken(null);
        setToken(null);
      },
    }),
    [token],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValue {
  const value = useContext(AuthContext);
  if (!value) {
    throw new Error('useAuth requires AuthProvider');
  }
  return value;
}

export { describeApiError };
