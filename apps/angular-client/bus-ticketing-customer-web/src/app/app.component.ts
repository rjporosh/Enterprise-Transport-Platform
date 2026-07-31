import { Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthStore } from './core/auth/auth.store';
import { ButtonComponent } from '@shared-ui/button/button.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, ButtonComponent],
  template: `
    <header class="sticky top-0 z-40 bg-ink-950/90 backdrop-blur border-b border-ink-800">
      <nav class="mx-auto max-w-5xl px-6 h-16 flex items-center justify-between">
        <a routerLink="/" class="font-display text-lg text-white">
          Bus<span class="text-saffron-500">Ticketing</span>
        </a>

        <div class="flex items-center gap-4">
          <a routerLink="/profile/bookings" class="text-white/70 hover:text-white text-sm font-medium hidden sm:inline">
            My bookings
          </a>

          @if (authStore.isAuthenticated()) {
            <span class="text-white/50 text-sm hidden md:inline">{{ authStore.user()?.fullName }}</span>
            <ui-button variant="ghost" size="sm" (clicked)="authStore.logout()">Sign out</ui-button>
          } @else {
            <a routerLink="/auth/login">
              <ui-button variant="secondary" size="sm">Sign in</ui-button>
            </a>
          }
        </div>
      </nav>
    </header>

    <router-outlet />
  `
})
export class AppComponent {
  protected readonly authStore = inject(AuthStore);
}
