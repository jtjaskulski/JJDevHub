import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectChange, MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { ApiService } from '../../services/api.service';
import {
  JobApplication,
  JobApplicationDashboard,
} from '../../models/job-application.model';
import { resolveApiErrorFromHttp } from '../../core/api-error-messages';

const STATUS_OPTIONS = [
  'Draft',
  'Applied',
  'PhoneScreen',
  'TechnicalInterview',
  'OnSite',
  'Offer',
  'Accepted',
  'Rejected',
  'Withdrawn',
] as const;

@Component({
  selector: 'app-admin-tracker',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatExpansionModule,
  ],
  templateUrl: './admin-tracker.page.html',
  styleUrl: './admin-tracker.page.scss',
})
export class AdminTrackerPage implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly snack = inject(MatSnackBar);

  readonly displayedColumns: string[] = [
    'company',
    'position',
    'status',
    'applied',
    'actions',
  ];

  readonly statusOptions = STATUS_OPTIONS;

  items = signal<JobApplication[]>([]);
  dashboard = signal<JobApplicationDashboard | null>(null);
  loading = signal(true);
  saving = signal(false);
  error = signal<string | null>(null);

  /** Pusty string = brak filtra po statusie (Material `mat-option` nie używa `null`). */
  filterStatus = signal<string>('');
  filterCompany = signal('');

  editingId = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    companyName: ['', Validators.required],
    location: [''],
    websiteUrl: [''],
    industry: [''],
    position: ['', Validators.required],
    status: ['Applied' as (typeof STATUS_OPTIONS)[number], Validators.required],
    appliedDate: ['', Validators.required],
    linkedCurriculumVitaeId: [''],
    version: [0],
  });

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getJobApplicationDashboard().subscribe({
      next: (d) => this.dashboard.set(d),
      error: () => this.dashboard.set(null),
    });
    this.api
      .getJobApplications({
        status: this.filterStatus() || undefined,
        companyContains: this.filterCompany().trim() || undefined,
      })
      .subscribe({
        next: (data) => {
          this.items.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(
            resolveApiErrorFromHttp(err, 'Nie udało się wczytać aplikacji.'),
          );
          this.loading.set(false);
        },
      });
  }

  applyFilters(): void {
    this.reload();
  }

  clearFilters(): void {
    this.filterStatus.set('');
    this.filterCompany.set('');
    this.reload();
  }

  onStatusFilterChange(event: MatSelectChange): void {
    this.filterStatus.set(typeof event.value === 'string' ? event.value : '');
    this.applyFilters();
  }

  startCreate(): void {
    this.editingId.set('new');
    const today = new Date().toISOString().slice(0, 10);
    this.form.reset({
      companyName: '',
      location: '',
      websiteUrl: '',
      industry: '',
      position: '',
      status: 'Applied',
      appliedDate: today,
      linkedCurriculumVitaeId: '',
      version: 0,
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  startEdit(row: JobApplication): void {
    this.editingId.set(row.id);
    this.form.patchValue({
      companyName: row.companyName,
      location: row.location ?? '',
      websiteUrl: row.websiteUrl ?? '',
      industry: row.industry ?? '',
      position: row.position,
      status: row.status as (typeof STATUS_OPTIONS)[number],
      appliedDate: row.appliedDate.slice(0, 10),
      linkedCurriculumVitaeId: row.linkedCurriculumVitaeId ?? '',
      version: row.version,
    });
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.saving.set(true);
    const linked = v.linkedCurriculumVitaeId.trim();
    const payload = {
      companyName: v.companyName.trim(),
      location: v.location.trim() || null,
      websiteUrl: v.websiteUrl.trim() || null,
      industry: v.industry.trim() || null,
      position: v.position.trim(),
      status: v.status,
      appliedDate: v.appliedDate,
      linkedCurriculumVitaeId: linked ? linked : null,
    };

    const id = this.editingId();
    if (id === 'new') {
      this.api.createJobApplication(payload).subscribe({
        next: () => {
          this.snack.open('Dodano aplikację', 'OK', { duration: 3000 });
          this.editingId.set(null);
          this.saving.set(false);
          this.reload();
        },
        error: (err) => {
          this.snack.open(resolveApiErrorFromHttp(err, 'Błąd zapisu'), 'Zamknij', {
            duration: 6000,
          });
          this.saving.set(false);
        },
      });
    } else if (id) {
      this.api
        .updateJobApplication(id, {
          ...payload,
          version: v.version,
        })
        .subscribe({
          next: () => {
            this.snack.open('Zapisano', 'OK', { duration: 3000 });
            this.editingId.set(null);
            this.saving.set(false);
            this.reload();
          },
          error: (err) => {
            this.snack.open(resolveApiErrorFromHttp(err, 'Błąd zapisu'), 'Zamknij', {
              duration: 6000,
            });
            this.saving.set(false);
          },
        });
    }
  }

  deleteRow(row: JobApplication): void {
    if (!confirm(`Usunąć aplikację u ${row.companyName}?`)) return;
    this.api.deleteJobApplication(row.id).subscribe({
      next: () => {
        this.snack.open('Usunięto', 'OK', { duration: 3000 });
        this.reload();
      },
      error: (err) => {
        this.snack.open(resolveApiErrorFromHttp(err, 'Błąd usuwania'), 'Zamknij', {
          duration: 6000,
        });
      },
    });
  }

  dashboardEntries(): { key: string; label: string; count: number }[] {
    const d = this.dashboard();
    if (!d) return [];
    return Object.entries(d.countByStatus).map(([key, count]) => ({
      key,
      label: key,
      count,
    }));
  }
}
