export const environment = {
  production: true,
  apiBaseUrl: '/api/v1',
  // All backend services are implemented and routed via nginx.conf in the
  // production container (see infrastructure/docker/docker-compose.yml).
  // Set back to true for a backend-less demo/interview build.
  mockApi: false
};
