import { Component, input } from '@angular/core';

@Component({
  selector: 'ui-empty-state',
  standalone: true,
  template: `
    <div class="flex flex-col items-center text-center gap-3 py-14 px-6">
      <p class="font-display text-lg text-white/80">{{ title() }}</p>
      @if (description()) {
        <p class="text-sm text-white/50 max-w-sm">{{ description() }}</p>
      }
      <ng-content />
    </div>
  `
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly description = input<string>('');
}
