import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { mockApiInterceptor } from './core/interceptors/mock-api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding()),
    // Order matters: auth attaches the token, error normalizes failures for
    // every interceptor upstream of it, and mockApiInterceptor sits last —
    // "closest to the backend" — so its responses/errors still flow back
    // through errorInterceptor's normalization. Drop mockApiInterceptor
    // once the real services are deployed (see its doc comment).
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, mockApiInterceptor]))
  ]
};
