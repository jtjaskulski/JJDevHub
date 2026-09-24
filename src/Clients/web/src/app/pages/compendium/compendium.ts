import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-compendium',
  imports: [RouterLink],
  templateUrl: './compendium.html',
  styleUrl: './compendium.scss',
})
export class CompendiumPage {
  private readonly content = inject(ContentService);

  protected readonly locale = this.content.locale;
  protected readonly section = computed(() => this.content.site().compendium);
}
