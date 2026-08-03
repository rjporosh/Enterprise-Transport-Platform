import { Booking } from '../../shared/types/booking.model';
import { PagedResult, TripSearchResult } from '../../shared/types/trip.model';

/**
 * In-memory fixtures backing the mock API interceptor (see
 * mock-api.interceptor.ts). This is the ONLY real backend today
 * (services/booking-service) plus everything the user asked us to
 * "imagine we have" — auth, payment, profile — so the app runs and demos
 * standalone without any service running. Swap `environment.mockApi` to
 * `false` once the real services are deployed; nothing else in the
 * feature code needs to change since it already calls HttpClient against
 * the real REST shape.
 */

let bookingSeq = 1000;

export const MOCK_OPERATORS = ['Green Line Paribahan', 'Shohagh Paribahan', 'Ena Transport', 'Hanif Enterprise'];
export const MOCK_BUS_TYPES = ['AC Business', 'AC Sleeper', 'Non-AC Deluxe'];
export const MOCK_CITIES = ['Dhaka', 'Chattogram', 'Sylhet', 'Khulna', 'Rajshahi', "Cox's Bazar"];

function hoursFromNow(hours: number): string {
  return new Date(Date.now() + hours * 3_600_000).toISOString();
}

export function generateMockTrips(origin: string, destination: string): TripSearchResult[] {
  return Array.from({ length: 6 }, (_, i) => {
    const totalSeats = 36;
    const availableSeats = Math.max(0, totalSeats - (i * 7 + 3));
    return {
      tripId: `trip-${origin}-${destination}-${i}`.toLowerCase().replace(/\s+/g, '-'),
      originCity: origin,
      destinationCity: destination,
      departureUtc: hoursFromNow(4 + i * 3),
      arrivalUtc: hoursFromNow(4 + i * 3 + 6),
      busType: MOCK_BUS_TYPES[i % MOCK_BUS_TYPES.length],
      operatorPlateNumber: `${MOCK_OPERATORS[i % MOCK_OPERATORS.length]} · DHK-${1200 + i}`,
      pricePerSeat: 850 + i * 120,
      currency: 'BDT',
      availableSeats,
      totalSeats
    };
  });
}

export function mockTripSearchResponse(origin: string, destination: string): PagedResult<TripSearchResult> {
  const items = generateMockTrips(origin, destination);
  return { items, totalCount: items.length, page: 1, pageSize: 20, totalPages: 1 };
}

const bookingsStore = new Map<string, Booking>();

export function createMockBooking(tripId: string, customerId: string, passengers: { seatNumber: string; fullName: string }[], pricePerSeat = 950): Booking {
  const bookingId = `bkg-${++bookingSeq}`;
  const booking: Booking = {
    bookingId,
    tripId,
    customerId,
    status: 'PendingPayment',
    totalAmount: pricePerSeat * passengers.length,
    currency: 'BDT',
    createdAtUtc: new Date().toISOString(),
    holdExpiresAtUtc: hoursFromNow(0.166), // ~10 minutes
    seats: passengers.map((p) => ({ seatNumber: p.seatNumber, passengerFullName: p.fullName }))
  };
  bookingsStore.set(bookingId, booking);
  return booking;
}

export function getMockBooking(bookingId: string): Booking | undefined {
  return bookingsStore.get(bookingId);
}

export function confirmMockBookingPayment(bookingId: string): Booking | undefined {
  const booking = bookingsStore.get(bookingId);
  if (!booking) return undefined;
  booking.status = 'Confirmed';
  bookingsStore.set(bookingId, booking);
  return booking;
}

export function cancelMockBooking(bookingId: string): Booking | undefined {
  const booking = bookingsStore.get(bookingId);
  if (!booking) return undefined;
  booking.status = 'Cancelled';
  bookingsStore.set(bookingId, booking);
  return booking;
}

export function listMockBookingsForCustomer(customerId: string): Booking[] {
  return Array.from(bookingsStore.values())
    .filter((b) => b.customerId === customerId)
    .sort((a, b) => (a.createdAtUtc < b.createdAtUtc ? 1 : -1));
}

// Seed a couple of historical bookings so "My Bookings" isn't empty on first load.
export function seedMockBookings(customerId: string): void {
  if (bookingsStore.size > 0) return;
  const seedTrip = generateMockTrips('Dhaka', 'Chattogram')[0];
  const confirmed = createMockBooking(seedTrip.tripId, customerId, [
    { seatNumber: 'A3', fullName: 'Rafiul Islam' },
    { seatNumber: 'A4', fullName: 'Nusrat Jahan' }
  ], seedTrip.pricePerSeat);
  confirmed.status = 'Confirmed';
  confirmed.createdAtUtc = hoursFromNow(-48);
  bookingsStore.set(confirmed.bookingId, confirmed);
}
