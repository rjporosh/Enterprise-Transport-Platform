export interface AdminAuthUser {
  userId: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'OperationsManager' | 'SupportAgent';
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  user: AdminAuthUser;
}
