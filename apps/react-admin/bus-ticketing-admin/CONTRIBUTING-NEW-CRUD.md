# Adding a new feature (CRUD) — React admin console

This app is organized as **feature modules under `src/modules/`**, each one
self-contained (own model, api client, hooks, pages, routes) and composed
together in `src/app/App.tsx`. Cross-cutting concerns live in `src/api/`
(axios instance, mock adapter) and `src/modules/auth/` (auth context,
protected route). Reusable UI comes from `@shared-ui/react` — never write a
new button/card/table from scratch.

Use the existing **Trips** module (`src/modules/trips/`) as the reference
implementation — every step below mirrors what's already there. Several
empty module folders already exist under `src/modules/` (customers,
pricing, promotions, reports, ...) as placeholders for the roadmap — follow
the same steps to fill one in.

## Worked example: adding a "Promotions" CRUD module

### 1. Model — `src/modules/promotions/models/promotion.model.ts`
```ts
export interface Promotion {
  promotionId: string;
  code: string;
  discountPercent: number;
  status: 'Active' | 'Expired';
}
```

### 2. API client — `src/modules/promotions/api/promotions.api.ts`
Thin wrapper around `httpClient` (the shared axios instance), one function
per REST operation — copy the shape of `modules/trips/api/trips.api.ts`:
```ts
import { httpClient } from '../../../api/httpClient';
import { Promotion } from '../models/promotion.model';
import { PagedResult } from '../../trips/api/trips.api';

export const promotionsApi = {
  list: async (params: { page?: number; pageSize?: number } = {}): Promise<PagedResult<Promotion>> => {
    const { data } = await httpClient.get<PagedResult<Promotion>>('/promotions', { params });
    return data;
  },
  create: async (payload: { code: string; discountPercent: number }): Promise<Promotion> => {
    const { data } = await httpClient.post<Promotion>('/promotions', payload);
    return data;
  }
};
```

### 3. React Query hooks — `src/modules/promotions/hooks/`
One hook per operation, following `modules/trips/hooks/useTrips.ts` (reads)
and `modules/bookings/hooks/useCancelBooking.ts` (mutations with cache
invalidation).

### 4. Pages — `src/modules/promotions/pages/`
Build list/detail pages from `@shared-ui/react` primitives —
`PageHeader`, `DataTable` + `Pagination` for lists, `Card`/`Badge`/`Button`
for detail views, `Modal` for confirmations, `Input`/`Select` for forms. See
`modules/trips/pages/TripsListPage.tsx` for a filtered, paginated list and
`modules/bookings/pages/BookingDetailPage.tsx` for a detail-plus-action
page.

### 5. Routes — `src/modules/promotions/routes.tsx`
```tsx
import { RouteObject } from 'react-router-dom';
import PromotionsListPage from './pages/PromotionsListPage';

export const promotionsRoutes: RouteObject[] = [{ path: 'promotions', element: <PromotionsListPage /> }];
```

### 6. Barrel — `src/modules/promotions/index.ts`
```ts
export { promotionsRoutes } from './routes';
export * from './models/promotion.model';
```

### 7. Wire it up — `src/app/App.tsx`
Add the import and spread it into `PROTECTED_MODULE_ROUTES`, same pattern
as the six modules already there.

### 8. Nav — `src/layouts/AdminLayout.tsx`
Add one entry to `NAV_ITEMS`.

### 9. Mock API (until the real backend exists) — `src/api/mockAdapter.ts`
Add matching `if (path === '/promotions' && method === 'GET') { ... }`
branches, backed by fixtures in `src/api/mock/fixtures.ts` — follow the
`/trips` or `/buses` branches already there. This is skipped automatically
once `VITE_USE_MOCK_API=false` and the real service is deployed — no code
changes needed in the module itself.

## Files touched for a typical new CRUD module
| File | Purpose |
|---|---|
| `modules/<name>/models/<entity>.model.ts` | TypeScript interface for the API shape |
| `modules/<name>/api/<entity>.api.ts` | axios calls via the shared `httpClient` |
| `modules/<name>/hooks/*.ts` | `useQuery`/`useMutation` wrappers |
| `modules/<name>/pages/*.tsx` | UI, built from `@shared-ui/react` |
| `modules/<name>/routes.tsx` | This module's `RouteObject[]` |
| `modules/<name>/index.ts` | Barrel export |
| `app/App.tsx` | Spread the new routes into the protected route tree |
| `layouts/AdminLayout.tsx` | One new nav entry |
| `api/mock/fixtures.ts` + `api/mockAdapter.ts` | Demo data until backend exists |

## Conventions to keep
- Function components + hooks only, no class components.
- All server state through TanStack Query (`useQuery`/`useMutation`) — never
  `useEffect` + manual `fetch`.
- All API calls go through `httpClient` (never a bare `axios.get(...)`) so
  the mock adapter and auth header interceptor apply consistently.
- UI primitives come from `@shared-ui/react` first; only reach for a raw
  `<div>`/`<table>` when nothing in the library fits, and consider adding a
  new shared component (with its Angular twin — see
  `apps/shared-ui-library/README.md`) instead.
- Every list page handles loading and empty states via `DataTable`'s
  built-in `loading`/`emptyTitle` props — don't hand-roll a spinner block.
