import { useMutation, useQueryClient } from '@tanstack/react-query';
import { bookingsApi } from '../api/bookings.api';

export function useCancelBooking() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (vars: { bookingId: string; customerId: string; reason: string }) =>
      bookingsApi.cancel(vars.bookingId, { customerId: vars.customerId, reason: vars.reason }),
    onSuccess: (_data, vars) => {
      queryClient.invalidateQueries({ queryKey: ['booking', vars.bookingId] });
      queryClient.invalidateQueries({ queryKey: ['bookings'] });
    }
  });
}
