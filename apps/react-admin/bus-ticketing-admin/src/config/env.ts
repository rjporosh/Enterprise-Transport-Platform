/** Central place for env-derived config so no component reaches into import.meta.env directly. */
export const env = {
  apiBaseUrl: (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:8080/api/v1'
};
