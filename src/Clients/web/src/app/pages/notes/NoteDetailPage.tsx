import { Link, Navigate, useLocation, useParams } from 'react-router';

import { localeFromPathname, noteBySlug, siteFor } from '../../content/content';
import { articleColumn } from '../../layout';
import { ArticleBody } from '../ArticleBody';

export function NoteDetailPage() {
  const { slug = '' } = useParams();
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).notes;
  const note = noteBySlug(slug, locale);

  if (!note) {
    return <Navigate to={`/${locale}/notes`} replace />;
  }

  return (
    <article className={articleColumn}>
      <header className="flex flex-col gap-3">
        <p>
          <Link to={`/${locale}/notes`} viewTransition className="text-small">
            {section.backLabel}
          </Link>
        </p>
        <time dateTime={note.date} className="text-caption tracking-caption text-muted">
          {note.date}
        </time>
        <h1 className="text-h2 font-semibold tracking-h2">{note.title}</h1>
        <p className="text-body text-muted">{note.summary}</p>
      </header>
      <ArticleBody paragraphs={note.body} />
    </article>
  );
}
