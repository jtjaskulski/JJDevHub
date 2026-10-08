import { Navigate, Outlet, useParams } from 'react-router';

import { isLocale } from './content/content';

export function LangGate() {
  const { lang } = useParams();
  if (!isLocale(lang)) {
    return <Navigate to="/pl" replace />;
  }
  return <Outlet />;
}
