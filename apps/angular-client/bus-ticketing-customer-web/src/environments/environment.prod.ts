export const environment = {
  production: true,
  // Relative — the production container's nginx.conf proxies /api/v1/* to the
  // ONE platform API gateway (service name `api-gateway`, see
  // infrastructure/docker/docker-compose.yml). No internal service URLs are
  // exposed to the browser.
  apiBaseUrl: '/api/v1',
  mockApi: false
};
