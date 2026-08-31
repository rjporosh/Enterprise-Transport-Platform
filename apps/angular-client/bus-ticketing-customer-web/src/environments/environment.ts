export const environment = {
  production: false,
  // Relative — every API call is proxied by `ng serve` (see proxy.conf.json)
  // to the ONE platform API gateway (YARP), which owns routing to the
  // individual backend services. There are no direct service URLs in this app
  // any more (M0). In the production container, nginx.conf proxies the same
  // /api/v1 prefix to the gateway.
  apiBaseUrl: '/api/v1',
  // mockApiInterceptor is off — this app talks to the real backend via the
  // gateway. Flip to true for a backend-less demo/click-through. Two paths
  // (GET /bookings/mine, POST /payments/{id}/confirm) still fall back to the
  // in-app mock even in real mode because no backend endpoint exists yet —
  // see src/app/core/interceptors/mock-api.interceptor.ts and
  // docs/API-GAPS.md (tracked for milestone M2/M3).
  mockApi: false
};
