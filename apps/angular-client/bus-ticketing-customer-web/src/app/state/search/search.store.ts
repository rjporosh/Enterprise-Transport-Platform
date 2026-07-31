import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TripSearchService } from '../../features/search/services/trip-search.service';
import { TripSearchCriteria, TripSearchResult } from '../../shared/types/trip.model';
import { ApiProblemDetails } from '../../core/interceptors/error.interceptor';

/**
 * Signal-based store for the trip search feature. Deliberately not using
 * NgRx here — the state graph is shallow (criteria -> results) and Angular
 * signals give us the same "single source of truth, reactive templates"
 * benefit with far less boilerplate. Booking's store follows the same
 * pattern for consistency across the app.
 */
@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly tripSearchService = inject(TripSearchService);

  private readonly _criteria = signal<TripSearchCriteria | null>(null);
  private readonly _results = signal<TripSearchResult[]>([]);
  private readonly _totalCount = signal(0);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly criteria = this._criteria.asReadonly();
  readonly results = this._results.asReadonly();
  readonly totalCount = this._totalCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly hasSearched = computed(() => this._criteria() !== null);

  async search(criteria: TripSearchCriteria): Promise<void> {
    this._criteria.set(criteria);
    this._loading.set(true);
    this._error.set(null);

    try {
      const result = await firstValueFrom(this.tripSearchService.search(criteria));
      this._results.set(result.items);
      this._totalCount.set(result.totalCount);
    } catch (err) {
      const problem = err as ApiProblemDetails;
      this._error.set(problem?.title ?? 'Unable to search trips right now.');
      this._results.set([]);
    } finally {
      this._loading.set(false);
    }
  }

  selectedTrip(tripId: string): TripSearchResult | undefined {
    return this._results().find((t) => t.tripId === tripId);
  }
}
