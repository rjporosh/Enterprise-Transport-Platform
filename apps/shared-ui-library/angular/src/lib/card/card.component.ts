import { Component, input } from '@angular/core';

/** Base surface used everywhere a bordered, elevated block of content is needed. */
@Component({
  selector: 'ui-card',
  standalone: true,
  template: `
    <div
      [class]="
        'rounded-xl border ' +
        (tone() === 'dark' ? 'bg-ink-800 border-ink-700 text-white' : 'bg-white border-slate-200 text-ink-950 shadow-sm') +
        (padded() ? ' p-5' : '')
      "
    >
      <ng-content />
    </div>
  `
})
export class CardComponent {
  readonly tone = input<'dark' | 'light'>('dark');
  readonly padded = input(true);
}
