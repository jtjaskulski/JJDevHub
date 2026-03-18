/** Komunikaty pod kody z Content.Api (i18n bez zależności od ngx-translate). */
const messages: Record<string, { en: string; pl: string }> = {
  'VALIDATION.FAILED': {
    en: 'Validation failed. Check the form.',
    pl: 'Walidacja nie powiodła się. Sprawdź formularz.',
  },
  'CONTENT.WORK_EXPERIENCE.CONCURRENCY_MISMATCH': {
    en: 'This entry was changed elsewhere. Refresh the page.',
    pl: 'Ktoś zmienił ten wpis. Odśwież stronę.',
  },
  'CONTENT.CONCURRENCY_CONFLICT': {
    en: 'Conflict saving data. Refresh and try again.',
    pl: 'Konflikt zapisu. Odśwież i spróbuj ponownie.',
  },
  'CONTENT.NOT_FOUND': {
    en: 'Resource not found.',
    pl: 'Nie znaleziono zasobu.',
  },
  'SERVER.INTERNAL_ERROR': {
    en: 'Server error. Try again later.',
    pl: 'Błąd serwera. Spróbuj później.',
  },
};

export function resolveApiErrorMessage(code: string | undefined, fallback: string): string {
  if (!code || !messages[code]) return fallback;
  const lang = typeof navigator !== 'undefined' && navigator.language.toLowerCase().startsWith('pl') ? 'pl' : 'en';
  return messages[code][lang];
}

export function resolveApiErrorFromHttp(err: unknown, fallback: string): string {
  if (err && typeof err === 'object' && 'error' in err) {
    const body = (err as { error?: { code?: string; message?: string } }).error;
    if (body?.code) return resolveApiErrorMessage(body.code, body.message ?? fallback);
  }
  return fallback;
}
