import { Routes } from '@angular/router';

export const SEARCH_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/trip-search-page/trip-search-page.component').then((m) => m.TripSearchPageComponent)
  }
];
