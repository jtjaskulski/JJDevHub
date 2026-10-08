import { useLocation } from 'react-router';

import { cvFor, localeFromPathname } from '../../content/content';
import { CvChapter } from './CvChapter';
import './cv.scss';

export function CvPage() {
  const { pathname } = useLocation();
  const cv = cvFor(localeFromPathname(pathname));

  return (
    <article className="cv-page cv">
      <header className="cv-hero reveal">
        <h1>{cv.name}</h1>
        <p className="cv-role">{cv.role}</p>
        <a className="cv-email" href={`mailto:${cv.email}`}>
          {cv.email}
        </a>
      </header>

      <section className="cv-section">
        <h2 className="reveal">{cv.experienceTitle}</h2>
        {cv.experience.map((entry) => (
          <CvChapter key={`${entry.title}|${entry.period}`} entry={entry} org={entry.company} />
        ))}
      </section>

      <section className="cv-section">
        <h2 className="reveal">{cv.educationTitle}</h2>
        {cv.education.map((entry) => (
          <CvChapter key={`${entry.title}|${entry.period}`} entry={entry} org={entry.institution} />
        ))}
      </section>

      <section className="cv-section">
        <h2 className="reveal">{cv.extrasTitle}</h2>
        {cv.extras.map((entry) => (
          <CvChapter key={`${entry.title}|${entry.period}`} entry={entry} />
        ))}
      </section>
    </article>
  );
}
