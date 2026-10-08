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
    <section className="block reveal">
      <h2>{title}</h2>
      <ul className="lesson-list">
        {lessons.map((lesson) => (
          <li key={lesson.slug} className="list-enter">
            <a href={lesson.url} target="_blank" rel="noopener noreferrer">
              <strong>{lesson.title}</strong>
              <span>{lesson.summary}</span>
            </a>
          </li>
        ))}
      </ul>
    </section>
  );
}
