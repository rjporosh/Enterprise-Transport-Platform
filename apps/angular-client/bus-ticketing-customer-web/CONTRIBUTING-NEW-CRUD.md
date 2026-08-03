# Adding a new feature (CRUD) — Angular customer web

This app is organized as **feature modules under `src/app/features/`**, each
one self-contained (own routes, pages, services, models) and lazy-loaded
from `app.routes.ts`. Cross-cutting concerns live in `src/app/core/`
(auth, HTTP interceptors, guards, mock API) and reusable pieces live in
`src/app/shared/` (types, layout, pipes) or in the framework-shared
`@shared-ui/*` component library.

Use the existing **Booking** feature
(`src/app/features/booking/`) as the reference implementation — every step
below mirrors what's already there.

## Worked example: adding a "Support Tickets" CRUD feature

### 1. Model — `src/app/shared/types/support-ticket.model.ts`
Define the shape returned by the API. Mirror `booking.model.ts`:
```ts
export interface SupportTicket {
  ticketId: string;
  subject: string;
  status: 'Open' | 'InProgress' | 'Resolved';
  createdAtUtc: string;
}
```
Put it in `shared/types/` (not inside the feature folder) if more than one
feature might reference it — e.g. an admin equivalent later.

### 2. HTTP service — `src/app/features/support/services/support-ticket.service.ts`
One method per REST operation, thin, no business logic — copy the shape of
`features/booking/services/booking.service.ts`:
```ts
@Injectable({ providedIn: 'root' })
export class SupportTicketService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/support-tickets`;

  list(): Observable<SupportTicket[]> { return this.http.get<SupportTicket[]>(this.baseUrl); }
  get(id: string): Observable<SupportTicket> { return this.http.get<SupportTicket>(`${this.baseUrl}/${id}`); }
  create(payload: { subject: string }): Observable<SupportTicket> { return this.http.post<SupportTicket>(this.baseUrl, payload); }
  update(id: string, payload: Partial<SupportTicket>): Observable<SupportTicket> { return this.http.patch<SupportTicket>(`${this.baseUrl}/${id}`, payload); }
  remove(id: string): Observable<void> { return this.http.delete<void>(`${this.baseUrl}/${id}`); }
}
```

### 3. State — `src/app/features/support/support.store.ts` (optional but recommended)
If the feature has more than one page sharing state, add a signal store
following `state/booking/booking.store.ts` / `core/auth/auth.store.ts` —
`signal()` for state, plain async methods that call the service and update
signals, `computed()` for derived values. Skip this for a single simple list
page and just call the service directly from the page component.

### 4. Pages — `src/app/features/support/pages/<page-name>/`
One folder per page, standalone component, same structure as
`features/booking/pages/seat-selection-page/`. Import shared UI primitives
from `@shared-ui/*` (`ui-page-header`, `ui-card`, `ui-button`, `ui-input`,
`ui-badge`, `ui-empty-state`, `ui-spinner`, `ui-modal`) instead of writing
new buttons/cards/inputs — see `features/profile/pages/my-bookings-page/`
for a page that uses nearly the whole shared set.

### 5. Routes — `src/app/features/support/routes.ts`
```ts
export const SUPPORT_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/ticket-list-page/ticket-list-page.component').then(m => m.TicketListPageComponent) },
  { path: ':ticketId', loadComponent: () => import('./pages/ticket-detail-page/ticket-detail-page.component').then(m => m.TicketDetailPageComponent) },
];
```
If any page should require sign-in, add `canActivate: [authGuard]` — see
`features/profile/routes.ts`.

### 6. Wire it up — `src/app/app.routes.ts`
Add one lazy `loadChildren` entry, same pattern as the five already there:
```ts
{ path: 'support', loadChildren: () => import('./features/support/routes').then(m => m.SUPPORT_ROUTES) },
```

### 7. Mock API (until the real backend exists) — `src/app/core/interceptors/mock-api.interceptor.ts`
Add matching `if (path === '/support-tickets' && req.method === 'GET') { ... }`
branches, backed by fixtures in `src/app/core/mock/mock-data.ts` — follow
the booking branches already there. Remove/bypass once the real service is
live (the interceptor already no-ops when `environment.mockApi === false`).

### 8. Navigation
Add a link in `app.component.ts`'s header nav if the feature should be
reachable from anywhere, the way "My bookings" is.

## Files touched for a typical new CRUD feature
| File | Purpose |
|---|---|
| `shared/types/<entity>.model.ts` | TypeScript interface for the API shape |
| `features/<name>/services/<entity>.service.ts` | HttpClient calls |
| `features/<name>/<name>.store.ts` | *(optional)* shared signal state |
| `features/<name>/pages/*/*.component.ts` | UI, using `@shared-ui/*` |
| `features/<name>/routes.ts` | Feature's own route table |
| `app.routes.ts` | One new `loadChildren` line |
| `core/mock/mock-data.ts` + `mock-api.interceptor.ts` | Demo data until backend exists |

## Conventions to keep
- Standalone components only — no `NgModule`s.
- Signals for local/shared state; RxJS stays inside services (`Observable`
  return types), converted with `firstValueFrom`/`toSignal` at the store
  boundary — see `auth.store.ts`.
- New-style control flow (`@if`, `@for`) in templates, not `*ngIf`/`*ngFor`.
- Reactive Forms + `@shared-ui/input` (implements `ControlValueAccessor`) for
  every form field — never a raw `<input>`.
- Every list/detail page handles loading, empty, and error states explicitly
  (`ui-spinner`, `ui-empty-state`) — see `my-bookings-page.component.ts`.
