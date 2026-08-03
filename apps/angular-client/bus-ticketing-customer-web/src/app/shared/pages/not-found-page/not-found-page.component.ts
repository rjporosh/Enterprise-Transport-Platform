import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '@shared-ui/button/button.component';

@Component({
  selector: 'app-not-found-page',
  standalone: true,
  imports: [RouterLink, ButtonComponent],
  template: `
    <main class="min-h-screen bg-ink-950 flex flex-col items-center justify-center px-6 text-center">
      <p class="font-display text-6xl text-saffron-500 mb-4">404</p>
      <h1 class="font-display text-2xl text-white mb-2">This route left without you</h1>
      <p class="text-white/50 text-sm mb-8">The page you're looking for doesn't exist or has moved.</p>
      <a routerLink="/">
        <ui-button>Back to search</ui-button>
      </a>
    </main>
  `
})
export class NotFoundPageComponent {}
