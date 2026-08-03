import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MyBookingsService } from '../../services/my-bookings.service';
import { Booking } from '../../../../shared/types/booking.model';
import { PageHeaderComponent } from '@shared-ui/page-header/page-header.component';
import { CardComponent } from '@shared-ui/card/card.component';
import { BadgeComponent, statusToBadgeTone } from '@shared-ui/badge/badge.component';
import { ButtonComponent } from '@shared-ui/button/button.component';
import { SpinnerComponent } from '@shared-ui/spinner/spinner.component';
import { EmptyStateComponent } from '@shared-ui/empty-state/empty-state.component';
import { ModalComponent } from '@shared-ui/modal/modal.component';

@Component({
  selector: 'app-my-bookings-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    PageHeaderComponent,
    CardComponent,
    BadgeComponent,
    ButtonComponent,
    SpinnerComponent,
    EmptyStateComponent,
    ModalComponent
  ],
  template: `
    <main class="min-h-screen bg-ink-950 px-6 py-10">
      <div class="mx-auto max-w-3xl">
        <ui-page-header eyebrow="Account" title="My bookings" description="Everything you've booked, held or completed.">
          <a routerLink="/">
            <ui-button variant="secondary" size="sm">Book another trip</ui-button>
          </a>
        </ui-page-header>

        @if (loading()) {
          <div class="flex justify-center py-16 text-white/50"><ui-spinner size="lg" /></div>
        } @else if (bookings().length === 0) {
          <ui-card>
            <ui-empty-state title="No bookings yet" description="Search for a route to make your first booking." />
          </ui-card>
        } @else {
          <div class="flex flex-col gap-3">
            @for (booking of bookings(); track booking.bookingId) {
              <ui-card class="flex flex-col md:flex-row md:items-center justify-between gap-3">
                <div>
                  <p class="text-white/50 text-xs font-mono">#{{ booking.bookingId.slice(0, 10) }}</p>
                  <p class="font-display text-lg text-white mt-1">{{ booking.seats.length }} seat(s)</p>
                  <p class="text-white/50 text-sm">{{ booking.createdAtUtc | date: 'medium' }}</p>
                </div>
                <div class="flex items-center gap-3">
                  <ui-badge [tone]="badgeTone(booking.status)">{{ booking.status }}</ui-badge>
                  <p class="font-display text-saffron-500 text-lg">{{ booking.totalAmount | number: '1.0-0' }} {{ booking.currency }}</p>
                  @if (booking.status === 'PendingPayment' || booking.status === 'Confirmed') {
                    <ui-button variant="ghost" size="sm" (clicked)="requestCancel(booking)">Cancel</ui-button>
                  }
                </div>
              </ui-card>
            }
          </div>
        }
      </div>
    </main>

    <ui-modal [open]="cancelTarget() !== null" title="Cancel this booking?" (close)="cancelTarget.set(null)">
      <p class="text-white/70 text-sm">
        This will release seat(s) {{ cancelTarget()?.seats?.length }} and cannot be undone.
      </p>
      <div modalFooter class="flex gap-2">
        <ui-button variant="secondary" size="sm" (clicked)="cancelTarget.set(null)">Keep booking</ui-button>
        <ui-button variant="danger" size="sm" [loading]="cancelling()" (clicked)="confirmCancel()">Yes, cancel</ui-button>
      </div>
    </ui-modal>
  `
})
export class MyBookingsPageComponent implements OnInit {
  private readonly bookingsService = inject(MyBookingsService);

  protected readonly bookings = signal<Booking[]>([]);
  protected readonly loading = signal(true);
  protected readonly cancelTarget = signal<Booking | null>(null);
  protected readonly cancelling = signal(false);

  ngOnInit(): void {
    this.bookingsService.listMine().subscribe({
      next: (bookings) => {
        this.bookings.set(bookings);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected badgeTone(status: string) {
    return statusToBadgeTone(status);
  }

  protected requestCancel(booking: Booking): void {
    this.cancelTarget.set(booking);
  }

  protected confirmCancel(): void {
    const target = this.cancelTarget();
    if (!target) return;

    this.cancelling.set(true);
    this.bookingsService.cancel(target.bookingId, 'Customer requested cancellation').subscribe({
      next: () => {
        this.bookings.update((list) =>
          list.map((b) => (b.bookingId === target.bookingId ? { ...b, status: 'Cancelled' } : b))
        );
        this.cancelling.set(false);
        this.cancelTarget.set(null);
      },
      error: () => this.cancelling.set(false)
    });
  }
}
