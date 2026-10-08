import { Link } from 'react-router';

import type { CourseContent, HomeContent, LocaleCode } from '../../content/content.model';

type HomeCoursesProps = {
  locale: LocaleCode;
  home: HomeContent;
  courses: CourseContent[];
};

export function HomeCourses({ locale, home, courses }: HomeCoursesProps) {
  return (
    <section className="section reveal">
      <div className="section-head">
        <h2>{home.coursesTitle}</h2>
        <p>{home.coursesLead}</p>
      </div>
      <div className="tile-grid">
        {courses.map((course) => (
          <Link
            key={course.slug}
            className="tile list-enter"
            to={`/${locale}/courses/${course.slug}`}
            viewTransition
          >
            <h3>{course.title}</h3>
            <p className="tile-sub">{course.subtitle}</p>
            <p>{course.summary[0]}</p>
          </Link>
        ))}
      </div>
    </section>
  );
}
