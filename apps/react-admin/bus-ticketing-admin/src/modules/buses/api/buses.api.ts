import { httpClient } from '../../../api/httpClient';
import { Bus } from '../models/bus.model';
import { PagedResult } from '../../trips/api/trips.api';

export const busesApi = {
  list: async (params: { page?: number; pageSize?: number } = {}): Promise<PagedResult<Bus>> => {
    const { data } = await httpClient.get<PagedResult<Bus>>('/buses', { params });
    return data;
  }
};
