import { cssCustomProperties, type ColorScheme } from '@jjdevhub/theme';

export const themeStorageKey = 'jjdevhub.theme';

export function readColorScheme(): ColorScheme {
  const stored = localStorage.getItem(themeStorageKey);
  if (stored === 'light' || stored === 'dark') {
    return stored;
  }
  if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
    return 'dark';
  }
  return 'light';
}

export function applyColorScheme(scheme: ColorScheme): void {
  const root = document.documentElement;
  for (const [name, value] of Object.entries(cssCustomProperties(scheme))) {
    root.style.setProperty(name, value);
  }
  root.style.colorScheme = scheme;
}

export function storeColorScheme(scheme: ColorScheme): void {
  localStorage.setItem(themeStorageKey, scheme);
}
