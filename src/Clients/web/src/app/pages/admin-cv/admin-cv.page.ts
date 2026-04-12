import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { CurriculumVitae } from '../../models/curriculum-vitae.model';
import { resolveApiErrorFromHttp } from '../../core/api-error-messages';

@Component({
  selector: 'app-admin-cv',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './admin-cv.page.html',
  styleUrl: './admin-cv.page.scss',
})
export class AdminCvPage implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly displayedColumns = ['name', 'email', 'actions'] as const;

  items = signal<CurriculumVitae[]>([]);
  loading = signal(true);
  generatingId = signal<string | null>(null);
  error = signal<string | null>(null);

  jobForm = this.fb.nonNullable.group({
    jobApplicationId: [''],
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getCurriculumVitaes().subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(resolveApiErrorFromHttp(err, 'Nie udało się wczytać CV.'));
        this.loading.set(false);
      },
    });
  }

  displayName(cv: CurriculumVitae): string {
    const p = cv.personalInfo;
    return `${p.firstName} ${p.lastName}`.trim();
  }

  generatePdf(cv: CurriculumVitae): void {
    const jobId = this.jobForm.controls.jobApplicationId.value.trim();
    this.generatingId.set(cv.id);
    this.api.generateCvPdf(cv.id, jobId || null).subscribe({
      next: (res) => {
        this.api.downloadCvPdfBlob(res.fileId).subscribe({
          next: (blob) => {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `cv-${cv.id}.pdf`;
            a.click();
            URL.revokeObjectURL(url);
            this.snack.open('Pobrano PDF', 'OK', { duration: 3000 });
            this.generatingId.set(null);
          },
          error: (err) => {
            this.snack.open(resolveApiErrorFromHttp(err, 'Błąd pobierania PDF'), 'Zamknij', {
              duration: 6000,
            });
            this.generatingId.set(null);
          },
        });
      },
      error: (err) => {
        this.snack.open(resolveApiErrorFromHttp(err, 'Błąd generowania PDF'), 'Zamknij', {
          duration: 6000,
        });
        this.generatingId.set(null);
      },
    });
  }
}
