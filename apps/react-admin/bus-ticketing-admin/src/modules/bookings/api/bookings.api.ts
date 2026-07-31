import { httpClient } from '../../../api/httpClient';
import { Booking } from '../models/booking.model';

/**
 * The Booking Service doesn't yet expose a "list all bookings" admin
 * endpoint (see services/booking-service/ROADMAP items for RBAC'd admin
 * queries) — GetBookingById is what exists today. getById is real; list()
 * is written against the endpoint shape the admin console will call once
 * that slice lands, so the UI and hook layer don't need to change later.
 */
export const bookingsApi = {
  getById: async (bookingId: string): Promise<Booking> => {
    const { data } = await httpClient.get<Booking>(`/bookings/${bookingId}`);
    return data;
  },

  list: async (params: { page?: number; pageSize?: number; status?: string } = {}): Promise<{
    items: Booking[];
    totalCount: number;
    page: number;
    pageSize: number;
  }> => {
    const { data } = await httpClient.get('/bookings', { params });
    return data;
  },

  cancel: async (bookingId: string, payload: { customerId: string; reason: string }): Promise<void> => {
    await httpClient.post(`/bookings/${bookingId}/cancel`, payload);
  }
};
