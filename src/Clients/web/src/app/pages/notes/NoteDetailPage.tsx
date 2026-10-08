import { Link, Navigate, useLocation, useParams } from 'react-router';

import { localeFromPathname, noteBySlug, siteFor } from '../../content/content';
import './note-detail.scss';

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
    <article className="note-page article">
      <header className="article-head reveal">
        <p className="eyebrow">
          <Link to={`/${locale}/notes`} aria-label={section.backLabel} viewTransition>
            ←
          </Link>
        </p>
        <time dateTime={note.date}>{note.date}</time>
        <h1>{note.title}</h1>
        <p className="summary">{note.summary}</p>
      </header>
      <div className="body reveal">
        {note.body.map((paragraph) => (
          <p key={paragraph} className="list-enter">
            {paragraph}
          </p>
        ))}
      </div>
    </article>
  );
}
