import { httpClient } from '../../../api/httpClient';
import { RouteDef } from '../models/route.model';
import { PagedResult } from '../../trips/api/trips.api';

export const routesApi = {
  list: async (params: { page?: number; pageSize?: number } = {}): Promise<PagedResult<RouteDef>> => {
    const { data } = await httpClient.get<PagedResult<RouteDef>>('/routes', { params });
    return data;
  }
};
