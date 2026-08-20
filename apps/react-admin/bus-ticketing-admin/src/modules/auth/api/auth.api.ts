import { httpClient } from '../../../api/httpClient';
import { CurrentUserResponse, LoginRequest, TokenPairResponse } from '../models/auth.model';

export const authApi = {
  login: async (payload: LoginRequest): Promise<TokenPairResponse> => {
    const { data } = await httpClient.post<TokenPairResponse>('/auth/login', payload);
    return data;
  },

  /** Login returns tokens only (no profile fields) -- this fills in the rest for AuthContext. */
  me: async (): Promise<CurrentUserResponse> => {
    const { data } = await httpClient.get<CurrentUserResponse>('/auth/me');
    return data;
  }
};
