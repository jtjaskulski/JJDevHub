export interface WorkExperience {
  id: string;
  companyName: string;
  position: string;
  period: {
    start: string;
    end: string | null;
  };
  isPublic: boolean;
  isActive: boolean;
  createdDate: string;
  modifiedDate: string | null;
}
