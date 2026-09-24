import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-notes',
  imports: [RouterLink],
  templateUrl: './notes.html',
  styleUrl: './notes.scss',
})
export class NotesPage {
  private readonly content = inject(ContentService);

  protected readonly locale = this.content.locale;
  protected readonly section = computed(() => this.content.site().notes);
}
