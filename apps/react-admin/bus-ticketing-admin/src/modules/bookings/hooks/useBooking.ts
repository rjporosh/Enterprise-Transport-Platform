import { useQuery } from '@tanstack/react-query';
import { bookingsApi } from '../api/bookings.api';

export function useBooking(bookingId: string | undefined) {
  return useQuery({
    queryKey: ['booking', bookingId],
    queryFn: () => bookingsApi.getById(bookingId!),
    enabled: Boolean(bookingId)
  });
}
