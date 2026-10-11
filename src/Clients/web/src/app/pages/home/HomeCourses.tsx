import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from '@/components/ui/empty';

import type { CourseContent, HomeContent, LocaleCode } from '../../content/content.model';
import { RevealItem } from '../../motion/RevealItem';
import { SectionEnter } from '../../motion/SectionEnter';
import { CourseCard } from '../CourseCard';

type HomeCoursesProps = {
  locale: LocaleCode;
  home: HomeContent;
  courses: CourseContent[];
};

export function HomeCourses({ locale, home, courses }: HomeCoursesProps) {
  if (courses.length === 0) {
    return (
      <SectionEnter className="mx-auto flex w-full max-w-3xl flex-col gap-4 px-4 py-6 md:px-6">
        <h2 className="text-h3 font-medium tracking-h3">{home.coursesTitle}</h2>
        <Empty>
          <EmptyHeader>
            <EmptyTitle>{home.coursesTitle}</EmptyTitle>
            <EmptyDescription>{home.coursesLead}</EmptyDescription>
          </EmptyHeader>
        </Empty>
      </SectionEnter>
    );
  }

  return (
    <SectionEnter className="mx-auto flex w-full max-w-3xl flex-col gap-4 px-4 py-6 md:px-6">
      <RevealItem>
        <header className="flex max-w-xl flex-col gap-2">
          <h2 className="text-h3 font-medium tracking-h3">{home.coursesTitle}</h2>
          <p className="text-body text-muted">{home.coursesLead}</p>
        </header>
      </RevealItem>
      {courses.map((course) => (
        <RevealItem key={course.slug}>
          <CourseCard locale={locale} course={course} />
        </RevealItem>
      ))}
    </SectionEnter>
  );
}
