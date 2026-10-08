import type { CvEntry } from '../../content/content.model';

type CvChapterProps = {
  entry: CvEntry;
  org?: string;
};

export function CvChapter({ entry, org }: CvChapterProps) {
  return (
    <section className="cv-chapter reveal list-enter">
      <p className="cv-period">{entry.period}</p>
      <h3>{entry.title}</h3>
      {org ? <p className="cv-org">{org}</p> : null}
      {entry.paragraphs.map((paragraph) => (
        <p key={paragraph}>{paragraph}</p>
      ))}
    </section>
  );
}
