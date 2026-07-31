import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { SearchStore } from '../../../../state/search/search.store';
import { BookingStore } from '../../../../state/booking/booking.store';

@Component({
  selector: 'app-trip-search-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './trip-search-page.component.html'
})
export class TripSearchPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  protected readonly store = inject(SearchStore);
  private readonly bookingStore = inject(BookingStore);

  protected readonly form = this.fb.nonNullable.group({
    origin: ['Dhaka', [Validators.required]],
    destination: ['Chattogram', [Validators.required]],
    date: [this.today(), [Validators.required]]
  });

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  protected async onSearch(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    await this.store.search(this.form.getRawValue());
  }

  protected selectTrip(tripId: string): void {
    const trip = this.store.selectedTrip(tripId);
    if (!trip) return;
    this.bookingStore.selectTrip(trip);
    this.router.navigate(['/book', tripId, 'seats']);
  }
}
