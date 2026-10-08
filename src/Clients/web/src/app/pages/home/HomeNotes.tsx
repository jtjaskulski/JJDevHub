import { Link } from 'react-router';

import type { HomeContent, LocaleCode, NoteContent } from '../../content/content.model';

type HomeNotesProps = {
  locale: LocaleCode;
  home: HomeContent;
  notes: NoteContent[];
};

export function HomeNotes({ locale, home, notes }: HomeNotesProps) {
  return (
    <section className="section reveal">
      <div className="section-head">
        <h2>{home.notesTitle}</h2>
        <p>{home.notesLead}</p>
      </div>
      <div className="notes-list">
        {notes.map((note) => (
          <Link key={note.slug} className="note-row list-enter" to={`/${locale}/notes/${note.slug}`} viewTransition>
            <time dateTime={note.date}>{note.date}</time>
            <span>
              <strong>{note.title}</strong>
              <span className="note-summary">{note.summary}</span>
            </span>
          </Link>
        ))}
      </div>
      <p className="section-more">
        <Link to={`/${locale}/notes`} viewTransition>
          {home.viewAll}
        </Link>
      </p>
    </section>
  );
}
