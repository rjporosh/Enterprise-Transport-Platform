import { Routes } from '@angular/router';

export const BOOKING_ROUTES: Routes = [
  {
    path: ':tripId/seats',
    loadComponent: () =>
      import('./pages/seat-selection-page/seat-selection-page.component').then((m) => m.SeatSelectionPageComponent)
  },
  {
    path: ':bookingId/confirmation',
    loadComponent: () =>
      import('./pages/booking-confirmation-page/booking-confirmation-page.component').then(
        (m) => m.BookingConfirmationPageComponent
      )
  }
];
