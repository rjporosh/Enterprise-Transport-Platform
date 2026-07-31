import { HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { delay, mergeMap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  cancelMockBooking,
  confirmMockBookingPayment,
  createMockBooking,
  getMockBooking,
  listMockBookingsForCustomer,
  mockTripSearchResponse,
  seedMockBookings
} from '../mock/mock-data';
import { AuthResponse, LoginRequest, RegisterRequest } from '../auth/auth.model';

const DEMO_CUSTOMER_ID = '00000000-0000-0000-0000-000000000001';
const LATENCY_MS = 450;

/**
 * Stands in for every backend this app calls until the real services
 * (auth-service, payment-service — booking-service already exists) are
 * deployed. It intercepts requests by URL/method *before* they leave the
 * browser and resolves them from in-memory fixtures, so `ng serve` gives a
 * fully working, click-through demo with zero backend running.
 *
 * Toggle off via `environment.mockApi = false` once real services are live
 * — every feature/service in this app already calls HttpClient against the
 * real REST contract, so nothing else changes.
 */
export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.mockApi || !req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }

  const path = req.url.slice(environment.apiBaseUrl.length);
  seedMockBookings(DEMO_CUSTOMER_ID);

  // --- Auth -----------------------------------------------------------
  if (path === '/auth/login' && req.method === 'POST') {
    const body = req.body as LoginRequest;
    if (!body?.email || !body?.password) {
      return respondError(400, 'Email and password are required.');
    }
    const response: AuthResponse = {
      accessToken: `demo-token-${Date.now()}`,
      user: { customerId: DEMO_CUSTOMER_ID, fullName: body.email.split('@')[0], email: body.email }
    };
    return respond(response);
  }

  if (path === '/auth/register' && req.method === 'POST') {
    const body = req.body as RegisterRequest;
    const response: AuthResponse = {
      accessToken: `demo-token-${Date.now()}`,
      user: { customerId: DEMO_CUSTOMER_ID, fullName: body.fullName, email: body.email }
    };
    return respond(response);
  }

  // --- Trips ------------------------------------------------------------
  if (path.startsWith('/trips/search') && req.method === 'GET') {
    const origin = req.params.get('origin') ?? 'Dhaka';
    const destination = req.params.get('destination') ?? 'Chattogram';
    return respond(mockTripSearchResponse(origin, destination));
  }

  // --- Bookings --------------------------------------------------------
  if (path === '/bookings' && req.method === 'POST') {
    const { tripId, passengers } = req.body as { tripId: string; passengers: { seatNumber: string; fullName: string }[] };
    const booking = createMockBooking(tripId, DEMO_CUSTOMER_ID, passengers);
    return respond(booking, 201);
  }

  if (path === '/bookings/mine' && req.method === 'GET') {
    return respond(listMockBookingsForCustomer(DEMO_CUSTOMER_ID));
  }

  const bookingByIdMatch = path.match(/^\/bookings\/([^/]+)$/);
  if (bookingByIdMatch && req.method === 'GET') {
    const booking = getMockBooking(bookingByIdMatch[1]);
    return booking ? respond(booking) : respondError(404, 'Booking not found.');
  }

  const cancelMatch = path.match(/^\/bookings\/([^/]+)\/cancel$/);
  if (cancelMatch && req.method === 'POST') {
    const booking = cancelMockBooking(cancelMatch[1]);
    return booking ? respond(undefined) : respondError(404, 'Booking not found.');
  }

  // --- Payments ---------------------------------------------------------
  const payMatch = path.match(/^\/payments\/([^/]+)\/confirm$/);
  if (payMatch && req.method === 'POST') {
    const booking = confirmMockBookingPayment(payMatch[1]);
    return booking ? respond(booking) : respondError(404, 'Booking not found.');
  }

  // Unrecognized path under the API base — fall through to a realistic 404
  // rather than silently hitting the network (which has nothing to answer).
  return respondError(404, `No mock handler for ${req.method} ${path}`);
};

function respond<T>(body: T, status = 200) {
  return of(new HttpResponse({ status, body })).pipe(delay(LATENCY_MS));
}

function respondError(status: number, title: string) {
  return of(null).pipe(
    delay(LATENCY_MS),
    mergeMap(() => throwError(() => ({ status, error: { title, status } })))
  );
}
