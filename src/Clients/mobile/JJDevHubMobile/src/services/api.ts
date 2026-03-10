import {WorkExperience} from '../models/work-experience';

const API_BASE_URL = __DEV__
  ? 'http://10.0.2.2:8081'
  : 'https://jjdevhub.com';

export async function getWorkExperiences(
  publicOnly = false,
): Promise<WorkExperience[]> {
  const url = new URL(`${API_BASE_URL}/api/content/work-experiences`);
  if (publicOnly) {
    url.searchParams.set('publicOnly', 'true');
  }
  const response = await fetch(url.toString());
  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }
  return response.json();
}
