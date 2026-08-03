import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { BookingStore } from '../../../../state/booking/booking.store';
import { PassengerInput } from '../../../../shared/types/booking.model';

/**
 * Simplified seat picker: the trip's total seat count is known, so we render
 * a numbered grid (A1, A2, ...) and let the customer tap to select up to
 * the number of passengers they add. A real seat map (with per-seat
 * availability from the API) is the natural next iteration — flagged in
 * ROADMAP.md — this demonstrates the booking flow end-to-end today.
 */
@Component({
  selector: 'app-seat-selection-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './seat-selection-page.component.html'
})
export class SeatSelectionPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  protected readonly store = inject(BookingStore);

  protected readonly selectedSeats = signal<string[]>([]);

  protected readonly seatLabels = computed(() => {
    const trip = this.store.selectedTrip();
    if (!trip) return [];
    return Array.from({ length: trip.totalSeats }, (_, i) => `A${i + 1}`);
  });

  protected readonly passengerForms = this.fb.array<ReturnType<typeof this.buildPassengerGroup>>([]);

  private buildPassengerGroup(seatNumber: string) {
    return this.fb.nonNullable.group({
      seatNumber: [seatNumber],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      age: [30, [Validators.required, Validators.min(1), Validators.max(120)]],
      gender: ['Male' as const, [Validators.required]]
    });
  }

  protected toggleSeat(seat: string): void {
    const current = this.selectedSeats();
    if (current.includes(seat)) {
      this.selectedSeats.set(current.filter((s) => s !== seat));
      const index = this.passengerForms.controls.findIndex((c) => c.value.seatNumber === seat);
      if (index >= 0) this.passengerForms.removeAt(index);
    } else {
      this.selectedSeats.set([...current, seat]);
      this.passengerForms.push(this.buildPassengerGroup(seat));
    }
  }

  protected get passengerFormsArray() {
    return this.passengerForms;
  }

  protected async onContinue(): Promise<void> {
    if (this.passengerForms.invalid || this.passengerForms.length === 0) {
      this.passengerForms.markAllAsTouched();
      return;
    }

    const passengers = this.passengerForms.getRawValue() as PassengerInput[];
    this.store.setPassengers(passengers);

    const booking = await this.store.confirmBooking();
    if (booking) {
      this.router.navigate(['/book', booking.bookingId, 'confirmation']);
    }
  }
}
