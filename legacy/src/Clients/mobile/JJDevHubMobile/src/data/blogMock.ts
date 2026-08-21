export interface BlogPost {
  slug: string;
  title: string;
  excerpt: string;
  intro: string;
  codeSample: string;
  codeLanguage: string;
}

export const MOCK_BLOG_POSTS: BlogPost[] = [
  {
    slug: 'angular-signals',
    title: 'Angular Signals: krótki start',
    excerpt: 'signal(), computed() i efekty w jednym miejscu.',
    intro:
      'Od Angular 16+ sygnały są preferowanym sposobem na lokalny stan reaktywny.',
    codeSample: `import { Component, signal, computed } from '@angular/core';

@Component({
  selector: 'app-counter',
  template: \`
    <p>{{ count() }} — podwojone: {{ doubled() }}</p>
  \`,
})
export class Counter {
  readonly count = signal(0);
  readonly doubled = computed(() => this.count() * 2);
}`,
    codeLanguage: 'typescript',
  },
  {
    slug: 'rxjs-debounce',
    title: 'RxJS: debounce w wyszukiwarce',
    excerpt: 'Ograniczanie zapytań przy wpisywaniu tekstu.',
    intro:
      'Operator debounceTime czeka na pauzę w strumieniu zdarzeń zanim wyemituje ostatnią wartość.',
    codeSample: `q.valueChanges.pipe(
  debounceTime(300),
  distinctUntilChanged(),
).subscribe(term => { });`,
    codeLanguage: 'typescript',
  },
];
