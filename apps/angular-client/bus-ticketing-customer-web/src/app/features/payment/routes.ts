import { Routes } from '@angular/router';

export const PAYMENT_ROUTES: Routes = [
  {
    path: ':bookingId',
    loadComponent: () => import('./pages/payment-page/payment-page.component').then((m) => m.PaymentPageComponent)
  }
];
