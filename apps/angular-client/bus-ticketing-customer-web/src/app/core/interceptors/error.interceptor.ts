import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export interface ApiProblemDetails {
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}

/** Normalizes backend ProblemDetails errors into a consistent shape for components to consume. */
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem: ApiProblemDetails = error.error?.title
        ? error.error
        : { title: 'Something went wrong. Please try again.', status: error.status };

      return throwError(() => problem);
    })
  );
