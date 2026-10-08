import { Link, useNavigate } from 'react-router';

import { useAuth } from '../../auth/auth';
import { AuthForm } from '../login/AuthForm';

export function RegisterPage() {
  const auth = useAuth();
  const navigate = useNavigate();

  async function submit(email: string, password: string): Promise<void> {
    await auth.register(email, password);
    void navigate('/', { viewTransition: true });
  }

  return (
    <AuthForm
      title="Register"
      submitLabel="Create account"
      pendingLabel="Creating account…"
      passwordAutoComplete="new-password"
      hint={
        <>
          Already registered? <Link to="/login">Log in</Link>
        </>
      }
      onSubmit={submit}
    />
  );
}
