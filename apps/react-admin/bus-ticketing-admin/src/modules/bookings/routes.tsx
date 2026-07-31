import { RouteObject } from 'react-router-dom';
import BookingsListPage from './pages/BookingsListPage';
import BookingDetailPage from './pages/BookingDetailPage';

export const bookingsRoutes: RouteObject[] = [
  { path: 'bookings', element: <BookingsListPage /> },
  { path: 'bookings/:bookingId', element: <BookingDetailPage /> }
];
