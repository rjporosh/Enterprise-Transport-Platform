import { FormEvent, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../AuthContext';
import { Button, Card, Input } from '@shared-ui/react';
import { env } from '../../../config/env';

export default function LoginPage() {
  const { login, submitting, error } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState(env.mockApi ? 'admin@transport.local' : '');
  const [password, setPassword] = useState(env.mockApi ? 'password123' : '');

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    const ok = await login({ email, password });
    if (ok) {
      const redirectTo = (location.state as { redirectTo?: string } | null)?.redirectTo ?? '/dashboard';
      navigate(redirectTo, { replace: true });
    }
  }

  return (
    <main className="min-h-screen bg-ink-950 flex items-center justify-center px-6 py-16">
      <div className="w-full max-w-sm">
        <p className="text-saffron-500 text-sm tracking-[0.2em] uppercase mb-2 text-center">Bus Ticketing</p>
        <h1 className="font-display text-3xl text-white text-center mb-8">Admin console</h1>

        <Card>
          <form onSubmit={onSubmit} className="flex flex-col gap-4">
            <Input
              label="Email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@transport.local"
            />
            <Input
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
            />

            {error && <p className="text-danger text-sm bg-danger-bg rounded-md px-3 py-2">{error}</p>}

            <Button type="submit" loading={submitting} className="w-full mt-1">
              Sign in
            </Button>
          </form>
        </Card>

        {env.mockApi && (
          <p className="text-white/30 text-xs text-center mt-4">Demo mode — any email/password combination signs you in.</p>
        )}
      </div>
    </main>
  );
}
