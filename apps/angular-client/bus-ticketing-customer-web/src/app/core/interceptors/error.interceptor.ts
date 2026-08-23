import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export interface ApiProblemDetails {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}

/**
 * Normalizes backend ProblemDetails errors into a consistent shape for
 * components to consume. Always logs the real HttpErrorResponse to the
 * console first -- previously any non-ProblemDetails failure (network
 * unreachable, CORS, timeout, a 500 with an HTML error page instead of
 * JSON) collapsed into the same generic string with nothing recorded
 * anywhere, making it impossible to tell those cases apart while debugging.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // eslint-disable-next-line no-console -- intentional: preserve the real error for dev diagnostics
      console.error(`[HTTP ${req.method} ${req.urlWithParams}]`, {
        status: error.status,
        statusText: error.statusText,
        message: error.message,
        error: error.error
      });

      const problem: ApiProblemDetails = error.error?.title
        ? error.error
        : { title: describeStatus(error), status: error.status };

      return throwError(() => problem);
    })
  );

function describeStatus(error: HttpErrorResponse): string {
  // Angular reports status 0 for both "network unreachable" and a CORS
  // rejection -- the browser doesn't expose enough to tell them apart from
  // JS. The console.error above still carries the raw message, which
  // usually does (a CORS rejection also logs a distinct browser warning).
  if (error.status === 0) {
    return 'Could not reach the server. Check your connection, that the service is running, and (if this persists) the browser console for a possible CORS error.';
  }
  if (error.status === 401) return 'Invalid credentials.';
  if (error.status === 403) return 'You do not have permission to do that.';
  if (error.status === 404) return "The requested endpoint was not found (404). This usually means the backend route doesn't exist yet.";
  if (error.status >= 500) return `The server returned an error (${error.status}). Check the service logs for details.`;
  return 'Something went wrong. Please try again.';
}
