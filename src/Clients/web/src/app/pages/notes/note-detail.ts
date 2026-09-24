import { Component, computed, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-note-detail',
  imports: [RouterLink],
  templateUrl: './note-detail.html',
  styleUrl: './note-detail.scss',
})
export class NoteDetailPage {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly slug = toSignal(this.route.paramMap.pipe(map((p) => p.get('slug') ?? '')), {
    initialValue: this.route.snapshot.paramMap.get('slug') ?? '',
  });

  protected readonly locale = this.content.locale;
  protected readonly note = computed(() => this.content.noteBySlug(this.slug()));

  constructor() {
    effect(() => {
      if (!this.note()) {
        void this.router.navigate(['/', this.locale(), 'notes']);
      }
    });
  }
}
