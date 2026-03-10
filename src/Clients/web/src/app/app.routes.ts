import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home.page').then((m) => m.HomePage),
  },
  {
    path: 'work-experience',
    loadComponent: () =>
      import('./pages/work-experience/work-experience.page').then((m) => m.WorkExperiencePage),
  },
  {
    path: 'about',
    loadComponent: () => import('./pages/about/about.page').then((m) => m.AboutPage),
  },
];
