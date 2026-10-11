import { NavLink } from 'react-router';

import type { LocaleCode, NavContent } from './content/content.model';

type ShellNavProps = {
  locale: LocaleCode;
  nav: NavContent;
  className: string;
  onNavigate?: () => void;
};

function linkClass({ isActive }: { isActive: boolean }): string {
  const base = 'text-small no-underline';
  return isActive ? `${base} text-ink` : `${base} text-muted`;
}

export function ShellNav({ locale, nav, className, onNavigate }: ShellNavProps) {
  function close(): void {
    onNavigate?.();
  }

  return (
    <nav aria-label={nav.menu} className={className}>
      <NavLink to={`/${locale}`} end className={linkClass} viewTransition onClick={close}>
        {nav.home}
      </NavLink>
      <NavLink to={`/${locale}/cv`} className={linkClass} viewTransition onClick={close}>
        {nav.cv}
      </NavLink>
      <NavLink to={`/${locale}/courses`} className={linkClass} viewTransition onClick={close}>
        {nav.courses}
      </NavLink>
      <NavLink to={`/${locale}/compendium`} className={linkClass} viewTransition onClick={close}>
        {nav.compendium}
      </NavLink>
      <NavLink to={`/${locale}/notes`} className={linkClass} viewTransition onClick={close}>
        {nav.notes}
      </NavLink>
    </nav>
  );
}
