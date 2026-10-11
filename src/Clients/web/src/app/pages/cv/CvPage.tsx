import { useLocation } from 'react-router';

import { cvFor, localeFromPathname } from '../../content/content';
import { articleColumn } from '../../layout';
import { CvChapter } from './CvChapter';

export function CvPage() {
  const { pathname } = useLocation();
  const cv = cvFor(localeFromPathname(pathname));

  return (
    <article className={articleColumn}>
      <header className="flex flex-col gap-3">
        <h1 className="text-h2 font-semibold tracking-h2">{cv.name}</h1>
        <p className="text-body text-muted">{cv.role}</p>
        <a className="text-small" href={`mailto:${cv.email}`}>
          {cv.email}
        </a>
      </header>

      <section className="flex flex-col gap-5 border-l border-ink/25 pl-5">
        <h2 className="text-h3 font-medium tracking-h3">{cv.experienceTitle}</h2>
        {cv.experience.map((entry) => (
          <CvChapter key={`${entry.title}|${entry.period}`} entry={entry} org={entry.company} />
        ))}
      </section>

      <section className="flex flex-col gap-5 border-l border-ink/25 pl-5">
        <h2 className="text-h3 font-medium tracking-h3">{cv.educationTitle}</h2>
        {cv.education.map((entry) => (
          <CvChapter key={`${entry.title}|${entry.period}`} entry={entry} org={entry.institution} />
        ))}
      </section>

      <section className="flex flex-col gap-5 border-l border-ink/25 pl-5">
        <h2 className="text-h3 font-medium tracking-h3">{cv.extrasTitle}</h2>
        {cv.extras.map((entry) => (
          <CvChapter key={`${entry.title}|${entry.period}`} entry={entry} />
        ))}
      </section>
    </article>
  );
}
