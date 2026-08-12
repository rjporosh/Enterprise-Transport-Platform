/** Central place for env-derived config so no component reaches into import.meta.env directly. */
export const env = {
  // Relative — routed by the Vite dev-server proxy (see vite.config.ts) to
  // the correct backend service per path prefix, the same way nginx.conf
  // routes it in the production container. No gateway exists yet
  // (infrastructure/gateway/ is an empty placeholder), so each service is
  // proxied individually.
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '/api/v1',
  // auth-service, booking-service, bus-service and route-service are all
  // implemented and reachable now (see mockAdapter.ts for exactly which
  // paths route to them vs. which two still have no real backend and stay
  // mocked either way). Set VITE_USE_MOCK_API=true for a fully
  // backend-less demo/click-through instead.
  mockApi: (import.meta.env.VITE_USE_MOCK_API as string | undefined) === 'true'
};
