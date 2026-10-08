export type ApiError = {
  status: number;
  message?: string;
  errors?: Record<string, string[]>;
  title?: string;
  detail?: string;
};

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

export function isApiError(err: unknown): err is ApiError {
  return isRecord(err) && typeof err.status === 'number';
}

export function describeApiError(err: unknown): string {
  if (!isApiError(err)) {
    return 'Something went wrong.';
  }
  if (err.status === 401) {
    return 'Invalid email or password.';
  }
  const messages = Object.values(err.errors ?? {})
    .flat()
    .filter(Boolean);
  if (messages.length) {
    return messages.join(' ');
  }
  return err.message || err.title || err.detail || 'Request failed.';
}

export function readApiError(status: number, value: unknown): ApiError {
  if (!isRecord(value)) {
    return { status };
  }
  return {
    status,
    message: typeof value.message === 'string' ? value.message : undefined,
    title: typeof value.title === 'string' ? value.title : undefined,
    detail: typeof value.detail === 'string' ? value.detail : undefined,
    errors: readErrors(value.errors),
  };
}

function readErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) {
    return undefined;
  }
  const errors: Record<string, string[]> = {};
  for (const [key, messages] of Object.entries(value)) {
    if (!Array.isArray(messages)) {
      continue;
    }
    const lines = messages.filter((item): item is string => typeof item === 'string');
    if (lines.length > 0) {
      errors[key] = lines;
    }
  }
  return errors;
}

export function readToken(value: unknown): string | undefined {
  if (!isRecord(value) || typeof value.token !== 'string' || value.token.length === 0) {
    return undefined;
  }
  return value.token;
}
