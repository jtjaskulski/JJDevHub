import { describeApiError, type ApiError } from './api-error';

describe('describeApiError', () => {
  it('maps 401 to a login message', () => {
    const err: ApiError = { status: 401, message: 'Unauthorized' };
    expect(describeApiError(err)).toBe('Invalid email or password.');
  });

  it('joins validation errors', () => {
    const err: ApiError = { status: 400, errors: { Password: ['too short', 'needs a digit'] } };
    expect(describeApiError(err)).toBe('too short needs a digit');
  });
});
