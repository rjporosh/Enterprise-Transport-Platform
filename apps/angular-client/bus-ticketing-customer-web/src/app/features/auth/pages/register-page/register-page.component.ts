import { Component, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../../../core/auth/auth.store';
import { ButtonComponent } from '@shared-ui/button/button.component';
import { InputComponent } from '@shared-ui/input/input.component';
import { CardComponent } from '@shared-ui/card/card.component';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, ButtonComponent, InputComponent, CardComponent],
  template: `
    <main class="min-h-screen bg-ink-950 flex items-center justify-center px-6 py-16">
      <div class="w-full max-w-sm">
        <p class="text-saffron-500 text-sm tracking-[0.2em] uppercase mb-2 text-center">Bus Ticketing</p>
        <h1 class="font-display text-3xl text-white text-center mb-8">Create your account</h1>

        <ui-card>
          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
            <ui-input formControlName="fullName" label="Full name" placeholder="Rafiul Islam" />
            <ui-input formControlName="email" type="email" label="Email" placeholder="you@example.com" />
            <ui-input formControlName="password" type="password" label="Password" placeholder="At least 6 characters" />

            @if (authStore.error()) {
              <p class="text-danger text-sm bg-danger-bg rounded-md px-3 py-2">{{ authStore.error() }}</p>
            }

            <ui-button type="submit" [loading]="authStore.submitting()" class="w-full mt-1">
              Create account
            </ui-button>
          </form>
        </ui-card>

        <p class="text-white/50 text-sm text-center mt-6">
          Already have an account?
          <a routerLink="/auth/login" class="text-saffron-500 underline">Sign in</a>
        </p>
      </div>
    </main>
  `
})
export class RegisterPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  protected readonly authStore = inject(AuthStore);

  protected readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  protected async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const ok = await this.authStore.register(this.form.getRawValue());
    if (ok) {
      this.router.navigateByUrl('/profile/bookings');
    }
  }
}
