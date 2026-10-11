import { motion, type MotionValue } from 'motion/react';

import type { CvEntry } from '../../content/content.model';

type HeroChapterProps = {
  item: CvEntry;
  style?: {
    opacity: MotionValue<number>;
    y: MotionValue<number>;
  };
};

export function HeroChapter({ item, style }: HeroChapterProps) {
  const lead = item.paragraphs[0];
  const org = item.company;

  return (
    <motion.article style={style} className="flex max-w-xl flex-col gap-1">
      <p className="text-caption tracking-caption text-dark-chapter-fg/60">{item.period}</p>
      <h3 className="text-h4 tracking-h4">{item.title}</h3>
      {org ? <p className="text-small text-dark-chapter-fg/70">{org}</p> : null}
      {lead ? <p className="text-small text-dark-chapter-fg/80">{lead}</p> : null}
    </motion.article>
  );
}
