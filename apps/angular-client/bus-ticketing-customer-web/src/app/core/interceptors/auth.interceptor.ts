import { HttpInterceptorFn } from '@angular/common/http';

/**
 * Attaches the bearer token (if present) to every outgoing API request.
 * Kept deliberately dumb here — token refresh/rotation belongs to the Auth
 * feature once it's built; this just reads whatever's currently stored.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = sessionStorage.getItem('access_token');

  if (!token || !req.url.includes('/api/')) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    })
  );
};
