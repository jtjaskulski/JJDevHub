import { Link, Navigate, useLocation, useParams } from 'react-router';

import { courseBySlug, localeFromPathname, siteFor } from '../../content/content';
import { CourseChapters } from './CourseChapters';
import { CourseLessons } from './CourseLessons';
import './course-detail.scss';

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
  const lab =
    course.labUrl && course.labLabel ? { url: course.labUrl, label: course.labLabel } : null;

  return (
    <article className="course-page article">
      <header className="article-head reveal">
        <p className="eyebrow">
          <Link to={`/${locale}/courses`} aria-label={section.backLabel} viewTransition>
            ←
          </Link>
        </p>
        <h1>{course.title}</h1>
        <p className="lead">{course.subtitle}</p>
        {course.summary.map((paragraph) => (
          <p key={paragraph}>{paragraph}</p>
        ))}
        <p className="links">
          <a href={course.repoUrl} target="_blank" rel="noopener noreferrer">
            {course.repoLabel}
          </a>
          {lab ? (
            <a href={lab.url} target="_blank" rel="noopener noreferrer">
              {lab.label}
            </a>
          ) : null}
        </p>
      </header>
      <CourseLessons title={course.lessonsTitle} lessons={lessons} />
      <CourseChapters title={course.chaptersTitle} chapters={chapters} />
      <CourseChapters title={course.plannedTitle} lead={course.plannedLead} chapters={planned} planned />
    </article>
  );
}
