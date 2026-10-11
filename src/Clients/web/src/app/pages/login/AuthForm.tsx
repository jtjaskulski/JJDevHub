import { useState, type FormEvent, type ReactNode } from 'react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

import { describeApiError } from '../../auth/auth';

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
    <section className="mx-auto flex w-full max-w-md flex-col gap-5 px-4 py-8">
      <h1 className="text-h3 font-medium tracking-h3">{title}</h1>
      <form className="flex flex-col gap-4" onSubmit={onFormSubmit}>
        <div className="flex flex-col gap-2">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            name="email"
            type="email"
            value={email}
            required
            autoComplete="username"
            onChange={(event) => setEmailFromInput(event.target.value)}
          />
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="password">Password</Label>
          <Input
            id="password"
            name="password"
            type="password"
            value={password}
            required
            minLength={8}
            autoComplete={passwordAutoComplete}
            onChange={(event) => setPasswordFromInput(event.target.value)}
          />
        </div>
        {error ? (
          <p role="alert" className="text-small text-accent">
            {error}
          </p>
        ) : null}
        <Button type="submit" disabled={pending}>
          {buttonLabel}
        </Button>
      </form>
      <p className="text-small text-muted">{hint}</p>
    </section>
  );
}
