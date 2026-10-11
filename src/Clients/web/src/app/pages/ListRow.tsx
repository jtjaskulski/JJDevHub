import { Link } from 'react-router';

type ListRowProps = {
  to: string;
  title: string;
  meta?: string;
  summary?: string;
};

export function ListRow({ to, title, meta, summary }: ListRowProps) {
  return (
    <Link
      to={to}
      viewTransition
      className="flex flex-col gap-1 border-b border-ink/15 py-4 text-ink no-underline"
    >
      {meta ? <span className="text-caption tracking-caption text-muted">{meta}</span> : null}
      <span className="text-h4 tracking-h4">{title}</span>
      {summary ? <span className="text-small text-muted">{summary}</span> : null}
    </Link>
  );
}
