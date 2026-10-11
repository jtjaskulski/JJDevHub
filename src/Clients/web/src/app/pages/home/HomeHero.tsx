import { tokens } from '@jjdevhub/theme';
import { motion, useReducedMotion, useScroll, useTransform } from 'motion/react';
import { useRef } from 'react';
import { Link } from 'react-router';

import { Button } from '@/components/ui/button';

import type { CvLocaleContent, HomeContent, LocaleCode } from '../../content/content.model';
import { HeroChapter } from './HeroChapter';
import { StackLine } from './StackLine';

type HomeHeroProps = {
  locale: LocaleCode;
  home: HomeContent;
  cv: CvLocaleContent;
};

export function HomeHero({ locale, home, cv }: HomeHeroProps) {
  const chapters = cv.experience.slice(0, 2);
  const firstChapter = chapters[0];
  const secondChapter = chapters[1];
  const sceneRef = useRef<HTMLElement>(null);
  const reduceMotion = useReducedMotion();
  const { scrollYProgress } = useScroll({
    target: sceneRef,
    offset: ['start start', 'end end'],
  });
  const { hero, space } = tokens;
  const nameScale = useTransform(scrollYProgress, [...hero.progress.name], [1, hero.nameScale]);
  const roleOpacity = useTransform(scrollYProgress, [...hero.progress.role], [0, 1]);
  const roleY = useTransform(scrollYProgress, [...hero.progress.role], [space[5], 0]);
  const firstOpacity = useTransform(scrollYProgress, [...hero.progress.first], [0, 1]);
  const firstY = useTransform(scrollYProgress, [...hero.progress.first], [space[6], 0]);
  const secondOpacity = useTransform(scrollYProgress, [...hero.progress.second], [0, 1]);
  const secondY = useTransform(scrollYProgress, [...hero.progress.second], [space[6], 0]);

  const scene = reduceMotion === false;
  const runway = `calc(100dvh + ${tokens.duration.heroPin * hero.runway}px)`;
  const frameClass = scene
    ? 'sticky top-0 flex min-h-dvh flex-col justify-center'
    : 'flex min-h-dvh flex-col justify-center';

  return (
    <section
      ref={sceneRef}
      aria-label={cv.name}
      className="bg-dark-chapter text-dark-chapter-fg"
      style={scene ? { minHeight: runway } : { minHeight: '100dvh' }}
    >
      <div className={frameClass}>
        <div className="mx-auto flex w-full max-w-3xl flex-col gap-4 px-4 py-8 md:px-6">
          <p className="text-caption tracking-caption">{home.eyebrow}</p>
          <motion.h1
            className="text-display font-semibold tracking-display"
            style={
              scene
                ? { scale: nameScale, transformOrigin: 'center top' }
                : { scale: hero.nameScale, transformOrigin: 'center top' }
            }
          >
            {cv.name}
          </motion.h1>
          <motion.div style={scene ? { opacity: roleOpacity, y: roleY } : undefined} className="flex flex-col gap-4">
            <p className="max-w-xl text-body">{cv.role}</p>
            <StackLine locale={locale} stack={home.stack} />
          </motion.div>
          <div className="flex flex-col gap-2">
            <h2 className="text-h3 font-medium tracking-h3">{home.currentTitle}</h2>
            <p className="max-w-xl text-body text-dark-chapter-fg/80">{home.currentLead}</p>
          </div>
          <div className="flex flex-col gap-4">
            {firstChapter ? (
              <HeroChapter
                item={firstChapter}
                style={scene ? { opacity: firstOpacity, y: firstY } : undefined}
              />
            ) : null}
            {secondChapter ? (
              <HeroChapter
                item={secondChapter}
                style={scene ? { opacity: secondOpacity, y: secondY } : undefined}
              />
            ) : null}
          </div>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link to={`/${locale}/cv`} viewTransition>
                {home.ctaCv}
              </Link>
            </Button>
            <Button
              asChild
              variant="outline"
              className="border-dark-chapter-fg/40 bg-transparent text-dark-chapter-fg hover:bg-dark-chapter-fg/10"
            >
              <Link to={`/${locale}/courses`} viewTransition>
                {home.ctaCourses}
              </Link>
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
