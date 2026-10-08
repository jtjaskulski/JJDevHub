import { Navigate, Route, Routes } from 'react-router';

import { GuestRoute } from './GuestRoute';
import { LangGate } from './LangGate';
import { Shell } from './Shell';
import { CompendiumDetailPage } from './pages/compendium/CompendiumDetailPage';
import { CompendiumPage } from './pages/compendium/CompendiumPage';
import { CourseDetailPage } from './pages/courses/CourseDetailPage';
import { CoursesPage } from './pages/courses/CoursesPage';
import { CvPage } from './pages/cv/CvPage';
import { HomePage } from './pages/home/HomePage';
import { LoginPage } from './pages/login/LoginPage';
import { NoteDetailPage } from './pages/notes/NoteDetailPage';
import { NotesPage } from './pages/notes/NotesPage';
import { RegisterPage } from './pages/register/RegisterPage';

export function App() {
  return (
    <Shell>
      <Routes>
        <Route path="/" element={<Navigate to="/pl" replace />} />
        <Route
          path="/login"
          element={
            <GuestRoute>
              <LoginPage />
            </GuestRoute>
          }
        />
        <Route
          path="/register"
          element={
            <GuestRoute>
              <RegisterPage />
            </GuestRoute>
          }
        />
        <Route path="/:lang" element={<LangGate />}>
          <Route index element={<HomePage />} />
          <Route path="cv" element={<CvPage />} />
          <Route path="courses" element={<CoursesPage />} />
          <Route path="courses/:slug" element={<CourseDetailPage />} />
          <Route path="compendium" element={<CompendiumPage />} />
          <Route path="compendium/:slug" element={<CompendiumDetailPage />} />
          <Route path="notes" element={<NotesPage />} />
          <Route path="notes/:slug" element={<NoteDetailPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/pl" replace />} />
      </Routes>
    </Shell>
  );
}
