export interface PersonalInfo {
  firstName: string;
  lastName: string;
  email: string;
  phone: string | null;
  location: string | null;
  bio: string | null;
}

export type SkillLevel = 'Beginner' | 'Intermediate' | 'Advanced' | 'Expert';
export type EducationDegree = 'Certificate' | 'BSc' | 'MSc' | 'PhD';

export interface CvSkill {
  id: string;
  name: string;
  category: string;
  level: SkillLevel;
}

export interface CvEducation {
  id: string;
  institution: string;
  fieldOfStudy: string;
  degree: EducationDegree;
  periodStart: string;
  periodEnd: string | null;
}

export interface CvProject {
  id: string;
  name: string;
  description: string;
  url: string | null;
  technologies: string[];
  periodStart: string;
  periodEnd: string | null;
}

export interface CurriculumVitae {
  id: string;
  version: number;
  personalInfo: PersonalInfo;
  skills: CvSkill[];
  educations: CvEducation[];
  projects: CvProject[];
  workExperienceIds: string[];
  createdDate: string;
  modifiedDate: string | null;
  lastModifiedAt: string;
}

export interface GenerateCvPdfResponse {
  fileId: string;
  downloadPath: string;
}
