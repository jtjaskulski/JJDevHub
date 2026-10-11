import { Link, Navigate, useLocation, useParams } from 'react-router';

import { compendiumBySlug, localeFromPathname, siteFor } from '../../content/content';
import { articleColumn } from '../../layout';
import { ArticleBody } from '../ArticleBody';

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
    <article className={articleColumn}>
      <header className="flex flex-col gap-3">
        <p>
          <Link to={`/${locale}/compendium`} viewTransition className="text-small">
            {section.backLabel}
          </Link>
        </p>
        <h1 className="text-h2 font-semibold tracking-h2">{term.term}</h1>
      </header>
      <ArticleBody paragraphs={term.definition} />
    </article>
  );
}
