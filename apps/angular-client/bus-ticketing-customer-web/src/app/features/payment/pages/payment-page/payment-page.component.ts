import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PaymentService } from '../../services/payment.service';
import { ButtonComponent } from '@shared-ui/button/button.component';
import { InputComponent } from '@shared-ui/input/input.component';
import { CardComponent } from '@shared-ui/card/card.component';
import { BadgeComponent } from '@shared-ui/badge/badge.component';

/**
 * Mock hosted-payment-page. Real integration will redirect to the Payment
 * Service's hosted checkout (see ROADMAP.md) — this simulates that round
 * trip end-to-end (card form -> "processing" -> booking flips to
 * Confirmed) so the full purchase journey is demoable today.
 */
@Component({
  selector: 'app-payment-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ButtonComponent, InputComponent, CardComponent, BadgeComponent],
  template: `
    <main class="min-h-screen bg-ink-950 flex items-center justify-center px-6 py-16">
      <div class="w-full max-w-md">
        @if (!confirmed()) {
          <p class="text-saffron-500 text-sm tracking-[0.2em] uppercase mb-2 text-center">Secure payment</p>
          <h1 class="font-display text-3xl text-white text-center mb-8">Complete your payment</h1>

          <ui-card>
            <form [formGroup]="form" (ngSubmit)="onPay()" class="flex flex-col gap-4">
              <ui-input formControlName="cardNumber" label="Card number" placeholder="4242 4242 4242 4242" />
              <div class="grid grid-cols-2 gap-3">
                <ui-input formControlName="expiry" label="Expiry" placeholder="MM/YY" />
                <ui-input formControlName="cvc" label="CVC" placeholder="123" />
              </div>
              <ui-input formControlName="cardholder" label="Cardholder name" placeholder="As printed on card" />

              @if (error()) {
                <p class="text-danger text-sm bg-danger-bg rounded-md px-3 py-2">{{ error() }}</p>
              }

              <ui-button type="submit" [loading]="submitting()" class="w-full mt-1">Pay now</ui-button>
              <p class="text-white/30 text-xs text-center">Demo mode — no real card is charged.</p>
            </form>
          </ui-card>
        } @else {
          <ui-card class="text-center">
            <ui-badge tone="success" class="mb-3">Payment successful</ui-badge>
            <h1 class="font-display text-2xl text-white mb-2">You're all set 🎟️</h1>
            <p class="text-white/60 text-sm mb-6">Your e-ticket has been confirmed and is available under My Bookings.</p>
            <a routerLink="/profile/bookings">
              <ui-button class="w-full">View my bookings</ui-button>
            </a>
          </ui-card>
        }
      </div>
    </main>
  `
})
export class PaymentPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly paymentService = inject(PaymentService);

  protected readonly submitting = signal(false);
  protected readonly confirmed = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    cardNumber: ['4242 4242 4242 4242', [Validators.required]],
    expiry: ['12/28', [Validators.required]],
    cvc: ['123', [Validators.required]],
    cardholder: ['', [Validators.required]]
  });

  protected onPay(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const bookingId = this.route.snapshot.paramMap.get('bookingId');
    if (!bookingId) return;

    this.submitting.set(true);
    this.error.set(null);

    this.paymentService.confirm(bookingId).subscribe({
      next: () => {
        this.submitting.set(false);
        this.confirmed.set(true);
      },
      error: () => {
        this.submitting.set(false);
        this.error.set('Payment could not be processed. Please try again.');
      }
    });
  }
}
