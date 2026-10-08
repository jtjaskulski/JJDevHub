import { Link, useLocation } from 'react-router';

import { localeFromPathname, siteFor } from '../../content/content';
import './compendium.scss';

export function CompendiumPage() {
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).compendium;

  return (
    <section className="compendium-page page">
      <header className="page-head reveal">
        <h1>{section.title}</h1>
        <p>{section.lead}</p>
        <p className="glossary">
          <a href={section.glossaryUrl} target="_blank" rel="noopener noreferrer">
            {section.glossaryLabel}
          </a>
        </p>
      </header>
      <div className="grid">
        {section.items.map((term) => (
          <Link
            key={term.slug}
            className="tile list-enter"
            to={`/${locale}/compendium/${term.slug}`}
            viewTransition
          >
            <h2>{term.term}</h2>
            <p>{term.definition[0]}</p>
          </Link>
        ))}
      </div>
    </section>
  );
}
