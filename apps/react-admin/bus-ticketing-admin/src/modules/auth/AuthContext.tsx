import { createContext, ReactNode, useCallback, useContext, useMemo, useState } from 'react';
import { authApi } from './api/auth.api';
import { AdminAuthUser, LoginRequest, TokenPairResponse } from './models/auth.model';

const TOKEN_KEY = 'admin_access_token';
const REFRESH_TOKEN_KEY = 'admin_refresh_token';
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
      // auth-service's /auth/login only returns a token pair (see
      // TokenPairResponse) — no name/role-friendly fields beyond
      // email/userId/roles. One follow-up GET /auth/me call fills in the
      // rest of what the console renders. If that call fails, the session
      // is not considered established.
      const tokens: TokenPairResponse = await authApi.login(payload);
      sessionStorage.setItem(TOKEN_KEY, tokens.accessToken);
      sessionStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);

      const profile = await authApi.me();
      const adminUser: AdminAuthUser = {
        userId: profile.id,
        fullName: `${profile.firstName} ${profile.lastName}`.trim(),
        email: profile.email,
        roles: profile.roles
      };

      sessionStorage.setItem(USER_KEY, JSON.stringify(adminUser));
      setUser(adminUser);

      // The account can sign in with any role — auth-service doesn't
      // reject login by role — but every actual admin write/read under
      // /api/v1/admin/* is [RequireRole("Admin")] server-side, so a
      // non-Admin account will see 401/403s on those screens even though
      // login itself succeeded. Surface that up front instead of letting
      // it show up as a confusing later failure.
      if (!adminUser.roles.includes('Admin')) {
        setError(
          `Signed in, but this account has no "Admin" role (roles: ${adminUser.roles.join(', ') || 'none'}). ` +
            'Admin-only screens will fail until an existing Admin grants this role — see ai-handover.md for how to bootstrap the first Admin.'
        );
      }

      return true;
    } catch {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(REFRESH_TOKEN_KEY);
      setError('Unable to sign in right now.');
      return false;
    } finally {
      setSubmitting(false);
    }
  }, []);

  const logout = useCallback(() => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
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
