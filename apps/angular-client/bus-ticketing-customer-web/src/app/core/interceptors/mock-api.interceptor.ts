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
import { CurrentUserResponse, LoginRequest, RegisterRequest, TokenPairResponse } from '../auth/auth.model';

const DEMO_CUSTOMER_ID = '00000000-0000-0000-0000-000000000001';
const LATENCY_MS = 450;
const demoRegisteredNames = new Map<string, { firstName: string; lastName: string }>();

/**
 * Full-mock mode (environment.mockApi = true): stands in for every backend
 * this app calls, resolving requests from in-memory fixtures so `ng serve`
 * gives a fully working, click-through demo with zero backend running.
 * Unchanged — still available for a backend-less demo.
 *
 * Real mode (environment.mockApi = false, the default now that
 * auth-service, booking-service, bus-service, payment-service,
 * route-service and notification-service are all implemented — see
 * infrastructure/docker/docker-compose.yml): every path below except two
 * falls through to `next(req)`, i.e. a real HTTP call proxied to the
 * matching backend (see proxy.conf.json / nginx.conf) — POST /auth/login,
 * /auth/register, GET /trips/search, POST /bookings, GET /bookings/{id}
 * and POST /bookings/{id}/cancel all have a real, contract-matching
 * endpoint now.
 *
 * Two surfaces this app calls have no real match anywhere in the platform,
 * so they keep resolving from the same mock fixtures even in real mode
 * rather than 404ing:
 *   - GET /bookings/mine — booking-service has get-by-id and cancel, but
 *     no "list my bookings" endpoint.
 *   - POST /payments/{bookingId}/confirm — this method's own doc comment
 *     (payment.service.ts) already says it "simulates a hosted
 *     payment-page redirect flow", a placeholder for the real Payment
 *     Service's actual multi-step create/process/confirm-by-paymentId
 *     flow, not a 1:1 match to any single real endpoint.
 * Per "do not create mock APIs or fake data" neither gets an invented
 * backend; they're left exactly as they already worked before this
 * change, until a real matching endpoint exists.
 */
export const mockApiInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiBaseUrl)) {
    return next(req);
  }
  const path = req.url.slice(environment.apiBaseUrl.length);

  if (!environment.mockApi) {
    seedMockBookings(DEMO_CUSTOMER_ID);

    if (path === '/bookings/mine' && req.method === 'GET') {
      return respond(listMockBookingsForCustomer(DEMO_CUSTOMER_ID));
    }
    const payMatch = path.match(/^\/payments\/([^/]+)\/confirm$/);
    if (payMatch && req.method === 'POST') {
      const booking = confirmMockBookingPayment(payMatch[1]);
      return booking ? respond(booking) : respondError(404, 'Booking not found.');
    }

    return next(req);
  }

  seedMockBookings(DEMO_CUSTOMER_ID);

  // --- Auth -----------------------------------------------------------
  // Shapes here mirror the real TokenPairResponse/UserDto (auth.model.ts)
  // so AuthStore's login/register/getCurrentUser flow works identically
  // whether mockApi is on or off.
  if (path === '/auth/login' && req.method === 'POST') {
    const body = req.body as LoginRequest;
    if (!body?.email || !body?.password) {
      return respondError(400, 'Email and password are required.');
    }
    const response: TokenPairResponse = {
      accessToken: `demo-token-${Date.now()}`,
      accessTokenExpiresAtUtc: new Date(Date.now() + 3600_000).toISOString(),
      refreshToken: `demo-refresh-${Date.now()}`,
      refreshTokenExpiresAtUtc: new Date(Date.now() + 30 * 86_400_000).toISOString(),
      userId: DEMO_CUSTOMER_ID,
      email: body.email,
      roles: ['Customer']
    };
    return respond(response);
  }

  if (path === '/auth/register' && req.method === 'POST') {
    const body = req.body as RegisterRequest;
    const response: TokenPairResponse = {
      accessToken: `demo-token-${Date.now()}`,
      accessTokenExpiresAtUtc: new Date(Date.now() + 3600_000).toISOString(),
      refreshToken: `demo-refresh-${Date.now()}`,
      refreshTokenExpiresAtUtc: new Date(Date.now() + 30 * 86_400_000).toISOString(),
      userId: DEMO_CUSTOMER_ID,
      email: body.email,
      roles: ['Customer']
    };
    demoRegisteredNames.set(DEMO_CUSTOMER_ID, { firstName: body.firstName, lastName: body.lastName });
    return respond(response);
  }

  if (path === '/auth/me' && req.method === 'GET') {
    const name = demoRegisteredNames.get(DEMO_CUSTOMER_ID) ?? { firstName: 'Demo', lastName: 'User' };
    const response: CurrentUserResponse = {
      id: DEMO_CUSTOMER_ID,
      email: 'demo@example.com',
      firstName: name.firstName,
      lastName: name.lastName,
      phoneNumber: null,
      isEmailVerified: true,
      createdAtUtc: new Date().toISOString(),
      lastLoginAtUtc: new Date().toISOString(),
      roles: ['Customer']
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
