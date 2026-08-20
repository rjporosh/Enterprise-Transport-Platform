import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './services/auth.service';
import { AuthUser, LoginRequest, RegisterRequest, TokenPairResponse } from './auth.model';
import { ApiProblemDetails } from '../interceptors/error.interceptor';

const TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const USER_KEY = 'auth_user';

/**
 * Signal-based auth store, same pattern as SearchStore/BookingStore --
 * session persisted to sessionStorage so a page refresh doesn't log the
 * user out mid-flow.
 *
 * auth-service's /login and /register only return a token pair (see
 * TokenPairResponse in auth.model.ts) -- no name/roles/profile beyond
 * email+userId. So a successful login/register is followed by one GET
 * /auth/me call to get the fields the UI actually renders (first/last
 * name). If that follow-up call fails, the session is not considered
 * established -- we don't half-log someone in with an empty name.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly authService = inject(AuthService);

  private readonly _user = signal<AuthUser | null>(this.restoreUser());
  private readonly _submitting = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly user = this._user.asReadonly();
  readonly submitting = this._submitting.asReadonly();
  readonly error = this._error.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  private restoreUser(): AuthUser | null {
    const raw = sessionStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  }

  async login(request: LoginRequest): Promise<boolean> {
    return this.submit(() => this.authService.login(request));
  }

  async register(request: RegisterRequest): Promise<boolean> {
    return this.submit(() => this.authService.register(request));
  }

  private async submit(call: () => ReturnType<AuthService['login']>): Promise<boolean> {
    this._submitting.set(true);
    this._error.set(null);
    try {
      const tokens: TokenPairResponse = await firstValueFrom(call());
      sessionStorage.setItem(TOKEN_KEY, tokens.accessToken);
      sessionStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);

      // Token is now in sessionStorage, so authInterceptor will attach it
      // to this next call automatically.
      const profile = await firstValueFrom(this.authService.getCurrentUser());
      const user: AuthUser = {
        customerId: profile.id,
        fullName: `${profile.firstName} ${profile.lastName}`.trim(),
        email: profile.email,
        roles: profile.roles
      };

      sessionStorage.setItem(USER_KEY, JSON.stringify(user));
      this._user.set(user);
      return true;
    } catch (err) {
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(REFRESH_TOKEN_KEY);
      const problem = err as ApiProblemDetails;
      this._error.set(problem?.title ?? 'Unable to sign in right now.');
      return false;
    } finally {
      this._submitting.set(false);
    }
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    this._user.set(null);
  }
}
