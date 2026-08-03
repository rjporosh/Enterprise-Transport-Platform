import { Booking, BookingSeat, BookingStatus } from '../../modules/bookings/models/booking.model';

/**
 * In-memory fixtures backing the mock axios adapter (see
 * src/api/mockAdapter.ts). booking-service is the only real backend today —
 * everything else here is the "imagined" API surface for trips, buses,
 * routes, users, auth and dashboard stats, shaped the way those services'
 * real REST contracts are expected to look so swapping the adapter for a
 * real HTTP call later is a non-event for the module code.
 */

export interface Trip {
  tripId: string;
  routeName: string;
  originCity: string;
  destinationCity: string;
  departureUtc: string;
  arrivalUtc: string;
  busPlateNumber: string;
  operator: string;
  status: 'Scheduled' | 'InTransit' | 'Completed' | 'Cancelled';
  totalSeats: number;
  availableSeats: number;
  pricePerSeat: number;
}

export interface Bus {
  busId: string;
  plateNumber: string;
  operator: string;
  busType: 'AC Business' | 'AC Sleeper' | 'Non-AC Deluxe';
  capacity: number;
  status: 'Active' | 'Maintenance' | 'Suspended';
}

export interface RouteDef {
  routeId: string;
  name: string;
  originCity: string;
  destinationCity: string;
  distanceKm: number;
  estimatedDurationMinutes: number;
  activeTrips: number;
}

export interface AdminUser {
  userId: string;
  fullName: string;
  email: string;
  role: 'Admin' | 'OperationsManager' | 'SupportAgent';
  status: 'Active' | 'Suspended';
  lastLoginUtc: string | null;
}

const OPERATORS = ['Green Line Paribahan', 'Shohagh Paribahan', 'Ena Transport', 'Hanif Enterprise'];
const CITY_PAIRS: [string, string][] = [
  ['Dhaka', 'Chattogram'],
  ['Dhaka', 'Sylhet'],
  ['Dhaka', "Cox's Bazar"],
  ['Chattogram', 'Khulna'],
  ['Dhaka', 'Rajshahi']
];

function hoursFromNow(hours: number): string {
  return new Date(Date.now() + hours * 3_600_000).toISOString();
}

export const MOCK_ROUTES: RouteDef[] = CITY_PAIRS.map(([origin, destination], i) => ({
  routeId: `route-${i + 1}`,
  name: `${origin} \u2192 ${destination}`,
  originCity: origin,
  destinationCity: destination,
  distanceKm: 180 + i * 90,
  estimatedDurationMinutes: 300 + i * 60,
  activeTrips: 4 + i
}));

export const MOCK_BUSES: Bus[] = Array.from({ length: 12 }, (_, i) => ({
  busId: `bus-${i + 1}`,
  plateNumber: `DHK-${1200 + i}`,
  operator: OPERATORS[i % OPERATORS.length],
  busType: (['AC Business', 'AC Sleeper', 'Non-AC Deluxe'] as const)[i % 3],
  capacity: [36, 30, 40][i % 3],
  status: i % 9 === 0 ? 'Maintenance' : i % 11 === 0 ? 'Suspended' : 'Active'
}));

export const MOCK_TRIPS: Trip[] = Array.from({ length: 24 }, (_, i) => {
  const [origin, destination] = CITY_PAIRS[i % CITY_PAIRS.length];
  const bus = MOCK_BUSES[i % MOCK_BUSES.length];
  const totalSeats = bus.capacity;
  const availableSeats = Math.max(0, totalSeats - ((i * 5) % totalSeats));
  return {
    tripId: `trip-${i + 1}`,
    routeName: `${origin} \u2192 ${destination}`,
    originCity: origin,
    destinationCity: destination,
    departureUtc: hoursFromNow(i * 3 - 12),
    arrivalUtc: hoursFromNow(i * 3 - 6),
    busPlateNumber: bus.plateNumber,
    operator: bus.operator,
    status: i < 4 ? 'Completed' : i < 6 ? 'InTransit' : i === 23 ? 'Cancelled' : 'Scheduled',
    totalSeats,
    availableSeats,
    pricePerSeat: 850 + (i % 6) * 120
  };
});

export const MOCK_USERS: AdminUser[] = [
  { userId: 'usr-1', fullName: 'Ariful Haque', email: 'ariful@transport.local', role: 'Admin', status: 'Active', lastLoginUtc: hoursFromNow(-2) },
  { userId: 'usr-2', fullName: 'Farzana Akter', email: 'farzana@transport.local', role: 'OperationsManager', status: 'Active', lastLoginUtc: hoursFromNow(-18) },
  { userId: 'usr-3', fullName: 'Tanvir Ahmed', email: 'tanvir@transport.local', role: 'SupportAgent', status: 'Active', lastLoginUtc: hoursFromNow(-40) },
  { userId: 'usr-4', fullName: 'Sabrina Kabir', email: 'sabrina@transport.local', role: 'SupportAgent', status: 'Suspended', lastLoginUtc: hoursFromNow(-720) }
];

const STATUSES: BookingStatus[] = ['Confirmed', 'Confirmed', 'PendingPayment', 'Cancelled', 'Confirmed', 'Refunded'];

function makeSeats(count: number): BookingSeat[] {
  return Array.from({ length: count }, (_, i) => ({
    seatNumber: `${String.fromCharCode(65 + (i % 4))}${i + 1}`,
    passengerFullName: ['Rafiul Islam', 'Nusrat Jahan', 'Kamal Hossain', 'Sultana Parvin'][i % 4]
  }));
}

export const MOCK_BOOKINGS: Booking[] = Array.from({ length: 47 }, (_, i) => {
  const trip = MOCK_TRIPS[i % MOCK_TRIPS.length];
  const seatCount = 1 + (i % 3);
  return {
    bookingId: `bkg-${1000 + i}`,
    tripId: trip.tripId,
    customerId: `cus-${(i % 15) + 1}`,
    status: STATUSES[i % STATUSES.length],
    totalAmount: trip.pricePerSeat * seatCount,
    currency: 'BDT',
    createdAtUtc: hoursFromNow(-(i * 4)),
    holdExpiresAtUtc: hoursFromNow(-(i * 4) + 0.166),
    seats: makeSeats(seatCount)
  };
});

export function getMockBookingById(bookingId: string): Booking | undefined {
  return MOCK_BOOKINGS.find((b) => b.bookingId === bookingId);
}

export function cancelMockBooking(bookingId: string): void {
  const booking = MOCK_BOOKINGS.find((b) => b.bookingId === bookingId);
  if (booking) booking.status = 'Cancelled';
}

export function dashboardStats() {
  const totalRevenue = MOCK_BOOKINGS.filter((b) => b.status === 'Confirmed').reduce((sum, b) => sum + b.totalAmount, 0);
  const activeTrips = MOCK_TRIPS.filter((t) => t.status === 'InTransit' || t.status === 'Scheduled').length;
  const pendingPayments = MOCK_BOOKINGS.filter((b) => b.status === 'PendingPayment').length;

  return {
    totalBookings: MOCK_BOOKINGS.length,
    totalRevenue,
    activeTrips,
    pendingPayments,
    recentBookings: [...MOCK_BOOKINGS].sort((a, b) => (a.createdAtUtc < b.createdAtUtc ? 1 : -1)).slice(0, 6)
  };
}
