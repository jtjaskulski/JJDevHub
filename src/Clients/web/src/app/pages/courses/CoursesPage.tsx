import { useLocation } from 'react-router';

import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/components/ui/empty';

import { localeFromPathname, siteFor } from '../../content/content';
import { listColumn } from '../../layout';
import { CourseCard } from '../CourseCard';
import { PageHeader } from '../PageHeader';

export function CoursesPage() {
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).courses;

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
      <div className="flex flex-col">
        {section.items.map((course) => (
          <CourseCard key={course.slug} locale={locale} course={course} />
        ))}
      </div>
    </section>
  );
}
