import { Component, input } from '@angular/core';

@Component({
  selector: 'ui-page-header',
  standalone: true,
  template: `
    <div class="flex flex-col md:flex-row md:items-end md:justify-between gap-4 mb-6">
      <div>
        @if (eyebrow()) {
          <p class="text-saffron-500 text-xs font-semibold tracking-[0.18em] uppercase mb-1">{{ eyebrow() }}</p>
        }
        <h1 class="font-display text-2xl md:text-3xl">{{ title() }}</h1>
        @if (description()) {
          <p class="text-sm opacity-70 mt-1 max-w-2xl">{{ description() }}</p>
        }
      </div>
      <div class="flex items-center gap-2">
        <ng-content />
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  readonly eyebrow = input<string>('');
  readonly title = input.required<string>();
  readonly description = input<string>('');
}
