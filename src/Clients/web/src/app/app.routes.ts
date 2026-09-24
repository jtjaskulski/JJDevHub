import { Routes } from '@angular/router';

import { guestGuard } from './auth/auth.guard';
import { langGuard } from './routing/lang.guard';
import { CompendiumDetailPage } from './pages/compendium/compendium-detail';
import { CompendiumPage } from './pages/compendium/compendium';
import { CourseDetailPage } from './pages/courses/course-detail';
import { CoursesPage } from './pages/courses/courses';
import { CvPage } from './pages/cv/cv';
import { Home } from './pages/home/home';
import { Login } from './pages/login/login';
import { NoteDetailPage } from './pages/notes/note-detail';
import { NotesPage } from './pages/notes/notes';
import { Register } from './pages/register/register';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pl' },
  { path: 'login', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  {
    path: ':lang',
    canActivate: [langGuard],
    children: [
      { path: '', component: Home },
      { path: 'cv', component: CvPage },
      { path: 'courses', component: CoursesPage },
      { path: 'courses/:slug', component: CourseDetailPage },
      { path: 'compendium', component: CompendiumPage },
      { path: 'compendium/:slug', component: CompendiumDetailPage },
      { path: 'notes', component: NotesPage },
      { path: 'notes/:slug', component: NoteDetailPage },
    ],
  },
  { path: '**', redirectTo: 'pl' },
];
