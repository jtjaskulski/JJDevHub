import { tokens } from '@jjdevhub/theme';
import { useLayoutEffect, useRef } from 'react';
import { Link } from 'react-router';

import type { CvLocaleContent, HomeContent, LocaleCode } from '../../content/content.model';
import { ensureGsapPlugins, gsap, ScrollTrigger } from '../../motion/gsap-setup';
import { prefersReducedMotion } from '../../motion/lenis';
import { HeroChapter } from './HeroChapter';

type HomeHeroProps = {
  locale: LocaleCode;
  home: HomeContent;
  cv: CvLocaleContent;
};

export function HomeHero({ locale, home, cv }: HomeHeroProps) {
  const chapters = cv.experience.slice(0, 2);
  const pinRef = useRef<HTMLDivElement>(null);
  const nameRef = useRef<HTMLHeadingElement>(null);
  const roleRef = useRef<HTMLParagraphElement>(null);
  const chapterRefs = useRef<Array<HTMLElement | null>>([]);

  useLayoutEffect(() => {
    const pin = pinRef.current;
    const name = nameRef.current;
    const role = roleRef.current;
    const chapterEls = chapterRefs.current.filter((el): el is HTMLElement => el !== null);
    if (!pin || !name || !role) {
      return;
    }

    ensureGsapPlugins();

    if (prefersReducedMotion()) {
      gsap.set(name, { scale: 0.55, transformOrigin: 'center top' });
      gsap.set(role, { opacity: 1, y: 0 });
      gsap.set(chapterEls, { opacity: 1, y: 0 });
      return;
    }

    gsap.set(role, { opacity: 0, y: 24 });
    gsap.set(chapterEls, { opacity: 0, y: 36 });

    const fade = tokens.duration.heroFade / 1000;
    const pinMs = tokens.duration.heroPin;
    const timeline = gsap.timeline({
      scrollTrigger: {
        trigger: pin,
        start: 'top top',
        end: `+=${pinMs * 2.5}`,
        pin: true,
        scrub: true,
        anticipatePin: 1,
      },
    });

    timeline
      .to(name, { scale: 0.55, transformOrigin: 'center top', ease: 'none', duration: fade })
      .to(role, { opacity: 1, y: 0, ease: 'none', duration: fade }, '-=0.35');

    chapterEls.forEach((el, index) => {
      const position = index === 0 ? '-=0.15' : '-=0.2';
      timeline.to(el, { opacity: 1, y: 0, ease: 'none', duration: fade }, position);
    });

    return () => {
      timeline.scrollTrigger?.kill();
      timeline.kill();
      ScrollTrigger.getAll()
        .filter((trigger) => trigger.trigger === pin)
        .forEach((trigger) => trigger.kill());
    };
  }, [cv.name, chapters.length]);

  return (
    <section className="hero" aria-label={cv.name}>
      <div className="hero-pin" ref={pinRef}>
        <p className="hero-eyebrow">{home.eyebrow}</p>
        <h1 className="hero-name" ref={nameRef}>
          {cv.name}
        </h1>
        <p className="hero-role" ref={roleRef}>
          {cv.role}
        </p>
        <div className="hero-current">
          <h2 className="hero-current-title">{home.currentTitle}</h2>
          <p className="hero-current-lead">{home.currentLead}</p>
          <div className="hero-chapters">
            {chapters.map((item, index) => (
              <HeroChapter
                key={`${item.title}|${item.period}`}
                item={item}
                onMount={(el) => {
                  chapterRefs.current[index] = el;
                }}
              />
            ))}
          </div>
        </div>
        <p className="hero-cta">
          <Link to={`/${locale}/cv`} viewTransition>
            {home.ctaCv}
          </Link>
          <Link to={`/${locale}/courses`} viewTransition>
            {home.ctaCourses}
          </Link>
        </p>
      </div>
    </section>
  );
}
