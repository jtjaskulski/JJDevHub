import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-courses',
  imports: [RouterLink],
  templateUrl: './courses.html',
  styleUrl: './courses.scss',
})
export class CoursesPage {
  private readonly content = inject(ContentService);

  protected readonly locale = this.content.locale;
  protected readonly section = computed(() => this.content.site().courses);
}
