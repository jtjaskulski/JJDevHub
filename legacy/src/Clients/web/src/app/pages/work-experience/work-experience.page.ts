import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../services/api.service';
import { WorkExperience } from '../../models/work-experience.model';
import { resolveApiErrorFromHttp } from '../../core/api-error-messages';

@Component({
  selector: 'app-work-experience',
  imports: [CommonModule, MatCardModule, MatChipsModule, MatProgressSpinnerModule, MatIconModule],
  templateUrl: './work-experience.page.html',
  styleUrl: './work-experience.page.scss',
})
export class WorkExperiencePage implements OnInit {
  experiences = signal<WorkExperience[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getWorkExperiences(true).subscribe({
      next: (data) => {
        this.experiences.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          resolveApiErrorFromHttp(err, 'Failed to load work experiences. Is the API running?'),
        );
        this.loading.set(false);
        console.error('API error:', err);
      },
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
    });
  }
}
