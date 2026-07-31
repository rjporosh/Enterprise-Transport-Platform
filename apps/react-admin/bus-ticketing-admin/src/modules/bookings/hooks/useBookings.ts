import { useQuery } from '@tanstack/react-query';
import { bookingsApi } from '../api/bookings.api';

export function useBookings(params: { page?: number; pageSize?: number; status?: string } = {}) {
  return useQuery({
    queryKey: ['bookings', params],
    queryFn: () => bookingsApi.list(params)
  });
}
