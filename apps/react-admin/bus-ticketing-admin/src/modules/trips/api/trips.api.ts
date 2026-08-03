import { httpClient } from '../../../api/httpClient';
import { Trip } from '../models/trip.model';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export const tripsApi = {
  list: async (params: { page?: number; pageSize?: number; status?: string; q?: string } = {}): Promise<PagedResult<Trip>> => {
    const { data } = await httpClient.get<PagedResult<Trip>>('/trips', { params });
    return data;
  }
};
