/**
 * Matches auth-service's real contracts (see
 * services/auth-service/src/AuthService.Api/Endpoints/AuthEndpoints.cs)
 * rather than a client-invented convenience shape. Login only returns a
 * token pair -- no name/role-friendly fields beyond email/userId/roles --
 * so AdminAuthUser (what the console renders and route-guards on) is
 * assembled client-side from TokenPairResponse + a follow-up GET
 * /auth/me call. See AuthContext.
 */

export interface AdminAuthUser {
  userId: string;
  fullName: string;
  email: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
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
