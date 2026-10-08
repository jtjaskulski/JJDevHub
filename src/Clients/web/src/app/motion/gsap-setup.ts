import gsap from 'gsap';
import { ScrollTrigger } from 'gsap/ScrollTrigger';

let registered = false;

/** Register GSAP plugins once, after the DOM/window APIs exist. */
export function ensureGsapPlugins(): void {
  if (registered) {
    return;
  }
  gsap.registerPlugin(ScrollTrigger);
  registered = true;
}

export { gsap, ScrollTrigger };
