# Adding a new module to the React admin console

Worked example: adding a **"Trips"** management module (list + detail),
following the same pattern as the existing `bookings` module in
`apps/react-admin/bus-ticketing-admin/src/modules/bookings/`.

## The pattern this app follows

- One folder per module under `modules/<name>/`: `api/`, `hooks/`,
  `models/`, `pages/`, `routes.tsx`, `index.ts`.
- Data fetching via TanStack Query hooks, never `fetch`/`axios` called
  directly from a component.
- A shared `httpClient` (`src/api/httpClient.ts`) with the bearer token
  interceptor already wired — new API modules import and use it, they don't
  create their own axios instance.

## Step 1 — Model

```typescript
// src/modules/trips/models/trip.model.ts
export interface Trip {
  tripId: string;
  originCity: string;
  destinationCity: string;
  departureUtc: string;
  availableSeats: number;
  totalSeats: number;
}
```

## Step 2 — API client

```typescript
// src/modules/trips/api/trips.api.ts
import { httpClient } from '../../../api/httpClient';
import { Trip } from '../models/trip.model';

export const tripsApi = {
  search: async (params: { origin: string; destination: string; date: string; page?: number; pageSize?: number }) => {
    const { data } = await httpClient.get<{ items: Trip[]; totalCount: number }>('/trips/search', { params });
    return data;
  }
};
```

## Step 3 — Hook

```typescript
// src/modules/trips/hooks/useTrips.ts
import { useQuery } from '@tanstack/react-query';
import { tripsApi } from '../api/trips.api';

export function useTrips(params: { origin: string; destination: string; date: string; page?: number; pageSize?: number }) {
  return useQuery({
    queryKey: ['trips', params],
    queryFn: () => tripsApi.search(params),
    enabled: Boolean(params.origin && params.destination && params.date)
  });
}
```

For a mutation (create/cancel/update), follow `useCancelBooking.ts`'s
pattern: `useMutation` + `queryClient.invalidateQueries(...)` in `onSuccess`
so the list re-fetches automatically after a write.

## Step 4 — Page

Follow `BookingsListPage.tsx` as the template: loading/error states handled
explicitly, Tailwind classes from the existing palette (`ink`/`saffron`,
same `tailwind.config.js` as the customer app for brand consistency),
pagination controls reading the same header shape documented in
`docs/api/API_PAGINATION.md` if you're building a paginated list (the
`X-Pagination` response header — `httpClient`'s axios response gives you
`response.headers['x-pagination']` if you need the metadata instead of
just `totalCount` from the body).

## Step 5 — Routes + navigation

```typescript
// src/modules/trips/routes.tsx
import { RouteObject } from 'react-router-dom';
import TripsListPage from './pages/TripsListPage';

export const tripsRoutes: RouteObject[] = [
  { path: 'trips', element: <TripsListPage /> }
];
```

```typescript
// src/modules/trips/index.ts
export { tripsRoutes } from './routes';
export * from './models/trip.model';
```

Wire into `src/app/App.tsx` (spread `tripsRoutes` alongside `bookingsRoutes`)
and add a nav entry in `src/layouts/AdminLayout.tsx`'s `NAV_ITEMS` array.

## Checklist before you call it done

- [ ] No direct `axios`/`fetch` calls outside `api/*.api.ts` files
- [ ] Every list/detail fetch goes through a `useQuery` hook, every write through `useMutation`
- [ ] Mutations invalidate the right query keys so the UI reflects the change without a manual refresh
- [ ] New nav entry added to `AdminLayout.tsx`
- [ ] Route added to `App.tsx`
