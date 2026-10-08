import { Link, useLocation } from 'react-router';

import { localeFromPathname, siteFor } from '../../content/content';
import './courses.scss';

export function CoursesPage() {
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const section = siteFor(locale).courses;

  return (
    <section className="courses-page page">
      <header className="page-head reveal">
        <h1>{section.title}</h1>
        <p>{section.lead}</p>
      </header>
      <div className="tile-grid">
        {section.items.map((course) => (
          <Link
            key={course.slug}
            className="tile list-enter"
            to={`/${locale}/courses/${course.slug}`}
            viewTransition
          >
            <h2>{course.title}</h2>
            <p className="tile-sub">{course.subtitle}</p>
            {course.summary.map((paragraph) => (
              <p key={paragraph}>{paragraph}</p>
            ))}
          </Link>
        ))}
      </div>
    </section>
  );
}
