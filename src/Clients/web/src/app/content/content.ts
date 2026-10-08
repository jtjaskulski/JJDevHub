import type {
  ChapterContent,
  CompendiumSection,
  CompendiumTerm,
  CourseContent,
  CoursesSection,
  CvEntry,
  CvFile,
  CvLocaleContent,
  HomeContent,
  LessonContent,
  LocaleCode,
  NavContent,
  NoteContent,
  NotesSection,
  SiteContent,
} from './content.model';

import en from '../../content/en.json';
import pl from '../../content/pl.json';
import cvFile from '../../content/cv.json';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isString(value: unknown): value is string {
  return typeof value === 'string';
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every(isString);
}

function hasStrings(record: Record<string, unknown>, keys: readonly string[]): boolean {
  return keys.every((key) => isString(record[key]));
}

function optionalString(record: Record<string, unknown>, key: string): boolean {
  return record[key] === undefined || isString(record[key]);
}

function isLesson(value: unknown): value is LessonContent {
  return isRecord(value) && hasStrings(value, ['slug', 'title', 'summary', 'url']);
}

function isChapter(value: unknown): value is ChapterContent {
  return isRecord(value) && hasStrings(value, ['slug', 'title', 'summary']);
}

function isCourse(value: unknown): value is CourseContent {
  if (!isRecord(value) || !hasStrings(value, ['slug', 'title', 'subtitle', 'repoUrl', 'repoLabel'])) {
    return false;
  }
  if (!isStringArray(value.summary)) {
    return false;
  }
  if (!optionalString(value, 'labUrl') || !optionalString(value, 'labLabel')) {
    return false;
  }
  if (!optionalString(value, 'lessonsTitle') || !optionalString(value, 'chaptersTitle')) {
    return false;
  }
  if (!optionalString(value, 'plannedTitle') || !optionalString(value, 'plannedLead')) {
    return false;
  }
  return (
    optionalList(value.lessons, isLesson) &&
    optionalList(value.chapters, isChapter) &&
    optionalList(value.planned, isChapter)
  );
}

function optionalList<T>(value: unknown, guard: (item: unknown) => item is T): boolean {
  return value === undefined || (Array.isArray(value) && value.every(guard));
}

function isNav(value: unknown): value is NavContent {
  return (
    isRecord(value) &&
    hasStrings(value, ['home', 'cv', 'courses', 'compendium', 'notes', 'language', 'languagePl', 'languageEn'])
  );
}

function isHome(value: unknown): value is HomeContent {
  return (
    isRecord(value) &&
    hasStrings(value, [
      'eyebrow',
      'ctaCv',
      'ctaCourses',
      'currentTitle',
      'currentLead',
      'coursesTitle',
      'coursesLead',
      'notesTitle',
      'notesLead',
      'viewAll',
    ])
  );
}

function isCourses(value: unknown): value is CoursesSection {
  return (
    isRecord(value) &&
    hasStrings(value, ['title', 'lead', 'backLabel']) &&
    Array.isArray(value.items) &&
    value.items.every(isCourse)
  );
}

function isTerm(value: unknown): value is CompendiumTerm {
  return isRecord(value) && hasStrings(value, ['slug', 'term']) && isStringArray(value.definition);
}

function isCompendium(value: unknown): value is CompendiumSection {
  return (
    isRecord(value) &&
    hasStrings(value, ['title', 'lead', 'backLabel', 'glossaryUrl', 'glossaryLabel']) &&
    Array.isArray(value.items) &&
    value.items.every(isTerm)
  );
}

function isNote(value: unknown): value is NoteContent {
  if (!isRecord(value) || !hasStrings(value, ['slug', 'title', 'date', 'summary']) || !isStringArray(value.body)) {
    return false;
  }
  return optionalString(value, 'relatedLessonSlug') && optionalString(value, 'relatedCourseSlug');
}

function isNotes(value: unknown): value is NotesSection {
  return (
    isRecord(value) &&
    hasStrings(value, ['title', 'lead', 'backLabel']) &&
    Array.isArray(value.items) &&
    value.items.every(isNote)
  );
}

function isSiteContent(value: unknown): value is SiteContent {
  return (
    isRecord(value) && isNav(value.nav) && isHome(value.home) && isCourses(value.courses) && isCompendium(value.compendium) && isNotes(value.notes)
  );
}

function isCvEntry(value: unknown): value is CvEntry {
  if (!isRecord(value) || !hasStrings(value, ['title', 'period']) || !isStringArray(value.paragraphs)) {
    return false;
  }
  return optionalString(value, 'company') && optionalString(value, 'institution');
}

function isCvLocale(value: unknown): value is CvLocaleContent {
  return (
    isRecord(value) &&
    hasStrings(value, ['name', 'role', 'email', 'experienceTitle', 'educationTitle', 'extrasTitle']) &&
    Array.isArray(value.experience) &&
    value.experience.every(isCvEntry) &&
    Array.isArray(value.education) &&
    value.education.every(isCvEntry) &&
    Array.isArray(value.extras) &&
    value.extras.every(isCvEntry)
  );
}

function isCvFile(value: unknown): value is CvFile {
  return isRecord(value) && isCvLocale(value.pl) && isCvLocale(value.en);
}

function readSite(value: unknown): SiteContent {
  if (!isSiteContent(value)) {
    throw new Error('Site content is missing required fields.');
  }
  return value;
}

function readCv(value: unknown): CvFile {
  if (!isCvFile(value)) {
    throw new Error('CV content is missing required fields.');
  }
  return value;
}

const sites: Record<LocaleCode, SiteContent> = {
  pl: readSite(pl),
  en: readSite(en),
};

const cvByLocale = readCv(cvFile);

const LOCALES: readonly LocaleCode[] = ['pl', 'en'];

export function isLocale(value: string | undefined | null): value is LocaleCode {
  return value === 'pl' || value === 'en';
}

export function localeFromPathname(pathname: string): LocaleCode {
  const segment = pathname.split('/').filter(Boolean)[0];
  return isLocale(segment) ? segment : 'pl';
}

export function siteFor(locale: LocaleCode): SiteContent {
  return sites[locale];
}

export function cvFor(locale: LocaleCode): CvLocaleContent {
  return cvByLocale[locale];
}

export function courseBySlug(slug: string, locale: LocaleCode): CourseContent | undefined {
  return sites[locale].courses.items.find((course) => course.slug === slug);
}

export function compendiumBySlug(slug: string, locale: LocaleCode): CompendiumTerm | undefined {
  return sites[locale].compendium.items.find((term) => term.slug === slug);
}

export function noteBySlug(slug: string, locale: LocaleCode): NoteContent | undefined {
  return sites[locale].notes.items.find((note) => note.slug === slug);
}

export const supportedLocales: readonly LocaleCode[] = LOCALES;

const UNLOCALIZED = new Set(['login', 'register']);

export function pathForLocale(pathname: string, search: string, hash: string, next: LocaleCode): string | null {
  const segments = pathname.split('/').filter(Boolean);
  if (UNLOCALIZED.has(segments[0] ?? '')) {
    return null;
  }
  if (isLocale(segments[0])) {
    segments[0] = next;
  } else {
    segments.unshift(next);
  }
  return `/${segments.join('/')}${search}${hash}`;
}
