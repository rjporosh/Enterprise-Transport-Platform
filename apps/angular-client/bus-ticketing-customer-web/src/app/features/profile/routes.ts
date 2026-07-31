import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const PROFILE_ROUTES: Routes = [
  {
    path: 'bookings',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/my-bookings-page/my-bookings-page.component').then((m) => m.MyBookingsPageComponent)
  }
];
