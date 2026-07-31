import { httpClient } from '../../../api/httpClient';
import { Booking } from '../../bookings/models/booking.model';

export interface DashboardStats {
  totalBookings: number;
  totalRevenue: number;
  activeTrips: number;
  pendingPayments: number;
  recentBookings: Booking[];
}

export const dashboardApi = {
  stats: async (): Promise<DashboardStats> => {
    const { data } = await httpClient.get<DashboardStats>('/dashboard/stats');
    return data;
  }
};
