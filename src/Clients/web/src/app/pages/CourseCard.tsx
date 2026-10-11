import type { CourseContent, LocaleCode } from '../content/content.model';
import { ListRow } from './ListRow';

type CourseCardProps = {
  locale: LocaleCode;
  course: CourseContent;
};

export function CourseCard({ locale, course }: CourseCardProps) {
  const summary = course.summary[0];

  return (
    <ListRow
      to={`/${locale}/courses/${course.slug}`}
      title={course.title}
      meta={course.subtitle}
      summary={summary}
    />
  );
}
