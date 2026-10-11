import { Link, Navigate, useLocation, useParams } from 'react-router';

import { courseBySlug, localeFromPathname, siteFor } from '../../content/content';
import { articleColumn } from '../../layout';
import { ArticleBody } from '../ArticleBody';
import { CourseChapters } from './CourseChapters';
import { CourseLessons } from './CourseLessons';

export function CourseDetailPage() {
  const { slug = '' } = useParams();
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).courses;
  const course = courseBySlug(slug, locale);

  if (!course) {
    return <Navigate to={`/${locale}/courses`} replace />;
  }

  const lessons = course.lessons ?? [];
  const chapters = course.chapters ?? [];
  const planned = course.planned ?? [];
  const labUrl = course.labUrl;
  const labLabel = course.labLabel;
  const showLab = labUrl !== undefined && labLabel !== undefined;

  return (
    <article className={articleColumn}>
      <header className="flex flex-col gap-3">
        <p>
          <Link to={`/${locale}/courses`} viewTransition className="text-small">
            {section.backLabel}
          </Link>
        </p>
        <h1 className="text-h2 font-semibold tracking-h2">{course.title}</h1>
        <p className="text-h4 text-muted">{course.subtitle}</p>
        <ArticleBody paragraphs={course.summary} />
        <p className="flex flex-wrap gap-4 text-small">
          <a href={course.repoUrl} target="_blank" rel="noopener noreferrer">
            {course.repoLabel}
          </a>
          {showLab ? (
            <a href={labUrl} target="_blank" rel="noopener noreferrer">
              {labLabel}
            </a>
          ) : null}
        </p>
      </header>
      <CourseLessons title={course.lessonsTitle} lessons={lessons} />
      <CourseChapters title={course.chaptersTitle} chapters={chapters} />
      <CourseChapters title={course.plannedTitle} lead={course.plannedLead} chapters={planned} />
    </article>
  );
}
