import { Link } from 'react-router';

import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/components/ui/empty';

import type { HomeContent, LocaleCode, NoteContent } from '../../content/content.model';
import { RevealItem } from '../../motion/RevealItem';
import { SectionEnter } from '../../motion/SectionEnter';
import { ListRow } from '../ListRow';

type HomeNotesProps = {
  locale: LocaleCode;
  home: HomeContent;
  notes: NoteContent[];
};

export function HomeNotes({ locale, home, notes }: HomeNotesProps) {
  if (notes.length === 0) {
    return (
      <SectionEnter className="mx-auto flex w-full max-w-3xl flex-col gap-4 px-4 py-6 md:px-6">
        <h2 className="text-h3 font-medium tracking-h3">{home.notesTitle}</h2>
        <Empty>
          <EmptyHeader>
            <EmptyTitle>{home.notesTitle}</EmptyTitle>
            <EmptyDescription>{home.notesLead}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      </SectionEnter>
    );
  }

  return (
    <SectionEnter className="mx-auto flex w-full max-w-3xl flex-col gap-4 px-4 pb-10 md:px-6">
      <RevealItem>
        <header className="flex max-w-xl flex-col gap-2">
          <h2 className="text-h3 font-medium tracking-h3">{home.notesTitle}</h2>
          <p className="text-body text-muted">{home.notesLead}</p>
        </header>
      </RevealItem>
      {notes.map((note) => (
        <RevealItem key={note.slug}>
          <ListRow
            to={`/${locale}/notes/${note.slug}`}
            title={note.title}
            meta={note.date}
            summary={note.summary}
          />
        </RevealItem>
      ))}
      <RevealItem>
        <p>
          <Link to={`/${locale}/notes`} viewTransition className="text-small">
            {home.viewAll}
          </Link>
        </p>
      </RevealItem>
    </SectionEnter>
  );
}
