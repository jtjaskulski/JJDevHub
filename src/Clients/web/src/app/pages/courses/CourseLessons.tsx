import type { LessonContent } from '../../content/content.model';

type CourseLessonsProps = {
  title?: string;
  lessons: LessonContent[];
};

export function CourseLessons({ title, lessons }: CourseLessonsProps) {
  if (lessons.length === 0) {
    return null;
  }

  return (
    <section className="flex flex-col gap-3">
      <h2 className="text-h3 font-medium tracking-h3">{title}</h2>
      <ul className="flex flex-col">
        {lessons.map((lesson) => (
          <li key={lesson.slug} className="border-b border-ink/15 py-4">
            <a href={lesson.url} target="_blank" rel="noopener noreferrer" className="flex flex-col gap-1 no-underline">
              <strong className="text-h4 font-medium tracking-h4 text-ink">{lesson.title}</strong>
              <span className="text-small text-muted">{lesson.summary}</span>
            </a>
          </li>
        ))}
      </ul>
    </section>
  );
}
