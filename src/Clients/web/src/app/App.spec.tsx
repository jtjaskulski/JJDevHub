import { cleanup, render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { afterEach, describe, expect, it } from 'vitest';

import { App } from './App';
import { AuthProvider } from './auth/auth';

afterEach(() => {
  cleanup();
  document.documentElement.lang = 'pl';
  localStorage.clear();
});

function renderAt(path: string) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[path]}>
        <App />
      </MemoryRouter>
    </AuthProvider>,
  );
}

describe('App', () => {
  it('renders the brand', async () => {
    renderAt('/pl');
    expect(screen.getByRole('link', { name: 'JJDevHub' }).textContent).toContain('JJDevHub');
  });

  it('keeps login unlocalized when switching language', async () => {
    renderAt('/login');
    screen.getByRole('button', { name: 'EN' }).click();
    expect(screen.getByRole('heading', { name: 'Log in' })).toBeTruthy();
  });

  it('sets the document language from the locale prefix', async () => {
    renderAt('/en');
    expect(document.documentElement.lang).toBe('en');
  });
});
