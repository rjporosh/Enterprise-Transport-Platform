/** Central place for env-derived config so no component reaches into import.meta.env directly. */
export const env = {
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:8080/api/v1',
  // Demo mode: the mock axios adapter (src/api/mockAdapter.ts) answers every
  // request in-process so the console is fully click-through-able with no
  // backend running. Set VITE_USE_MOCK_API=false once real services (only
  // booking-service exists today; trips/buses/routes/users/auth are
  // "imagined" API surfaces per the brief) are reachable at apiBaseUrl.
  mockApi: (import.meta.env.VITE_USE_MOCK_API as string | undefined) !== 'false'
};
