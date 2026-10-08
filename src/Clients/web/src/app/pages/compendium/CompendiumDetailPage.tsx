import { Link, Navigate, useLocation, useParams } from 'react-router';

import { compendiumBySlug, localeFromPathname, siteFor } from '../../content/content';
import './compendium-detail.scss';

export function CompendiumDetailPage() {
  const { slug = '' } = useParams();
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).compendium;
  const term = compendiumBySlug(slug, locale);

  if (!term) {
    return <Navigate to={`/${locale}/compendium`} replace />;
  }

  return (
    <article className="compendium-detail article">
      <header className="article-head reveal">
        <p className="eyebrow">
          <Link to={`/${locale}/compendium`} aria-label={section.backLabel} viewTransition>
            ←
          </Link>
        </p>
        <h1>{term.term}</h1>
      </header>
      <div className="body reveal">
        {term.definition.map((paragraph) => (
          <p key={paragraph} className="list-enter">
            {paragraph}
          </p>
        ))}
      </div>
    </article>
  );
}
