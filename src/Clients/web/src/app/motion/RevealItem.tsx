import { tokens } from '@jjdevhub/theme';
import { motion, useReducedMotion } from 'motion/react';
import type { ReactNode } from 'react';

type RevealItemProps = {
  children: ReactNode;
};

const ease = tokens.easing.reveal;
const enterY = tokens.space[5];
const enterSeconds = tokens.duration.reveal / 1000;

export function RevealItem({ children }: RevealItemProps) {
  const reduce = useReducedMotion() === true;

  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: enterY },
        visible: {
          opacity: 1,
          y: 0,
          transition: { duration: reduce ? 0 : enterSeconds, ease },
        },
      }}
    >
      {children}
    </motion.div>
  );
}
