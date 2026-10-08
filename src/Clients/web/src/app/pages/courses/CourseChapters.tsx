import type { ChapterContent } from '../../content/content.model';

type CourseChaptersProps = {
  title?: string;
  lead?: string;
  chapters: ChapterContent[];
  planned?: boolean;
};

export function CourseChapters({ title, lead, chapters, planned = false }: CourseChaptersProps) {
  if (chapters.length === 0) {
    return null;
  }

  const className = planned ? 'block planned reveal' : 'block reveal';

  return (
    <section className={className}>
      <h2>{title}</h2>
      {lead ? <p className="planned-lead">{lead}</p> : null}
      {chapters.map((chapter) => (
        <div key={chapter.slug} className="chapter list-enter">
          <h3>{chapter.title}</h3>
          <p>{chapter.summary}</p>
        </div>
      ))}
    </section>
  );
}
