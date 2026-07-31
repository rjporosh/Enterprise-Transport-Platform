import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from './services/auth.service';
import { AuthUser, LoginRequest, RegisterRequest } from './auth.model';
import { ApiProblemDetails } from '../interceptors/error.interceptor';

const TOKEN_KEY = 'access_token';
const USER_KEY = 'auth_user';

/**
 * Signal-based auth store, same pattern as SearchStore/BookingStore —
 * session persisted to sessionStorage so a page refresh doesn't log the
 * demo user out mid-flow.
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
      const response = await firstValueFrom(call());
      sessionStorage.setItem(TOKEN_KEY, response.accessToken);
      sessionStorage.setItem(USER_KEY, JSON.stringify(response.user));
      this._user.set(response.user);
      return true;
    } catch (err) {
      const problem = err as ApiProblemDetails;
      this._error.set(problem?.title ?? 'Unable to sign in right now.');
      return false;
    } finally {
      this._submitting.set(false);
    }
  }

  logout(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    this._user.set(null);
  }
}
