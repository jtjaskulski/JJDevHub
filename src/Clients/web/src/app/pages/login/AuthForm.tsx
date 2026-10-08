import { useState, type FormEvent, type ReactNode } from 'react';

import { describeApiError } from '../../auth/auth';
import './login.scss';

type AuthFormProps = {
  title: string;
  submitLabel: string;
  pendingLabel: string;
  passwordAutoComplete: 'current-password' | 'new-password';
  hint: ReactNode;
  onSubmit: (email: string, password: string) => Promise<void>;
};

export function AuthForm({
  title,
  submitLabel,
  pendingLabel,
  passwordAutoComplete,
  hint,
  onSubmit,
}: AuthFormProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  function setEmailFromInput(value: string): void {
    setEmail(value);
  }

  function setPasswordFromInput(value: string): void {
    setPassword(value);
  }

  async function submit(event: FormEvent): Promise<void> {
    event.preventDefault();
    setPending(true);
    setError(null);
    try {
      await onSubmit(email, password);
    } catch (err) {
      setPending(false);
      setError(describeApiError(err));
    }
  }

  function onFormSubmit(event: FormEvent): void {
    void submit(event);
  }

  const buttonLabel = pending ? pendingLabel : submitLabel;

  return (
    <section className="auth-page card">
      <h2>{title}</h2>
      <form onSubmit={onFormSubmit}>
        <label>
          Email
          <input
            name="email"
            type="email"
            value={email}
            required
            autoComplete="username"
            onChange={(event) => setEmailFromInput(event.target.value)}
          />
        </label>
        <label>
          Password
          <input
            name="password"
            type="password"
            value={password}
            required
            minLength={8}
            autoComplete={passwordAutoComplete}
            onChange={(event) => setPasswordFromInput(event.target.value)}
          />
        </label>
        {error ? <p className="error">{error}</p> : null}
        <button type="submit" disabled={pending}>
          {buttonLabel}
        </button>
      </form>
      <p className="hint">{hint}</p>
    </section>
  );
}
