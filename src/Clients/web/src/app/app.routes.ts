import { Routes } from '@angular/router';
import { adminAuthGuard } from './core/admin-auth.guard';

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
  {
    path: 'blog',
    loadComponent: () => import('./pages/blog-list/blog-list.page').then((m) => m.BlogListPage),
  },
  {
    path: 'blog/:slug',
    loadComponent: () => import('./pages/blog-post/blog-post.page').then((m) => m.BlogPostPage),
  },
  {
    path: 'admin/work-experience',
    canActivate: [adminAuthGuard],
    loadComponent: () =>
      import('./pages/admin-work-experience/admin-work-experience.page').then(
        (m) => m.AdminWorkExperiencePage,
      ),
  },
];
