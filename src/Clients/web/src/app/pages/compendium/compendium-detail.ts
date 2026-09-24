import { Component, computed, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-compendium-detail',
  imports: [RouterLink],
  templateUrl: './compendium-detail.html',
  styleUrl: './compendium-detail.scss',
})
export class CompendiumDetailPage {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly slug = toSignal(this.route.paramMap.pipe(map((p) => p.get('slug') ?? '')), {
    initialValue: this.route.snapshot.paramMap.get('slug') ?? '',
  });

  protected readonly locale = this.content.locale;
  protected readonly backLabel = computed(() => this.content.site().compendium.backLabel);
  protected readonly term = computed(() => this.content.compendiumBySlug(this.slug()));

  constructor() {
    effect(() => {
      if (!this.term()) {
        void this.router.navigate(['/', this.locale(), 'compendium']);
      }
    });
  }
}
