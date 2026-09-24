import { Component, computed, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-course-detail',
  imports: [RouterLink],
  templateUrl: './course-detail.html',
  styleUrl: './course-detail.scss',
})
export class CourseDetailPage {
  private readonly content = inject(ContentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly slug = toSignal(this.route.paramMap.pipe(map((p) => p.get('slug') ?? '')), {
    initialValue: this.route.snapshot.paramMap.get('slug') ?? '',
  });

  protected readonly locale = this.content.locale;
  protected readonly course = computed(() => this.content.courseBySlug(this.slug()));

  constructor() {
    effect(() => {
      if (!this.course()) {
        void this.router.navigate(['/', this.locale(), 'courses']);
      }
    });
  }
}
