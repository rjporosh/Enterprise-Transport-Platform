import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Booking, CreateBookingRequest } from '../../../shared/types/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/bookings`;

  create(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>(this.baseUrl, request);
  }

  getById(bookingId: string): Observable<Booking> {
    return this.http.get<Booking>(`${this.baseUrl}/${bookingId}`);
  }

  cancel(bookingId: string, customerId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${bookingId}/cancel`, { customerId, reason });
  }
}
