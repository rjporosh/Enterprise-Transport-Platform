/**
 * Shapes below are intentionally 1:1 with auth-service's real contracts
 * (services/auth-service/src/AuthService.Api/Endpoints/AuthEndpoints.cs),
 * not a client-invented convenience shape. Login/Register return a token
 * pair only -- no profile fields -- so AuthUser (what the rest of the app
 * renders) is assembled client-side from TokenPairResponse + a follow-up
 * GET /auth/me call. See AuthStore.
 */

export interface AuthUser {
  customerId: string;
  fullName: string;
  email: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** Matches AuthService RegisterCommand: split first/last name, no single "fullName" field on the wire. */
export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
}

/** Matches AuthEndpoints.TokenPairResponse exactly (PascalCase C# record serialized camelCase by System.Text.Json Web defaults). */
export interface TokenPairResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  userId: string;
  email: string;
  roles: string[];
}

/** Matches GetCurrentUser's UserDto (GET /auth/me). */
export interface CurrentUserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  isEmailVerified: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
  roles: string[];
}
