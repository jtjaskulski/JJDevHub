import type { ColorScheme } from '@jjdevhub/theme';
import { type ReactNode, useEffect, useState } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogTitle } from '@/components/ui/dialog';

import { localeFromPathname, pathForLocale, siteFor } from './content/content';
import type { LocaleCode } from './content/content.model';
import { ShellNav } from './ShellNav';
import { applyColorScheme, readColorScheme, storeColorScheme } from './theme/scheme';

type ShellProps = {
  children: ReactNode;
};

export function Shell({ children }: ShellProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const locale = localeFromPathname(location.pathname);
  const nav = siteFor(locale).nav;
  const [scheme, setScheme] = useState<ColorScheme>(readColorScheme);
  const [menuOpen, setMenuOpen] = useState(false);

  useEffect(() => {
    const previous = document.documentElement.lang;
    document.documentElement.lang = locale;
    return () => {
      document.documentElement.lang = previous;
    };
  }, [locale]);

  useEffect(() => {
    applyColorScheme(scheme);
  }, [scheme]);

  function switchLocale(next: LocaleCode): void {
    if (next === locale) {
      return;
    }
    const target = pathForLocale(location.pathname, location.search, location.hash, next);
    if (!target) {
      return;
    }
    void navigate(target, { viewTransition: true });
  }

  function showPolish(): void {
    switchLocale('pl');
  }

  function showEnglish(): void {
    switchLocale('en');
  }

  function toggleScheme(): void {
    const next = scheme === 'light' ? 'dark' : 'light';
    storeColorScheme(next);
    applyColorScheme(next);
    setScheme(next);
  }

  function openMenu(): void {
    setMenuOpen(true);
  }

  function closeMenu(): void {
    setMenuOpen(false);
  }

  const themeLabel = scheme === 'light' ? nav.themeDark : nav.themeLight;
  const polishClass = locale === 'pl' ? 'text-ink' : 'text-muted';
  const englishClass = locale === 'en' ? 'text-ink' : 'text-muted';

  return (
    <div className="flex min-h-svh flex-col bg-canvas text-ink">
      <header className="sticky top-0 z-40 border-b border-ink/15 bg-canvas">
        <div className="mx-auto flex w-full max-w-3xl items-center gap-4 px-4 py-3 md:px-6">
          <NavLink to={`/${locale}`} end className="shrink-0 text-small font-semibold text-ink no-underline" viewTransition>
            JJDevHub
          </NavLink>
          <ShellNav
            locale={locale}
            nav={nav}
            className="hidden min-w-0 flex-1 items-center gap-4 md:flex"
          />
          <div className="ml-auto flex shrink-0 items-center gap-1 md:ml-0">
            <div className="flex gap-1" role="group" aria-label={nav.language}>
              <button type="button" className={`rounded-pill px-3 py-1 text-caption ${polishClass}`} onClick={showPolish}>
                {nav.languagePl}
              </button>
              <button type="button" className={`rounded-pill px-3 py-1 text-caption ${englishClass}`} onClick={showEnglish}>
                {nav.languageEn}
              </button>
            </div>
            <button
              type="button"
              className="rounded-pill px-3 py-1 text-caption text-muted"
              aria-pressed={scheme === 'dark'}
              onClick={toggleScheme}
            >
              {themeLabel}
            </button>
            <Button type="button" variant="ghost" className="md:hidden" onClick={openMenu}>
              {nav.menu}
            </Button>
          </div>
        </div>
      </header>
      <Dialog open={menuOpen} onOpenChange={setMenuOpen}>
        <DialogContent>
          <DialogTitle className="text-h4 tracking-h4">{nav.menu}</DialogTitle>
          <ShellNav
            locale={locale}
            nav={nav}
            className="flex flex-col items-start gap-3"
            onNavigate={closeMenu}
          />
        </DialogContent>
      </Dialog>
      <main className="w-full flex-1">{children}</main>
      <footer className="border-t border-ink/15 px-4 py-6 md:px-6">
        <p className="mx-auto w-full max-w-3xl text-small text-muted">JJDevHub {__APP_VERSION__}</p>
      </footer>
    </div>
  );
}
