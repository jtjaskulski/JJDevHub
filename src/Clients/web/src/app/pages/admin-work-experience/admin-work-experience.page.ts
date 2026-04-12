import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../services/api.service';
import { WorkExperience } from '../../models/work-experience.model';
import { resolveApiErrorFromHttp } from '../../core/api-error-messages';

@Component({
  selector: 'app-admin-work-experience',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
  ],
  templateUrl: './admin-work-experience.page.html',
  styleUrl: './admin-work-experience.page.scss',
})
export class AdminWorkExperiencePage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly snack = inject(MatSnackBar);

  readonly displayedColumns: string[] = [
    'companyName',
    'position',
    'period',
    'isPublic',
    'actions',
  ];

  items = signal<WorkExperience[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  saving = signal(false);

  /** `null` — brak formularza; `'new'` — tworzenie; inaczej `id` edycji. */
  editingId = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    companyName: ['', Validators.required],
    position: ['', Validators.required],
    startDate: ['', Validators.required],
    endDate: [''],
    isPublic: [true],
    version: [0],
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getWorkExperiences(false).subscribe({
      next: (data) => {
        this.items.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(
          resolveApiErrorFromHttp(err, 'Nie udało się wczytać listy. Czy API działa?'),
        );
        this.loading.set(false);
      },
    });
  }

  startCreate(): void {
    this.editingId.set('new');
    this.form.reset({
      companyName: '',
      position: '',
      startDate: '',
      endDate: '',
      isPublic: true,
      version: 0,
    });
  }

  startEdit(row: WorkExperience): void {
    this.editingId.set(row.id);
    this.form.patchValue({
      companyName: row.companyName,
      position: row.position,
      startDate: row.startDate.slice(0, 10),
      endDate: row.endDate ? row.endDate.slice(0, 10) : '',
      isPublic: row.isPublic,
      version: row.version,
    });
  }

  cancelForm(): void {
    this.editingId.set(null);
    this.form.reset();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const startDate = new Date(`${raw.startDate}T12:00:00`).toISOString();
    const endDate = raw.endDate ? new Date(`${raw.endDate}T12:00:00`).toISOString() : null;

    const id = this.editingId();
    if (id === null) {
      return;
    }

    this.saving.set(true);

    if (id === 'new') {
      this.api
        .createWorkExperience({
          companyName: raw.companyName,
          position: raw.position,
          startDate,
          endDate,
          isPublic: raw.isPublic,
        })
        .subscribe({
          next: () => {
            this.snack.open('Dodano wpis', 'OK', { duration: 3000 });
            this.saving.set(false);
            this.cancelForm();
            this.reload();
          },
          error: (err) => {
            this.snack.open(
              resolveApiErrorFromHttp(err, 'Błąd zapisu'),
              'Zamknij',
              { duration: 5000 },
            );
            this.saving.set(false);
          },
        });
      return;
    }

    this.api
      .updateWorkExperience(id, {
        version: raw.version,
        companyName: raw.companyName,
        position: raw.position,
        startDate,
        endDate,
        isPublic: raw.isPublic,
      })
      .subscribe({
        next: () => {
          this.snack.open('Zapisano zmiany', 'OK', { duration: 3000 });
          this.saving.set(false);
          this.cancelForm();
          this.reload();
        },
        error: (err) => {
          this.snack.open(
            resolveApiErrorFromHttp(err, 'Błąd zapisu'),
            'Zamknij',
            { duration: 5000 },
          );
          this.saving.set(false);
        },
      });
  }

  deleteRow(row: WorkExperience): void {
    if (!globalThis.confirm(`Usunąć „${row.companyName}”?`)) return;

    this.api.deleteWorkExperience(row.id).subscribe({
      next: () => {
        this.snack.open('Usunięto', 'OK', { duration: 3000 });
        this.reload();
      },
      error: (err) => {
        this.snack.open(
          resolveApiErrorFromHttp(err, 'Błąd usuwania'),
          'Zamknij',
          { duration: 5000 },
        );
      },
    });
  }

  formatPeriod(row: WorkExperience): string {
    const s = new Date(row.startDate).toLocaleDateString(undefined, {
      year: 'numeric',
      month: 'short',
    });
    const e = row.endDate
      ? new Date(row.endDate).toLocaleDateString(undefined, { year: 'numeric', month: 'short' })
      : 'obecnie';
    return `${s} — ${e}`;
  }
}
