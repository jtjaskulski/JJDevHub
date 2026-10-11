import { useLocation } from 'react-router';

import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/components/ui/empty';

import { localeFromPathname, siteFor } from '../../content/content';
import { listColumn } from '../../layout';
import { ListRow } from '../ListRow';
import { PageHeader } from '../PageHeader';

export function CompendiumPage() {
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).compendium;

  if (section.items.length === 0) {
    return (
      <section className={listColumn}>
        <PageHeader title={section.title} lead={section.lead} />
        <Empty>
          <EmptyHeader>
            <EmptyTitle>{section.title}</EmptyTitle>
            <EmptyDescription>{section.lead}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      </section>
    );
  }

  return (
    <section className={listColumn}>
      <PageHeader title={section.title} lead={section.lead} />
      <p>
        <a href={section.glossaryUrl} target="_blank" rel="noopener noreferrer" className="text-small">
          {section.glossaryLabel}
        </a>
      </p>
      <div className="flex flex-col">
        {section.items.map((term) => (
          <ListRow
            key={term.slug}
            to={`/${locale}/compendium/${term.slug}`}
            title={term.term}
            summary={term.definition[0]}
          />
        ))}
      </div>
    </section>
  );
}
