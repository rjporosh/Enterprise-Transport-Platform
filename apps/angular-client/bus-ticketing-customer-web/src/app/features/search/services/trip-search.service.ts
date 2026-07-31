import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { PagedResult, TripSearchCriteria, TripSearchResult } from '../../../shared/types/trip.model';

@Injectable({ providedIn: 'root' })
export class TripSearchService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/trips`;

  search(criteria: TripSearchCriteria, page = 1, pageSize = 20): Observable<PagedResult<TripSearchResult>> {
    const params = new HttpParams()
      .set('origin', criteria.origin)
      .set('destination', criteria.destination)
      .set('date', criteria.date)
      .set('page', page)
      .set('pageSize', pageSize);

    return this.http.get<PagedResult<TripSearchResult>>(`${this.baseUrl}/search`, { params });
  }
}
