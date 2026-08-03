import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./features/search/routes').then((m) => m.SEARCH_ROUTES)
  },
  {
    path: 'book',
    loadChildren: () => import('./features/booking/routes').then((m) => m.BOOKING_ROUTES)
  },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/routes').then((m) => m.AUTH_ROUTES)
  },
  {
    path: 'profile',
    loadChildren: () => import('./features/profile/routes').then((m) => m.PROFILE_ROUTES)
  },
  {
    path: 'payment',
    loadChildren: () => import('./features/payment/routes').then((m) => m.PAYMENT_ROUTES)
  },
  {
    path: 'not-found',
    loadComponent: () => import('./shared/pages/not-found-page/not-found-page.component').then((m) => m.NotFoundPageComponent)
  },
  { path: '**', redirectTo: 'not-found' }
];
