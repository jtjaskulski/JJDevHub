import { Link } from 'react-router';

import type { LocaleCode, StackLinkContent } from '../../content/content.model';

type StackLineProps = {
  locale: LocaleCode;
  stack: readonly StackLinkContent[];
};

function isExternal(path: string): boolean {
  return path.startsWith('https://') || path.startsWith('http://');
}

export function StackLine({ locale, stack }: StackLineProps) {
  return (
    <p className="flex flex-wrap items-baseline gap-4">
      {stack.map((item) => {
        const className = item.accent
          ? 'text-h4 tracking-h4 text-accent no-underline'
          : 'text-small tracking-small text-dark-chapter-fg/70 no-underline';
        if (isExternal(item.path)) {
          return (
            <a key={item.label} href={item.path} target="_blank" rel="noopener noreferrer" className={className}>
              {item.label}
            </a>
          );
        }
        return (
          <Link key={item.label} to={`/${locale}/${item.path}`} viewTransition className={className}>
            {item.label}
          </Link>
        );
      })}
    </p>
  );
}
