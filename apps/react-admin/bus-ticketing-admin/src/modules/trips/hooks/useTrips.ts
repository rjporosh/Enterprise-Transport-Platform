import { useQuery } from '@tanstack/react-query';
import { tripsApi } from '../api/trips.api';

export function useTrips(params: { page?: number; pageSize?: number; status?: string; q?: string } = {}) {
  return useQuery({ queryKey: ['trips', params], queryFn: () => tripsApi.list(params) });
}
