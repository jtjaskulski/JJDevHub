import { Link, useNavigate } from 'react-router';

import { useAuth } from '../../auth/auth';
import { AuthForm } from './AuthForm';

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();

  async function submit(email: string, password: string): Promise<void> {
    await auth.login(email, password);
    void navigate('/', { viewTransition: true });
  }

  return (
    <AuthForm
      title="Log in"
      submitLabel="Log in"
      pendingLabel="Signing in…"
      passwordAutoComplete="current-password"
      hint={
        <>
          No account? <Link to="/register">Register</Link>
        </>
      }
      onSubmit={submit}
    />
  );
}
