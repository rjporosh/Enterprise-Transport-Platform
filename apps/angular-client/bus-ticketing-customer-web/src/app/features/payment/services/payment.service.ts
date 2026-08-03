import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Booking } from '../../../shared/types/booking.model';

@Injectable({ providedIn: 'root' })
export class PaymentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/payments`;

  /** Confirms payment for a held booking. Card details are deliberately never sent anywhere — this vertical slice simulates a hosted payment-page redirect flow, which is what the real Payment Service will do (see ROADMAP.md). */
  confirm(bookingId: string): Observable<Booking> {
    return this.http.post<Booking>(`${this.baseUrl}/${bookingId}/confirm`, {});
  }
}
