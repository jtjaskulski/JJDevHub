import { ApiException, HttpValidationProblemDetails } from '../api/jjdevhub-api.client';

import { describeApiError } from './api-error';

describe('describeApiError', () => {
  it('maps 401 to a login message', () => {
    const err = new ApiException('Unauthorized', 401, '', {}, null);
    expect(describeApiError(err)).toBe('Invalid email or password.');
  });

  it('joins validation errors', () => {
    const err = new HttpValidationProblemDetails({
      errors: { Password: ['too short', 'needs a digit'] },
    });
    expect(describeApiError(err)).toBe('too short needs a digit');
  });
});
