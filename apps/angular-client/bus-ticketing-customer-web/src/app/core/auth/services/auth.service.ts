import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CurrentUserResponse, LoginRequest, RegisterRequest, TokenPairResponse } from '../auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  login(request: LoginRequest): Observable<TokenPairResponse> {
    return this.http.post<TokenPairResponse>(`${this.baseUrl}/login`, request);
  }

  register(request: RegisterRequest): Observable<TokenPairResponse> {
    return this.http.post<TokenPairResponse>(`${this.baseUrl}/register`, request);
  }

  /** Login/Register return tokens only (no profile fields) -- this fills in the rest for AuthStore. */
  getCurrentUser(): Observable<CurrentUserResponse> {
    return this.http.get<CurrentUserResponse>(`${this.baseUrl}/me`);
  }
}
