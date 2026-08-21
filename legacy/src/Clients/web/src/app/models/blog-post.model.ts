export interface BlogPost {
  slug: string;
  title: string;
  excerpt: string;
  /** Tekst wprowadzenia (HTML-safe: tylko tekst w szablonie). */
  intro: string;
  codeSample: string;
  codeLanguage: string;
}
