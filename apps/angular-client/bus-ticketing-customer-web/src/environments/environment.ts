export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:8080/api/v1',
  // Demo mode: mockApiInterceptor answers every request in-browser so the
  // app is fully click-through-able without booking-service (or the
  // not-yet-built auth/payment services) running. Flip to false once real
  // backends are reachable at apiBaseUrl.
  mockApi: true
};
