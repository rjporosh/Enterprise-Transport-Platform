import axios from 'axios';
import type { ApiProblemDetails } from './httpClient';

/**
 * Classifies a caught error into a short, user-safe message, while always
 * console-logging the real underlying error/response so it's visible in
 * dev tools. Previously several call sites (e.g. admin login) used a bare
 * `catch { setError('Unable to sign in right now.') }`, which discarded
 * the actual cause -- network unreachable vs. CORS vs. 401 vs. 500 vs. a
 * malformed response all looked identical to the user and to the console.
 *
 * This does not change what the UI shows for expected auth failures (still
 * short and generic where that's appropriate, e.g. wrong password) -- it
 * changes what's discoverable while debugging, and gives distinct enough
 * user-facing text that "the network is down" no longer looks identical to
 * "your password is wrong."
 */
export function describeApiError(error: unknown, context: string): string {
  if (axios.isAxiosError(error)) {
    // eslint-disable-next-line no-console -- intentional: preserve the real error for dev diagnostics
    console.error(`[${context}]`, {
      message: error.message,
      code: error.code,
      status: error.response?.status,
      data: error.response?.data,
      url: error.config?.url,
      baseURL: error.config?.baseURL
    });

    if (error.code === 'ECONNABORTED') {
      return 'The request timed out. The service may be slow or unreachable -- check that it is running.';
    }

    if (!error.response) {
      // Axios gives no `response` for both "network unreachable" and CORS
      // rejections -- the browser does not expose enough detail to tell
      // them apart from JS. The console.error above still has the raw
      // message/code, which usually does distinguish them (a CORS
      // rejection logs a distinct browser console warning alongside this).
      return 'Could not reach the server. Check your connection, that the service is running, and (if this persists) the browser console for a possible CORS error.';
    }

    const problem = error.response.data as ApiProblemDetails | undefined;
    const status = error.response.status;

    switch (status) {
      case 400:
        return problem?.title ?? 'The request was invalid.';
      case 401:
        return problem?.title ?? 'Invalid credentials.';
      case 403:
        return problem?.title ?? 'You do not have permission to do that.';
      case 404:
        return `The requested endpoint was not found (404). This usually means the backend route doesn't exist yet.`;
      case 429:
        return 'Too many attempts. Please wait a moment and try again.';
      default:
        if (status >= 500) {
          return `The server returned an error (${status}). Check the service logs for details.`;
        }
        return problem?.title ?? `Request failed (${status}).`;
    }
  }

  // Not an axios error at all -- e.g. JSON.parse failing on a malformed
  // response, or a bug in the calling code. Still log it instead of hiding it.
  // eslint-disable-next-line no-console
  console.error(`[${context}] Non-HTTP error:`, error);
  return 'Something unexpected went wrong. See the browser console for details.';
}
