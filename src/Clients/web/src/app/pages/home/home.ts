import {
  afterNextRender,
  Component,
  computed,
  ElementRef,
  inject,
  OnDestroy,
  viewChild,
  viewChildren,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { tokens } from '@jjdevhub/theme';

import { ContentService } from '../../content/content.service';
import { ensureGsapPlugins, gsap, ScrollTrigger } from '../../motion/gsap-setup';
import { LenisService } from '../../motion/lenis.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnDestroy {
  private readonly content = inject(ContentService);
  private readonly lenis = inject(LenisService);

  private readonly heroPin = viewChild<ElementRef<HTMLElement>>('heroPin');
  private readonly heroName = viewChild<ElementRef<HTMLElement>>('heroName');
  private readonly heroRole = viewChild<ElementRef<HTMLElement>>('heroRole');
  private readonly chapters = viewChildren<ElementRef<HTMLElement>>('chapter');

  private timeline: gsap.core.Timeline | null = null;

  protected readonly locale = this.content.locale;
  protected readonly site = this.content.site;
  protected readonly cv = this.content.cv;

  protected readonly currentChapters = computed(() => this.cv().experience.slice(0, 2));
  protected readonly courses = computed(() => this.site().courses.items);
  protected readonly latestNotes = computed(() => this.site().notes.items.slice(0, 3));

  constructor() {
    afterNextRender(() => this.setupHero());
  }

  ngOnDestroy(): void {
    this.killHero();
  }

  private setupHero(): void {
    const pin = this.heroPin()?.nativeElement;
    const name = this.heroName()?.nativeElement;
    const role = this.heroRole()?.nativeElement;
    const chapterEls = this.chapters().map((ref) => ref.nativeElement);

    if (!pin || !name || !role) {
      return;
    }

    ensureGsapPlugins();

    if (this.lenis.prefersReducedMotion()) {
      gsap.set(name, { scale: 0.55, transformOrigin: 'center top' });
      gsap.set(role, { opacity: 1, y: 0 });
      gsap.set(chapterEls, { opacity: 1, y: 0 });
      return;
    }

    gsap.set(role, { opacity: 0, y: 24 });
    gsap.set(chapterEls, { opacity: 0, y: 36 });

    const fade = tokens.duration.heroFade / 1000;
    const pinMs = tokens.duration.heroPin;

    this.timeline = gsap.timeline({
      scrollTrigger: {
        trigger: pin,
        start: 'top top',
        end: `+=${pinMs * 2.5}`,
        pin: true,
        scrub: true,
        anticipatePin: 1,
      },
    });

    this.timeline
      .to(name, {
        scale: 0.55,
        transformOrigin: 'center top',
        ease: 'none',
        duration: fade,
      })
      .to(
        role,
        {
          opacity: 1,
          y: 0,
          ease: 'none',
          duration: fade,
        },
        '-=0.35',
      );

    chapterEls.forEach((el, index) => {
      this.timeline?.to(
        el,
        {
          opacity: 1,
          y: 0,
          ease: 'none',
          duration: fade,
        },
        index === 0 ? '-=0.15' : '-=0.2',
      );
    });
  }

  private killHero(): void {
    this.timeline?.scrollTrigger?.kill();
    this.timeline?.kill();
    this.timeline = null;
    const pinEl = this.heroPin()?.nativeElement;
    if (pinEl) {
      ScrollTrigger.getAll()
        .filter((st) => st.trigger === pinEl)
        .forEach((st) => st.kill());
    }
  }
}
