import { tokens } from '@jjdevhub/theme';
import { motion, useReducedMotion } from 'motion/react';
import type { ReactNode } from 'react';

type SectionEnterProps = {
  children: ReactNode;
  className: string;
};

const staggerSeconds = tokens.duration.revealFast / 8000;

export function SectionEnter({ children, className }: SectionEnterProps) {
  const reduce = useReducedMotion() === true;

  if (reduce) {
    return <section className={className}>{children}</section>;
  }

  return (
    <motion.section
      className={className}
      initial={reduce ? false : 'hidden'}
      whileInView="visible"
      viewport={{ once: true, amount: 0.2 }}
      variants={{
        hidden: {},
        visible: {
          transition: reduce ? { duration: 0 } : { staggerChildren: staggerSeconds },
        },
      }}
    >
      {children}
    </motion.section>
  );
}
