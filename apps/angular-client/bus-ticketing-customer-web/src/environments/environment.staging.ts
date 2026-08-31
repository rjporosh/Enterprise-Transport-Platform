export const environment = {
  production: true,
  // Staging build. Same single-gateway model as production — the staging
  // container's nginx proxies /api/v1/* to the staging API gateway. Kept as a
  // relative path so the deployed origin (staging domain) is authoritative and
  // no internal URL is baked into the bundle.
  apiBaseUrl: '/api/v1',
  mockApi: false
};
