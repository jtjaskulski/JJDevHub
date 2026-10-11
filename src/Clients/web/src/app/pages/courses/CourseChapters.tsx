import type { ChapterContent } from '../../content/content.model';

type CourseChaptersProps = {
  title?: string;
  lead?: string;
  chapters: ChapterContent[];
};

export function CourseChapters({ title, lead, chapters }: CourseChaptersProps) {
  if (chapters.length === 0) {
    return null;
  }

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-h3 font-medium tracking-h3">{title}</h2>
      {lead ? <p className="text-body text-muted">{lead}</p> : null}
      {chapters.map((chapter) => (
        <div key={chapter.slug} className="border-b border-ink/15 py-4">
          <h3 className="text-h4 tracking-h4">{chapter.title}</h3>
          <p className="mt-2 text-small text-muted">{chapter.summary}</p>
        </div>
      ))}
    </section>
  );
}
