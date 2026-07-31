import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { BookingService } from '../../features/booking/services/booking.service';
import { Booking, CreateBookingRequest, PassengerInput } from '../../shared/types/booking.model';
import { TripSearchResult } from '../../shared/types/trip.model';
import { ApiProblemDetails } from '../../core/interceptors/error.interceptor';

/** Placeholder until the Auth feature is built — real customer id comes from the JWT. */
const DEMO_CUSTOMER_ID = '00000000-0000-0000-0000-000000000001';

@Injectable({ providedIn: 'root' })
export class BookingStore {
  private readonly bookingService = inject(BookingService);

  private readonly _selectedTrip = signal<TripSearchResult | null>(null);
  private readonly _passengers = signal<PassengerInput[]>([]);
  private readonly _currentBooking = signal<Booking | null>(null);
  private readonly _submitting = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly selectedTrip = this._selectedTrip.asReadonly();
  readonly passengers = this._passengers.asReadonly();
  readonly currentBooking = this._currentBooking.asReadonly();
  readonly submitting = this._submitting.asReadonly();
  readonly error = this._error.asReadonly();

  selectTrip(trip: TripSearchResult): void {
    this._selectedTrip.set(trip);
    this._passengers.set([]);
    this._error.set(null);
  }

  setPassengers(passengers: PassengerInput[]): void {
    this._passengers.set(passengers);
  }

  async confirmBooking(): Promise<Booking | null> {
    const trip = this._selectedTrip();
    if (!trip || this._passengers().length === 0) {
      this._error.set('Select a trip and at least one seat before booking.');
      return null;
    }

    this._submitting.set(true);
    this._error.set(null);

    const request: CreateBookingRequest = {
      tripId: trip.tripId,
      customerId: DEMO_CUSTOMER_ID,
      passengers: this._passengers()
    };

    try {
      const booking = await firstValueFrom(this.bookingService.create(request));
      this._currentBooking.set(booking);
      return booking;
    } catch (err) {
      const problem = err as ApiProblemDetails;
      // 409 means someone else grabbed the seat between search and submit —
      // surface that distinctly so the UI can prompt the user to re-search
      // rather than showing a generic error.
      this._error.set(
        problem?.status === 409
          ? 'One of the selected seats was just taken. Please choose another seat.'
          : problem?.title ?? 'Unable to complete the booking. Please try again.'
      );
      return null;
    } finally {
      this._submitting.set(false);
    }
  }

  reset(): void {
    this._selectedTrip.set(null);
    this._passengers.set([]);
    this._currentBooking.set(null);
    this._error.set(null);
  }
}
