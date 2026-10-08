import type { CvEntry } from '../../content/content.model';

type HeroChapterProps = {
  item: CvEntry;
  onMount: (element: HTMLElement | null) => void;
};

export function HeroChapter({ item, onMount }: HeroChapterProps) {
  const company = item.company;
  return (
    <article className="hero-chapter" ref={onMount}>
      <p className="hero-chapter-period">{item.period}</p>
      <h3>{item.title}</h3>
      {company ? <p className="hero-chapter-company">{company}</p> : null}
      {item.paragraphs.map((paragraph) => (
        <p key={paragraph}>{paragraph}</p>
      ))}
    </article>
  );
}
