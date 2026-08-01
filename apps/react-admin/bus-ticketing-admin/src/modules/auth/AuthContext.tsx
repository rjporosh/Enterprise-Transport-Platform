import { createContext, ReactNode, useCallback, useContext, useMemo, useState } from 'react';
import { authApi } from './api/auth.api';
import { AdminAuthUser, LoginRequest } from './models/auth.model';

const TOKEN_KEY = 'admin_access_token';
const USER_KEY = 'admin_auth_user';

interface AuthContextValue {
  user: AdminAuthUser | null;
  isAuthenticated: boolean;
  submitting: boolean;
  error: string | null;
  login: (payload: LoginRequest) => Promise<boolean>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function restoreUser(): AdminAuthUser | null {
  const raw = sessionStorage.getItem(USER_KEY);
  return raw ? (JSON.parse(raw) as AdminAuthUser) : null;
}

/** Wrap the app root once (main.tsx) — every module reads auth state via useAuth(). */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AdminAuthUser | null>(restoreUser);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const login = useCallback(async (payload: LoginRequest) => {
    setSubmitting(true);
    setError(null);
    try {
      const response = await authApi.login(payload);
      sessionStorage.setItem(TOKEN_KEY, response.accessToken);
      sessionStorage.setItem(USER_KEY, JSON.stringify(response.user));
      setUser(response.user);
      return true;
    } catch {
      setError('Unable to sign in right now.');
      return false;
    } finally {
      setSubmitting(false);
    }
  }, []);

  const logout = useCallback(() => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    setUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: user !== null, submitting, error, login, logout }),
    [user, submitting, error, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth() must be used within <AuthProvider>');
  return ctx;
}
