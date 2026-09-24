import { DestroyRef, inject, Injectable } from '@angular/core';
import Lenis from 'lenis';

import { ensureGsapPlugins, gsap, ScrollTrigger } from './gsap-setup';

@Injectable({ providedIn: 'root' })
export class LenisService {
  private readonly destroyRef = inject(DestroyRef);
  private lenis: Lenis | null = null;
  private tickerFn: ((time: number) => void) | null = null;

  /** One app-wide Lenis instance; no-op when the user prefers reduced motion. */
  start(): void {
    if (this.lenis || this.prefersReducedMotion() || typeof ResizeObserver === 'undefined') {
      return;
    }

    ensureGsapPlugins();

    const lenis = new Lenis({
      autoRaf: false,
      smoothWheel: true,
    });

    lenis.on('scroll', ScrollTrigger.update);

    const tickerFn = (time: number) => {
      lenis.raf(time * 1000);
    };
    gsap.ticker.add(tickerFn);
    gsap.ticker.lagSmoothing(0);

    this.lenis = lenis;
    this.tickerFn = tickerFn;

    this.destroyRef.onDestroy(() => this.stop());
  }

  stop(): void {
    if (this.tickerFn) {
      gsap.ticker.remove(this.tickerFn);
      this.tickerFn = null;
    }
    this.lenis?.destroy();
    this.lenis = null;
  }

  prefersReducedMotion(): boolean {
    return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}
