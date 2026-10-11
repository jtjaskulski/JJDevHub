import { useLocation } from 'react-router';

import { cvFor, localeFromPathname, siteFor } from '../../content/content';
import { HomeCourses } from './HomeCourses';
import { HomeHero } from './HomeHero';
import { HomeNotes } from './HomeNotes';

export function HomePage() {
  const { pathname } = useLocation();
  const locale = localeFromPathname(pathname);
  const site = siteFor(locale);
  const cv = cvFor(locale);
  const courses = site.courses.items.slice(0, 2);
  const notes = site.notes.items.slice(0, 3);

  return (
    <div>
      <HomeHero locale={locale} home={site.home} cv={cv} />
      <HomeCourses locale={locale} home={site.home} courses={courses} />
      <HomeNotes locale={locale} home={site.home} notes={notes} />
    </div>
  );
}
