export const environment = {
  production: false,
  // Relative — routed by the Angular CLI dev-server proxy (see
  // proxy.conf.json) to the correct backend service per path prefix, the
  // same way nginx.conf routes it in the production container. No gateway
  // exists yet (see infrastructure/gateway/, currently an empty
  // placeholder), so each service is proxied individually.
  apiBaseUrl: '/api/v1',
  // All of auth-service, booking-service, bus-service, payment-service,
  // route-service and notification-service are now implemented and wired
  // (see infrastructure/docker/docker-compose.yml) — mockApiInterceptor is
  // off so this app talks to them for real. Every feature/service already
  // calls HttpClient against the real REST contract, so nothing else
  // changes; flip back to true for a backend-less demo/click-through.
  mockApi: false
};
