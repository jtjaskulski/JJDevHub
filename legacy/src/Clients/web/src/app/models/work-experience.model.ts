/** Odpowiada WorkExperienceDto z Content.Api (camelCase z JSON). */
export interface WorkExperience {
  id: string;
  version: number;
  companyName: string;
  position: string;
  startDate: string;
  endDate: string | null;
  isPublic: boolean;
  isCurrent: boolean;
  durationInMonths: number;
}
