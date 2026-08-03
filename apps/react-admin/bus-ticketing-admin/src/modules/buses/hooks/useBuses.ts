import { useQuery } from '@tanstack/react-query';
import { busesApi } from '../api/buses.api';

export function useBuses(params: { page?: number; pageSize?: number } = {}) {
  return useQuery({ queryKey: ['buses', params], queryFn: () => busesApi.list(params) });
}
