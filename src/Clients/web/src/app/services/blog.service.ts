import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { BlogPost } from '../models/blog-post.model';

/** Mock do czasu endpointów Content API (blog / snippets). */
const MOCK_POSTS: BlogPost[] = [
  {
    slug: 'angular-signals',
    title: 'Angular Signals: krótki start',
    excerpt: 'signal(), computed() i efekty w jednym miejscu.',
    intro:
      'Od Angular 16+ sygnały są preferowanym sposobem na lokalny stan reaktywny. ' +
      'Poniżej minimalny przykład zależności między sygnałami.',
    codeSample: `import { Component, signal, computed } from '@angular/core';

@Component({
  selector: 'app-counter',
  template: \`
    <p>{{ count() }} — podwojone: {{ doubled() }}</p>
    <button type="button" (click)="count.update(c => c + 1)">+1</button>
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
      'Operator debounceTime czeka na pauzę w strumieniu zdarzeń zanim wyemituje ostatnią wartość. ' +
      'Przydatne przy polach wyszukiwania podpiętych pod API.',
    codeSample: `import { FormControl } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';

const q = new FormControl('', { nonNullable: true });

q.valueChanges.pipe(
  debounceTime(300),
  distinctUntilChanged(),
).subscribe(term => {
  // wywołaj API z term
});`,
    codeLanguage: 'typescript',
  },
];

@Injectable({ providedIn: 'root' })
export class BlogService {
  getPosts(): Observable<BlogPost[]> {
    return of([...MOCK_POSTS]);
  }

  getBySlug(slug: string): Observable<BlogPost | undefined> {
    return of(MOCK_POSTS.find((p) => p.slug === slug));
  }
}
