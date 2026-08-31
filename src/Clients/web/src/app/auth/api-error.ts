import { ApiException, HttpValidationProblemDetails } from '../api/jjdevhub-api.client';

export function describeApiError(err: unknown): string {
  if (err instanceof ApiException || (err != null && ApiException.isApiException(err))) {
    if (err.status === 401) {
      return 'Invalid email or password.';
    }
    return err.message || 'Request failed.';
  }

  if (err instanceof HttpValidationProblemDetails) {
    const messages = Object.values(err.errors ?? {})
      .flat()
      .filter(Boolean);
    if (messages.length) {
      return messages.join(' ');
    }
    return err.title || err.detail || 'Validation failed.';
  }

  return 'Something went wrong.';
}
