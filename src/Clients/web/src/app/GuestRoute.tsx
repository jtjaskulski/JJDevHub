import type { ReactNode } from 'react';
import { Navigate } from 'react-router';

import { useAuth } from './auth/auth';

type GuestRouteProps = {
  children: ReactNode;
};

export function GuestRoute({ children }: GuestRouteProps) {
  const { isAuthenticated } = useAuth();
  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }
  return children;
}
