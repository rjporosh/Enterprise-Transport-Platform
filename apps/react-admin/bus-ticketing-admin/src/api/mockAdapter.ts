import type { AxiosAdapter, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { AxiosHeaders } from 'axios';
import {
  MOCK_BUSES,
  MOCK_ROUTES,
  MOCK_TRIPS,
  MOCK_USERS,
  cancelMockBooking,
  dashboardStats,
  getMockBookingById,
  MOCK_BOOKINGS
} from './mock/fixtures';

const LATENCY_MS = 350;
const DEMO_ADMIN = { userId: 'usr-1', fullName: 'Ariful Haque', email: 'admin@transport.local', role: 'Admin' as const };

function paginate<T>(items: T[], page = 1, pageSize = 20) {
  const start = (page - 1) * pageSize;
  return { items: items.slice(start, start + pageSize), totalCount: items.length, page, pageSize };
}

function ok<T>(config: InternalAxiosRequestConfig, data: T, status = 200): Promise<AxiosResponse<T>> {
  return delay(() => ({
    data,
    status,
    statusText: status === 201 ? 'Created' : 'OK',
    headers: new AxiosHeaders(),
    config,
    request: {}
  }));
}

function fail(config: InternalAxiosRequestConfig, status: number, title: string): Promise<AxiosResponse> {
  return delay(() => {
    // Reject in the same shape axios itself uses so callers' existing
    // `error.response.data` / interceptor.ts handling keeps working
    // unchanged whether the mock or a real backend answers.
    const error = new Error(title) as Error & { response: AxiosResponse; isAxiosError: true; config: InternalAxiosRequestConfig };
    error.isAxiosError = true;
    error.config = config;
    error.response = {
      data: { title, status },
      status,
      statusText: title,
      headers: new AxiosHeaders(),
      config,
      request: {}
    };
    throw error;
  });
}

function delay<T>(factory: () => T): Promise<T> {
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      try {
        resolve(factory());
      } catch (err) {
        reject(err);
      }
    }, LATENCY_MS);
  });
}

/**
 * Stands in for auth-service, trip-service, fleet-service, route-service and
 * the admin surface of booking-service until they're deployed — everything
 * this app calls that isn't real booking-service today. Assigned as the
 * axios instance's `adapter` (see httpClient.ts), so every module's
 * `*.api.ts` file already calls the real REST shape; flipping
 * `VITE_USE_MOCK_API=false` is the only change needed once real services
 * exist.
 */
export const mockAdapter: AxiosAdapter = async (config) => {
  const method = (config.method ?? 'get').toUpperCase();
  const url = new URL(config.url ?? '', 'http://mock.local');
  const path = url.pathname;
  const params = { ...Object.fromEntries(url.searchParams.entries()), ...(config.params ?? {}) };
  const page = Number(params.page ?? 1);
  const pageSize = Number(params.pageSize ?? 20);

  // --- Auth --------------------------------------------------------------
  if (path === '/auth/login' && method === 'POST') {
    const body = JSON.parse(config.data ?? '{}');
    if (!body.email || !body.password) return fail(config, 400, 'Email and password are required.');
    return ok(config, { accessToken: `demo-admin-token-${Date.now()}`, user: DEMO_ADMIN });
  }

  // --- Dashboard -----------------------------------------------------------
  if (path === '/dashboard/stats' && method === 'GET') {
    return ok(config, dashboardStats());
  }

  // --- Bookings (admin surface) -------------------------------------------
  if (path === '/bookings' && method === 'GET') {
    const filtered = params.status ? MOCK_BOOKINGS.filter((b) => b.status === params.status) : MOCK_BOOKINGS;
    return ok(config, paginate(filtered, page, pageSize));
  }
  const bookingMatch = path.match(/^\/bookings\/([^/]+)$/);
  if (bookingMatch && method === 'GET') {
    const booking = getMockBookingById(bookingMatch[1]);
    return booking ? ok(config, booking) : fail(config, 404, 'Booking not found.');
  }
  const cancelMatch = path.match(/^\/bookings\/([^/]+)\/cancel$/);
  if (cancelMatch && method === 'POST') {
    if (!getMockBookingById(cancelMatch[1])) return fail(config, 404, 'Booking not found.');
    cancelMockBooking(cancelMatch[1]);
    return ok(config, undefined);
  }

  // --- Trips ---------------------------------------------------------------
  if (path === '/trips' && method === 'GET') {
    let items = MOCK_TRIPS;
    if (params.status) items = items.filter((t) => t.status === params.status);
    if (params.q) items = items.filter((t) => t.routeName.toLowerCase().includes(String(params.q).toLowerCase()));
    return ok(config, paginate(items, page, pageSize));
  }

  // --- Buses -----------------------------------------------------------------
  if (path === '/buses' && method === 'GET') {
    return ok(config, paginate(MOCK_BUSES, page, pageSize));
  }

  // --- Routes ------------------------------------------------------------
  if (path === '/routes' && method === 'GET') {
    return ok(config, paginate(MOCK_ROUTES, page, pageSize));
  }

  // --- Users -----------------------------------------------------------------
  if (path === '/users' && method === 'GET') {
    return ok(config, paginate(MOCK_USERS, page, pageSize));
  }

  return fail(config, 404, `No mock handler for ${method} ${path}`);
};
