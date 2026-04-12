export type ApplicationStatus =
  | 'Draft'
  | 'Applied'
  | 'PhoneScreen'
  | 'TechnicalInterview'
  | 'OnSite'
  | 'Offer'
  | 'Accepted'
  | 'Rejected'
  | 'Withdrawn';

export interface JobApplicationRequirement {
  id: string;
  description: string;
  category: string;
  priority: string;
  isMet: boolean;
}

export interface JobApplicationNote {
  id: string;
  content: string;
  createdAt: string;
  noteType: string;
}

export interface JobApplicationInterviewStage {
  id: string;
  stageName: string;
  scheduledAt: string;
  status: string;
  feedback: string | null;
}

export interface JobApplication {
  id: string;
  version: number;
  companyName: string;
  location: string | null;
  websiteUrl: string | null;
  industry: string | null;
  position: string;
  status: ApplicationStatus;
  appliedDate: string;
  linkedCurriculumVitaeId: string | null;
  requirements: JobApplicationRequirement[];
  notes: JobApplicationNote[];
  interviewStages: JobApplicationInterviewStage[];
  createdDate: string;
  modifiedDate: string | null;
  lastModifiedAt: string;
}

export interface JobApplicationDashboard {
  total: number;
  countByStatus: Record<string, number>;
}
