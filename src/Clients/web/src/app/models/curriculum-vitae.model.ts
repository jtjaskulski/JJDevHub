export interface PersonalInfo {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  location: string | null;
  bio: string | null;
}

export interface CurriculumVitae {
  id: string;
  version: number;
  personalInfo: PersonalInfo;
  skills: unknown[];
  educations: unknown[];
  projects: unknown[];
  workExperienceIds: string[];
  createdDate: string;
  modifiedDate: string | null;
  lastModifiedAt: string;
}

export interface GenerateCvPdfResponse {
  fileId: string;
  downloadPath: string;
}
