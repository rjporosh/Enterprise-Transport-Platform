import { httpClient } from '../../../api/httpClient';
import { AuthResponse, LoginRequest } from '../models/auth.model';

export const authApi = {
  login: async (payload: LoginRequest): Promise<AuthResponse> => {
    const { data } = await httpClient.post<AuthResponse>('/auth/login', payload);
    return data;
  }
};
