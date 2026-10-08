import { useEffect, type ReactNode } from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router';

import { localeFromPathname, pathForLocale, siteFor } from './content/content';
import type { LocaleCode } from './content/content.model';
import { startLenis, stopLenis } from './motion/lenis';
import './app.scss';

type ShellProps = {
  children: ReactNode;
};

function navClass({ isActive }: { isActive: boolean }): string | undefined {
  return isActive ? 'is-active' : undefined;
}

export function Shell({ children }: ShellProps) {
  const location = useLocation();
  const navigate = useNavigate();
  const locale = localeFromPathname(location.pathname);
  const nav = siteFor(locale).nav;

  useEffect(() => {
    const previous = document.documentElement.lang;
    document.documentElement.lang = locale;
    return () => {
      document.documentElement.lang = previous;
    };
  }, [locale]);

  useEffect(() => {
    startLenis();
    return () => stopLenis();
  }, []);

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

  const polishClass = locale === 'pl' ? 'lang-btn is-active' : 'lang-btn';
  const englishClass = locale === 'en' ? 'lang-btn is-active' : 'lang-btn';

  return (
    <div className="shell">
      <header className="shell-nav">
        <NavLink to={`/${locale}`} end className="brand" viewTransition>
          JJDevHub
        </NavLink>
        <nav aria-label={nav.language}>
          <NavLink to={`/${locale}`} end className={navClass} viewTransition>
            {nav.home}
          </NavLink>
          <NavLink to={`/${locale}/cv`} className={navClass} viewTransition>
            {nav.cv}
          </NavLink>
          <NavLink to={`/${locale}/courses`} className={navClass} viewTransition>
            {nav.courses}
          </NavLink>
          <NavLink to={`/${locale}/compendium`} className={navClass} viewTransition>
            {nav.compendium}
          </NavLink>
          <NavLink to={`/${locale}/notes`} className={navClass} viewTransition>
            {nav.notes}
          </NavLink>
        </nav>
        <div className="lang" role="group" aria-label={nav.language}>
          <button type="button" className={polishClass} onClick={showPolish}>
            {nav.languagePl}
          </button>
          <button type="button" className={englishClass} onClick={showEnglish}>
            {nav.languageEn}
          </button>
        </div>
      </header>
      <main className="shell-main">{children}</main>
      <footer className="shell-footer">
        <p className="shell-footer-brand">JJDevHub</p>
      </footer>
    </div>
  );
}
