type PageHeaderProps = {
  title: string;
  lead?: string;
};

export function PageHeader({ title, lead }: PageHeaderProps) {
  return (
    <header className="flex max-w-xl flex-col gap-3">
      <h1 className="text-h2 font-semibold tracking-h2">{title}</h1>
      {lead ? <p className="text-body text-muted">{lead}</p> : null}
    </header>
  );
}
