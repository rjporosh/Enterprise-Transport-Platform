import { Component, input } from '@angular/core';
import { CardComponent } from '../card/card.component';

export type StatDeltaDirection = 'up' | 'down' | 'flat';

const DELTA_COLOR: Record<StatDeltaDirection, string> = {
  up: 'text-success',
  down: 'text-danger',
  flat: 'text-white/50'
};

const DELTA_ARROW: Record<StatDeltaDirection, string> = { up: '▲', down: '▼', flat: '•' };

/** KPI tile used on dashboards and overview pages. */
@Component({
  selector: 'ui-stat-card',
  standalone: true,
  imports: [CardComponent],
  template: `
    <ui-card [tone]="tone()" class="flex flex-col gap-2">
      <div class="flex items-center justify-between">
        <span [class]="'text-xs uppercase tracking-wide ' + (tone() === 'dark' ? 'text-white/50' : 'text-ink-700/60')">
          {{ label() }}
        </span>
      </div>
      <span [class]="'font-display text-3xl ' + (tone() === 'dark' ? 'text-white' : 'text-ink-950')">{{ value() }}</span>
      @if (deltaValue()) {
        <span [class]="'text-xs font-medium ' + deltaColor()">{{ deltaArrow() }} {{ deltaValue() }}</span>
      }
    </ui-card>
  `
})
export class StatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly tone = input<'dark' | 'light'>('dark');
  readonly deltaValue = input<string>('');
  readonly deltaDirection = input<StatDeltaDirection>('flat');

  protected deltaColor(): string {
    return DELTA_COLOR[this.deltaDirection()];
  }

  protected deltaArrow(): string {
    return DELTA_ARROW[this.deltaDirection()];
  }
}
