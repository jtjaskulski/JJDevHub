type ArticleBodyProps = {
  paragraphs: readonly string[];
};

type ParagraphRow = {
  id: string;
  text: string;
};

function paragraphRows(paragraphs: readonly string[]): ParagraphRow[] {
  const seen = new Map<string, number>();
  return paragraphs.map((text) => {
    const occurrence = seen.get(text) ?? 0;
    seen.set(text, occurrence + 1);
    const id = occurrence === 0 ? text : `${text}\u0000${occurrence}`;
    return { id, text };
  });
}

export function ArticleBody({ paragraphs }: ArticleBodyProps) {
  if (paragraphs.length === 0) {
    return null;
  }

  const rows = paragraphRows(paragraphs);

  return (
    <div className="flex flex-col gap-4">
      {rows.map((row) => (
        <p key={row.id} className="text-body">
          {row.text}
        </p>
      ))}
    </div>
  );
}
