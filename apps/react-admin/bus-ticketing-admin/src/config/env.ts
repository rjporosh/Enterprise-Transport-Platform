/** Central place for env-derived config so no component reaches into import.meta.env directly. */
export const env = {
  // Relative — every API call is routed by the Vite dev proxy (vite.config.ts)
  // or the production nginx to the ONE platform API gateway (YARP), which owns
  // routing to the individual backend services. There are no direct service
  // URLs in this app any more (M0).
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '/api/v1',
  // With the gateway + services running this app talks to the real backend.
  // A few list/aggregation screens still fall back to the in-app mock even in
  // real mode because no backend endpoint exists yet — see mockAdapter.ts and
  // docs/API-GAPS.md. Set VITE_USE_MOCK_API=true for a fully backend-less
  // click-through.
  mockApi: (import.meta.env.VITE_USE_MOCK_API as string | undefined) === 'true'
};
