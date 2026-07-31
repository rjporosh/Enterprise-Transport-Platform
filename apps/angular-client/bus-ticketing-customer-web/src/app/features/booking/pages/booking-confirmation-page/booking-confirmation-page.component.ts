import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BookingStore } from '../../../../state/booking/booking.store';
import { BookingService } from '../../services/booking.service';
import { Booking } from '../../../../shared/types/booking.model';

@Component({
  selector: 'app-booking-confirmation-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './booking-confirmation-page.component.html'
})
export class BookingConfirmationPageComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly bookingService = inject(BookingService);
  protected readonly store = inject(BookingStore);

  protected readonly booking = signal<Booking | null>(null);
  protected readonly loading = signal(true);
  protected readonly secondsRemaining = signal(0);

  private timer?: ReturnType<typeof setInterval>;

  protected readonly holdMinutesRemaining = computed(() => Math.max(0, Math.ceil(this.secondsRemaining() / 60)));

  ngOnInit(): void {
    // Prefer the booking we just created in-memory (no extra round trip);
    // fall back to fetching by id, e.g. on a hard page refresh.
    const inMemory = this.store.currentBooking();
    const bookingId = this.route.snapshot.paramMap.get('bookingId');

    if (inMemory && inMemory.bookingId === bookingId) {
      this.booking.set(inMemory);
      this.loading.set(false);
      this.startCountdown(inMemory.holdExpiresAtUtc);
      return;
    }

    if (bookingId) {
      this.bookingService.getById(bookingId).subscribe({
        next: (b) => {
          this.booking.set(b);
          this.loading.set(false);
          this.startCountdown(b.holdExpiresAtUtc);
        },
        error: () => this.loading.set(false)
      });
    }
  }

  private startCountdown(holdExpiresAtUtc: string): void {
    const expiry = new Date(holdExpiresAtUtc).getTime();
    const tick = () => this.secondsRemaining.set(Math.max(0, Math.floor((expiry - Date.now()) / 1000)));
    tick();
    this.timer = setInterval(tick, 1000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }
}
