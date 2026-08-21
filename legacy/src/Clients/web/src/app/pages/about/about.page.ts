import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-about',
  imports: [MatCardModule, MatIconModule, MatChipsModule],
  templateUrl: './about.page.html',
  styleUrl: './about.page.scss',
})
export class AboutPage {}
