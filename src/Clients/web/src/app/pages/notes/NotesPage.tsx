import { Link, useLocation } from 'react-router';

import { localeFromPathname, siteFor } from '../../content/content';
import './notes.scss';

export function NotesPage() {
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).notes;

  return (
    <section className="notes-page page">
      <header className="page-head reveal">
        <h1>{section.title}</h1>
        <p>{section.lead}</p>
      </header>
      <div className="grid">
        {section.items.map((note) => (
          <Link key={note.slug} className="tile list-enter" to={`/${locale}/notes/${note.slug}`} viewTransition>
            <time dateTime={note.date}>{note.date}</time>
            <h2>{note.title}</h2>
            <p>{note.summary}</p>
          </Link>
        ))}
      </div>
    </section>
  );
}
