import { Component, inject } from '@angular/core';

import { ContentService } from '../../content/content.service';

@Component({
  selector: 'app-cv',
  templateUrl: './cv.html',
  styleUrl: './cv.scss',
})
export class CvPage {
  private readonly content = inject(ContentService);

  protected readonly cv = this.content.cv;
}
