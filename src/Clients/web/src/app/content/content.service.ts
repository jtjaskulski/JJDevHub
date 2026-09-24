import { computed, inject, Injectable, signal } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import type {
  CompendiumTerm,
  CourseContent,
  CvFile,
  CvLocaleContent,
  LocaleCode,
  LocaleContent,
  NoteContent,
  SiteContent,
} from './content.model';

import en from '../../content/en.json';
import pl from '../../content/pl.json';
import cvFile from '../../content/cv.json';

const sites: Record<LocaleCode, SiteContent> = {
  pl: pl as SiteContent,
  en: en as SiteContent,
};

const cvByLocale = cvFile as CvFile;

const LOCALES = new Set<LocaleCode>(['pl', 'en']);

function isLocale(value: string | undefined | null): value is LocaleCode {
  return value === 'pl' || value === 'en';
}

@Injectable({ providedIn: 'root' })
export class ContentService {
  private readonly router = inject(Router);

  private readonly localeSignal = signal<LocaleCode>(this.readLocaleFromUrl());

  /** Active UI language from the `/:lang` URL prefix (defaults to `pl`). */
  readonly locale = this.localeSignal.asReadonly();

  /** Site chrome + teaching content for the active locale. */
  readonly site = computed(() => sites[this.localeSignal()]);

  /** Personal CV for the active locale. */
  readonly cv = computed(() => cvByLocale[this.localeSignal()]);

  /** Site and CV bundled for the active locale. */
  readonly content = computed<LocaleContent>(() => ({
    locale: this.localeSignal(),
    site: this.site(),
    cv: this.cv(),
  }));

  constructor() {
    this.router.events.pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd)).subscribe(() => {
      this.localeSignal.set(this.readLocaleFromUrl());
    });
  }

  forLocale(locale: LocaleCode): LocaleContent {
    return {
      locale,
      site: sites[locale],
      cv: cvByLocale[locale],
    };
  }

  siteFor(locale: LocaleCode): SiteContent {
    return sites[locale];
  }

  cvFor(locale: LocaleCode): CvLocaleContent {
    return cvByLocale[locale];
  }

  courseBySlug(slug: string, locale: LocaleCode = this.localeSignal()): CourseContent | undefined {
    return sites[locale].courses.items.find((c) => c.slug === slug);
  }

  compendiumBySlug(slug: string, locale: LocaleCode = this.localeSignal()): CompendiumTerm | undefined {
    return sites[locale].compendium.items.find((t) => t.slug === slug);
  }

  noteBySlug(slug: string, locale: LocaleCode = this.localeSignal()): NoteContent | undefined {
    return sites[locale].notes.items.find((n) => n.slug === slug);
  }

  private readLocaleFromUrl(): LocaleCode {
    const segment = this.router.url.split(/[/?#]/).find(Boolean);
    if (isLocale(segment)) {
      return segment;
    }
    return 'pl';
  }

  static isLocaleCode(value: string | undefined | null): value is LocaleCode {
    return isLocale(value);
  }

  static readonly supportedLocales: readonly LocaleCode[] = [...LOCALES];
}
