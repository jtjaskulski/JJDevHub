import type { CvEntry } from '../../content/content.model';
import { ArticleBody } from '../ArticleBody';

type CvChapterProps = {
  entry: CvEntry;
  org?: string;
};

export function CvChapter({ entry, org }: CvChapterProps) {
  return (
    <section className="flex flex-col gap-2">
      <p className="text-caption tracking-caption text-muted">{entry.period}</p>
      <h3 className="text-h4 tracking-h4">{entry.title}</h3>
      {org ? <p className="text-small text-muted">{org}</p> : null}
      <ArticleBody paragraphs={entry.paragraphs} />
    </section>
  );
}
