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
  { path: '**', redirectTo: '' }
];
