import Lenis from 'lenis';

import { ensureGsapPlugins, gsap, ScrollTrigger } from './gsap-setup';

let lenis: Lenis | null = null;
let tickerFn: ((time: number) => void) | null = null;

export function prefersReducedMotion(): boolean {
  return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/** One app-wide Lenis instance. No-op when the user prefers reduced motion. */
export function startLenis(): void {
  if (lenis || prefersReducedMotion() || typeof ResizeObserver === 'undefined') {
    return;
  }

  ensureGsapPlugins();

  const instance = new Lenis({
    autoRaf: false,
    smoothWheel: true,
  });

  instance.on('scroll', ScrollTrigger.update);

  const tick = (time: number) => {
    instance.raf(time * 1000);
  };
  gsap.ticker.add(tick);
  gsap.ticker.lagSmoothing(0);

  lenis = instance;
  tickerFn = tick;
}

export function stopLenis(): void {
  if (tickerFn) {
    gsap.ticker.remove(tickerFn);
    tickerFn = null;
  }
  lenis?.destroy();
  lenis = null;
}
