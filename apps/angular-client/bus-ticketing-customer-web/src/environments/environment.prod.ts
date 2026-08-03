export const environment = {
  production: true,
  apiBaseUrl: '/api/v1',
  // Kept true here too so the production build served for demo/interview
  // purposes also works with no backend deployed. Set to false for a real
  // production deployment once services are live behind apiBaseUrl.
  mockApi: true
};
