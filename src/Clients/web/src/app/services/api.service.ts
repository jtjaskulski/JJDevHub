import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { WorkExperience } from '../models/work-experience.model';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  getWorkExperiences(publicOnly = false): Observable<WorkExperience[]> {
    const url = `${this.baseUrl}/api/content/work-experiences`;
    if (publicOnly) {
      return this.http.get<WorkExperience[]>(url, { params: { publicOnly: 'true' } });
    }
    return this.http.get<WorkExperience[]>(url);
  }

  getWorkExperience(id: string): Observable<WorkExperience> {
    return this.http.get<WorkExperience>(`${this.baseUrl}/api/content/work-experiences/${id}`);
  }

  createWorkExperience(
    data: Omit<WorkExperience, 'id' | 'isActive' | 'createdDate' | 'modifiedDate'>
  ): Observable<WorkExperience> {
    return this.http.post<WorkExperience>(`${this.baseUrl}/api/content/work-experiences`, data);
  }

  updateWorkExperience(id: string, data: Partial<WorkExperience>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/api/content/work-experiences/${id}`, data);
  }

  deleteWorkExperience(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/content/work-experiences/${id}`);
  }
}
