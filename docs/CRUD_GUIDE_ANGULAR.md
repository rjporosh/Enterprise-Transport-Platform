# Adding a new feature to the Angular customer app

Worked example: adding a **"My Bookings"** list + detail feature, following
the same pattern as the existing `search` and `booking` features in
`apps/angular-client/bus-ticketing-customer-web/src/app/features/`.

## The pattern this app follows

- One folder per feature under `features/<name>/`, each with its own
  `pages/`, `services/`, and `routes.ts`.
- One signal-based store per feature under `state/<name>/` (see
  `state/search/search.store.ts`, `state/booking/booking.store.ts`) — no
  NgRx; state is shallow enough that Angular signals are simpler and give
  the same "single source of truth, reactive templates" benefit.
- Shared TypeScript types mirroring the backend DTOs live in
  `shared/types/`.
- Routes are lazy-loaded (`loadChildren`/`loadComponent`) and wired into
  `app.routes.ts`.

## Step 1 — Shared type

```typescript
// src/app/shared/types/my-booking.model.ts
export interface MyBookingSummary {
  bookingId: string;
  tripId: string;
  status: string;
  totalAmount: number;
  currency: string;
  createdAtUtc: string;
}
```

## Step 2 — HTTP service

```typescript
// src/app/features/my-bookings/services/my-bookings.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../../environments/environment';
import { MyBookingSummary } from '../../../shared/types/my-booking.model';

@Injectable({ providedIn: 'root' })
export class MyBookingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/bookings`;

  listMine(customerId: string) {
    return this.http.get<{ items: MyBookingSummary[] }>(`${this.baseUrl}?customerId=${customerId}`);
  }
}
```

The `authInterceptor` (`core/interceptors/auth.interceptor.ts`) already
attaches the bearer token to any request under `/api/`, so you don't add
auth headers manually here.

## Step 3 — Signal store

```typescript
// src/app/state/my-bookings/my-bookings.store.ts
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MyBookingsService } from '../../features/my-bookings/services/my-bookings.service';
import { MyBookingSummary } from '../../shared/types/my-booking.model';

@Injectable({ providedIn: 'root' })
export class MyBookingsStore {
  private readonly service = inject(MyBookingsService);

  private readonly _bookings = signal<MyBookingSummary[]>([]);
  private readonly _loading = signal(false);

  readonly bookings = this._bookings.asReadonly();
  readonly loading = this._loading.asReadonly();

  async load(customerId: string): Promise<void> {
    this._loading.set(true);
    try {
      const result = await firstValueFrom(this.service.listMine(customerId));
      this._bookings.set(result.items);
    } finally {
      this._loading.set(false);
    }
  }
}
```

## Step 4 — Page component

Follow `features/search/pages/trip-search-page/` as the template: a
standalone component, `imports: [CommonModule, ...]`, inject the store,
render with `@if`/`@for` control flow (not `*ngIf`/`*ngFor` — this app uses
the new Angular control-flow syntax throughout).

## Step 5 — Routes

```typescript
// src/app/features/my-bookings/routes.ts
import { Routes } from '@angular/router';

export const MY_BOOKINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/my-bookings-page/my-bookings-page.component').then((m) => m.MyBookingsPageComponent)
  }
];
```

```typescript
// src/app/app.routes.ts — add one entry
{
  path: 'my-bookings',
  loadChildren: () => import('./features/my-bookings/routes').then((m) => m.MY_BOOKINGS_ROUTES)
}
```

## Step 6 — Styling

Use the existing Tailwind palette (`ink`/`saffron`, defined in
`tailwind.config.js`) rather than introducing new colors — see
`trip-search-page.component.html` for the visual language (dark background,
saffron accents, `font-display` for headings).

## Checklist before you call it done

- [ ] Backend endpoint exists and is documented in `docs/api/API_EXAMPLES.md`
- [ ] Shared type matches the backend DTO exactly (field names, casing)
- [ ] Store handles the loading + error states (see `SearchStore`/`BookingStore` for the pattern)
- [ ] Route is lazy-loaded, not eagerly imported into `app.routes.ts`
- [ ] Component uses `@if`/`@for`, not the legacy structural directives
