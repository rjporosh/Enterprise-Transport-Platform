export type AdminUserRole = 'Admin' | 'OperationsManager' | 'SupportAgent';
export type AdminUserStatus = 'Active' | 'Suspended';

export interface AdminUser {
  userId: string;
  fullName: string;
  email: string;
  role: AdminUserRole;
  status: AdminUserStatus;
  lastLoginUtc: string | null;
}
