import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { ApiService } from '../../services/api.service';
import {
  CurriculumVitae,
  EducationDegree,
  SkillLevel,
} from '../../models/curriculum-vitae.model';
import { WorkExperience } from '../../models/work-experience.model';
import { resolveApiErrorFromHttp } from '../../core/api-error-messages';

const SKILL_LEVELS: SkillLevel[] = ['Beginner', 'Intermediate', 'Advanced', 'Expert'];
const DEGREES: EducationDegree[] = ['Certificate', 'BSc', 'MSc', 'PhD'];

@Component({
  selector: 'app-admin-cv',
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
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatExpansionModule,
  ],
  templateUrl: './admin-cv.page.html',
  styleUrl: './admin-cv.page.scss',
})
export class AdminCvPage implements OnInit {
  private readonly api = inject(ApiService);
  private readonly snack = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);

  readonly displayedColumns = ['name', 'email', 'actions'] as const;
  readonly skillLevels = SKILL_LEVELS;
  readonly degrees = DEGREES;

  items = signal<CurriculumVitae[]>([]);
  workExperiences = signal<WorkExperience[]>([]);
  loading = signal(true);
  saving = signal(false);
  generatingId = signal<string | null>(null);
  error = signal<string | null>(null);
  editingId = signal<string | null>(null);

  jobForm = this.fb.nonNullable.group({
    jobApplicationId: [''],
  });

  personalForm = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    location: [''],
    bio: [''],
    version: [0],
  });

  skillForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    category: ['Backend', Validators.required],
    level: ['Intermediate' as SkillLevel, Validators.required],
  });

  educationForm = this.fb.nonNullable.group({
    institution: ['', Validators.required],
    fieldOfStudy: ['', Validators.required],
    degree: ['MSc' as EducationDegree, Validators.required],
    periodStart: ['', Validators.required],
    periodEnd: [''],
  });

  projectForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    url: [''],
    technologies: [''],
    periodStart: ['', Validators.required],
    periodEnd: [''],
  });

  linkWeForm = this.fb.nonNullable.group({
    workExperienceId: ['', Validators.required],
  });

  ngOnInit(): void {
    this.reload();
    this.api.getWorkExperiences(false).subscribe({
      next: (data) => this.workExperiences.set(data),
      error: () => this.workExperiences.set([]),
    });
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

  workExperienceLabel(id: string): string {
    const we = this.workExperiences().find((w) => w.id === id);
    return we ? `${we.position} @ ${we.companyName}` : id;
  }

  startCreate(): void {
    this.editingId.set('new');
    this.personalForm.reset({
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      location: '',
      bio: '',
      version: 0,
    });
  }

  startEdit(cv: CurriculumVitae): void {
    this.editingId.set(cv.id);
    this.personalForm.patchValue({
      firstName: cv.personalInfo.firstName,
      lastName: cv.personalInfo.lastName,
      email: cv.personalInfo.email,
      phone: cv.personalInfo.phone ?? '',
      location: cv.personalInfo.location ?? '',
      bio: cv.personalInfo.bio ?? '',
      version: cv.version,
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  savePersonal(): void {
    if (this.personalForm.invalid) {
      this.personalForm.markAllAsTouched();
      return;
    }
    const v = this.personalForm.getRawValue();
    const payload = {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      email: v.email.trim(),
      phone: v.phone.trim() || null,
      location: v.location.trim() || null,
      bio: v.bio.trim() || null,
    };
    this.saving.set(true);
    const id = this.editingId();
    if (id === 'new') {
      this.api.createCurriculumVitae(payload).subscribe({
        next: () => {
          this.snack.open('Utworzono CV', 'OK', { duration: 3000 });
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
        .updateCurriculumVitaePersonalInfo(id, { ...payload, version: v.version })
        .subscribe({
          next: () => {
            this.snack.open('Zapisano dane osobowe', 'OK', { duration: 3000 });
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

  deleteRow(cv: CurriculumVitae): void {
    if (!confirm(`Usunąć CV ${this.displayName(cv)}?`)) return;
    this.api.deleteCurriculumVitae(cv.id).subscribe({
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

  private afterNestedChange(okMsg: string): void {
    this.snack.open(okMsg, 'OK', { duration: 3000 });
    this.reload();
  }

  addSkill(cv: CurriculumVitae): void {
    if (this.skillForm.invalid) return;
    const v = this.skillForm.getRawValue();
    this.api
      .addCurriculumVitaeSkill(cv.id, {
        version: cv.version,
        name: v.name.trim(),
        category: v.category.trim(),
        level: v.level,
      })
      .subscribe({
        next: () => {
          this.skillForm.reset({ name: '', category: 'Backend', level: 'Intermediate' });
          this.afterNestedChange('Dodano umiejętność');
        },
        error: (err) =>
          this.snack.open(resolveApiErrorFromHttp(err, 'Błąd'), 'Zamknij', { duration: 6000 }),
      });
  }

  removeSkill(cv: CurriculumVitae, skillId: string): void {
    this.api.removeCurriculumVitaeSkill(cv.id, skillId, cv.version).subscribe({
      next: () => this.afterNestedChange('Usunięto umiejętność'),
      error: (err) =>
        this.snack.open(resolveApiErrorFromHttp(err, 'Błąd'), 'Zamknij', { duration: 6000 }),
    });
  }

  addEducation(cv: CurriculumVitae): void {
    if (this.educationForm.invalid) return;
    const v = this.educationForm.getRawValue();
    this.api
      .addCurriculumVitaeEducation(cv.id, {
        version: cv.version,
        institution: v.institution.trim(),
        fieldOfStudy: v.fieldOfStudy.trim(),
        degree: v.degree,
        periodStart: v.periodStart,
        periodEnd: v.periodEnd.trim() || null,
      })
      .subscribe({
        next: () => {
          this.educationForm.reset({
            institution: '',
            fieldOfStudy: '',
            degree: 'MSc',
            periodStart: '',
            periodEnd: '',
          });
          this.afterNestedChange('Dodano wykształcenie');
        },
        error: (err) =>
          this.snack.open(resolveApiErrorFromHttp(err, 'Błąd'), 'Zamknij', { duration: 6000 }),
      });
  }

  addProject(cv: CurriculumVitae): void {
    if (this.projectForm.invalid) return;
    const v = this.projectForm.getRawValue();
    const technologies = v.technologies
      .split(',')
      .map((t) => t.trim())
      .filter((t) => t.length > 0);
    this.api
      .addCurriculumVitaeProject(cv.id, {
        version: cv.version,
        name: v.name.trim(),
        description: v.description.trim(),
        url: v.url.trim() || null,
        technologies,
        periodStart: v.periodStart,
        periodEnd: v.periodEnd.trim() || null,
      })
      .subscribe({
        next: () => {
          this.projectForm.reset({
            name: '',
            description: '',
            url: '',
            technologies: '',
            periodStart: '',
            periodEnd: '',
          });
          this.afterNestedChange('Dodano projekt');
        },
        error: (err) =>
          this.snack.open(resolveApiErrorFromHttp(err, 'Błąd'), 'Zamknij', { duration: 6000 }),
      });
  }

  linkWorkExperience(cv: CurriculumVitae): void {
    if (this.linkWeForm.invalid) return;
    const weId = this.linkWeForm.controls.workExperienceId.value;
    this.api
      .linkCurriculumVitaeWorkExperience(cv.id, { version: cv.version, workExperienceId: weId })
      .subscribe({
        next: () => {
          this.linkWeForm.reset({ workExperienceId: '' });
          this.afterNestedChange('Powiązano doświadczenie');
        },
        error: (err) =>
          this.snack.open(resolveApiErrorFromHttp(err, 'Błąd'), 'Zamknij', { duration: 6000 }),
      });
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
