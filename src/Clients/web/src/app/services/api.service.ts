import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkExperience } from '../models/work-experience.model';
import {
  CurriculumVitae,
  GenerateCvPdfResponse,
} from '../models/curriculum-vitae.model';
import {
  JobApplication,
  JobApplicationDashboard,
} from '../models/job-application.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly contentUrl = environment.contentApiUrl;

  constructor(private http: HttpClient) {}

  getWorkExperiences(publicOnly = false): Observable<WorkExperience[]> {
    const url = `${this.contentUrl}/work-experiences`;
    if (publicOnly) {
      return this.http.get<WorkExperience[]>(url, { params: { publicOnly: 'true' } });
    }
    return this.http.get<WorkExperience[]>(url);
  }

  getWorkExperience(id: string): Observable<WorkExperience> {
    return this.http.get<WorkExperience>(`${this.contentUrl}/work-experiences/${id}`);
  }

  createWorkExperience(body: {
    companyName: string;
    position: string;
    startDate: string;
    endDate: string | null;
    isPublic: boolean;
  }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.contentUrl}/work-experiences`, body);
  }

  /** Body musi zawierać version (optymistyczna współbieżność). */
  updateWorkExperience(
    id: string,
    body: {
      version: number;
      companyName: string;
      position: string;
      startDate: string;
      endDate: string | null;
      isPublic: boolean;
    },
  ): Observable<void> {
    return this.http.put<void>(`${this.contentUrl}/work-experiences/${id}`, body);
  }

  deleteWorkExperience(id: string): Observable<void> {
    return this.http.delete<void>(`${this.contentUrl}/work-experiences/${id}`);
  }

  getJobApplications(params?: {
    status?: string;
    companyContains?: string;
  }): Observable<JobApplication[]> {
    const q: Record<string, string> = {};
    if (params?.status) q['status'] = params.status;
    if (params?.companyContains) q['companyContains'] = params.companyContains;
    return this.http.get<JobApplication[]>(`${this.contentUrl}/applications`, { params: q });
  }

  getJobApplicationDashboard(): Observable<JobApplicationDashboard> {
    return this.http.get<JobApplicationDashboard>(`${this.contentUrl}/applications/dashboard`);
  }

  getJobApplication(id: string): Observable<JobApplication> {
    return this.http.get<JobApplication>(`${this.contentUrl}/applications/${id}`);
  }

  createJobApplication(body: {
    companyName: string;
    location?: string | null;
    websiteUrl?: string | null;
    industry?: string | null;
    position: string;
    status: string;
    appliedDate: string;
    linkedCurriculumVitaeId?: string | null;
  }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.contentUrl}/applications`, body);
  }

  updateJobApplication(
    id: string,
    body: {
      version: number;
      companyName: string;
      location?: string | null;
      websiteUrl?: string | null;
      industry?: string | null;
      position: string;
      status: string;
      appliedDate: string;
      linkedCurriculumVitaeId?: string | null;
    },
  ): Observable<void> {
    return this.http.put<void>(`${this.contentUrl}/applications/${id}`, body);
  }

  deleteJobApplication(id: string): Observable<void> {
    return this.http.delete<void>(`${this.contentUrl}/applications/${id}`);
  }

  addJobApplicationRequirement(
    applicationId: string,
    body: {
      version: number;
      description: string;
      category: string;
      priority: string;
      isMet: boolean;
    },
  ): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.contentUrl}/applications/${applicationId}/requirements`,
      body,
    );
  }

  addJobApplicationNote(
    applicationId: string,
    body: {
      version: number;
      content: string;
      noteType: string;
      createdAt: string;
    },
  ): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.contentUrl}/applications/${applicationId}/notes`,
      body,
    );
  }

  addJobApplicationInterviewStage(
    applicationId: string,
    body: {
      version: number;
      stageName: string;
      scheduledAt: string;
      status: string;
      feedback?: string | null;
    },
  ): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(
      `${this.contentUrl}/applications/${applicationId}/interview-stages`,
      body,
    );
  }

  getCurriculumVitaes(): Observable<CurriculumVitae[]> {
    return this.http.get<CurriculumVitae[]>(`${this.contentUrl}/cv`);
  }

  generateCvPdf(
    curriculumVitaeId: string,
    jobApplicationId?: string | null,
  ): Observable<GenerateCvPdfResponse> {
    return this.http.post<GenerateCvPdfResponse>(`${this.contentUrl}/cv/${curriculumVitaeId}/pdf`, {
      jobApplicationId: jobApplicationId ?? null,
    });
  }

  downloadCvPdfBlob(fileId: string): Observable<Blob> {
    return this.http.get(`${this.contentUrl}/cv/pdf-download/${fileId}`, {
      responseType: 'blob',
    });
  }
}
