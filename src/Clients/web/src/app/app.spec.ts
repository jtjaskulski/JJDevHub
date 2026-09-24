import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { App } from './app';
import { routes } from './app.routes';

function mockMatchMedia(): void {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }),
  });
}

describe('App', () => {
  beforeEach(async () => {
    mockMatchMedia();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient()],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the brand', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.brand')?.textContent).toContain('JJDevHub');
  });

  it('keeps login unlocalized when switching language', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/login');
    fixture.detectChanges();
    await fixture.whenStable();

    const en = [...(fixture.nativeElement as HTMLElement).querySelectorAll('button.lang-btn')].find((button) =>
      button.textContent?.includes('EN'),
    ) as HTMLButtonElement | undefined;
    en?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(router.url.split(/[?#]/)[0]).toBe('/login');
  });

  it('sets the document language from the locale prefix', async () => {
    const fixture = TestBed.createComponent(App);
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/en');
    fixture.detectChanges();
    await fixture.whenStable();
    TestBed.flushEffects();

    expect(document.documentElement.lang).toBe('en');
  });
});
