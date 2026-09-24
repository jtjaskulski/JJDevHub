import { Component, computed, inject, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import type { LocaleCode } from './content/content.model';
import { ContentService } from './content/content.service';
import { LenisService } from './motion/lenis.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly content = inject(ContentService);
  private readonly router = inject(Router);
  private readonly lenis = inject(LenisService);

  protected readonly title = 'JJDevHub';
  protected readonly locale = this.content.locale;
  protected readonly nav = computed(() => this.content.site().nav);

  ngOnInit(): void {
    this.lenis.start();
  }

  protected switchLocale(next: LocaleCode): void {
    if (next === this.locale()) {
      return;
    }

    const url = this.router.url;
    const [pathPart, query = ''] = url.split('?');
    const [pathname, hash = ''] = pathPart.split('#');
    const segments = pathname.split('/').filter(Boolean);

    if (ContentService.isLocaleCode(segments[0])) {
      segments[0] = next;
    } else {
      segments.unshift(next);
    }

    const target = `/${segments.join('/')}${query ? `?${query}` : ''}${hash ? `#${hash}` : ''}`;
    void this.router.navigateByUrl(target);
  }
}
