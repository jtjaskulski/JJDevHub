import { tokens } from '@jjdevhub/theme';
import { AnimatePresence, motion, useReducedMotion } from 'motion/react';
import { Navigate, Outlet, useLocation, useParams } from 'react-router';

import { isLocale } from './content/content';

export function LangGate() {
  const { lang } = useParams();
  const location = useLocation();
  const reduce = useReducedMotion() === true;

  if (!isLocale(lang)) {
    return <Navigate to="/pl" replace />;
  }

  if (reduce) {
    return <Outlet />;
  }

  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={location.pathname}
        initial={{ opacity: 0, y: tokens.space[3] }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0 }}
        transition={{
          duration: tokens.duration.revealFast / 1000,
          ease: tokens.easing.reveal,
        }}
      >
        <Outlet />
      </motion.div>
    </AnimatePresence>
  );
}
