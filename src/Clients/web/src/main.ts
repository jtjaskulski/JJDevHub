import { bootstrapApplication } from '@angular/platform-browser';
import { cssCustomProperties } from '@jjdevhub/theme';

import { appConfig } from './app/app.config';
import { App } from './app/app';

const root = document.documentElement;
for (const [name, value] of Object.entries(cssCustomProperties())) {
  root.style.setProperty(name, value);
}

bootstrapApplication(App, appConfig).catch((err) => console.error(err));
