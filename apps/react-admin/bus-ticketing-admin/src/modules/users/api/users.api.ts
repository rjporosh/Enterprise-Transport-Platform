import { httpClient } from '../../../api/httpClient';
import { AdminUser } from '../models/user.model';
import { PagedResult } from '../../trips/api/trips.api';

export const usersApi = {
  list: async (params: { page?: number; pageSize?: number } = {}): Promise<PagedResult<AdminUser>> => {
    const { data } = await httpClient.get<PagedResult<AdminUser>>('/users', { params });
    return data;
  }
};
