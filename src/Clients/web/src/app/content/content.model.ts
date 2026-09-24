export type LocaleCode = 'pl' | 'en';

export interface NavContent {
  home: string;
  cv: string;
  courses: string;
  compendium: string;
  notes: string;
  language: string;
  languagePl: string;
  languageEn: string;
}

export interface HomeContent {
  eyebrow: string;
  ctaCv: string;
  ctaCourses: string;
  currentTitle: string;
  currentLead: string;
  coursesTitle: string;
  coursesLead: string;
  notesTitle: string;
  notesLead: string;
  viewAll: string;
}

export interface LessonContent {
  slug: string;
  title: string;
  summary: string;
  url: string;
}

export interface ChapterContent {
  slug: string;
  title: string;
  summary: string;
}

export interface CourseContent {
  slug: string;
  title: string;
  subtitle: string;
  summary: string[];
  repoUrl: string;
  labUrl?: string;
  repoLabel: string;
  labLabel?: string;
  lessonsTitle?: string;
  lessons?: LessonContent[];
  chaptersTitle?: string;
  chapters?: ChapterContent[];
  plannedTitle?: string;
  plannedLead?: string;
  planned?: ChapterContent[];
}

export interface CoursesSection {
  title: string;
  lead: string;
  items: CourseContent[];
}

export interface CompendiumTerm {
  slug: string;
  term: string;
  definition: string[];
}

export interface CompendiumSection {
  title: string;
  lead: string;
  glossaryUrl: string;
  glossaryLabel: string;
  items: CompendiumTerm[];
}

export interface NoteContent {
  slug: string;
  title: string;
  date: string;
  summary: string;
  body: string[];
  relatedLessonSlug?: string;
  relatedCourseSlug?: string;
}

export interface NotesSection {
  title: string;
  lead: string;
  items: NoteContent[];
}

/** Site chrome and teaching content — no personal CV fields. */
export interface SiteContent {
  nav: NavContent;
  home: HomeContent;
  courses: CoursesSection;
  compendium: CompendiumSection;
  notes: NotesSection;
}

export interface CvEntry {
  title: string;
  company?: string;
  institution?: string;
  period: string;
  paragraphs: string[];
}

/** Personal CV for one locale. */
export interface CvLocaleContent {
  name: string;
  role: string;
  email: string;
  experienceTitle: string;
  educationTitle: string;
  extrasTitle: string;
  experience: CvEntry[];
  education: CvEntry[];
  extras: CvEntry[];
}

/** CV file shape: both locales in one JSON. */
export type CvFile = Record<LocaleCode, CvLocaleContent>;

/** Merged view for the active language. */
export interface LocaleContent {
  locale: LocaleCode;
  site: SiteContent;
  cv: CvLocaleContent;
}
