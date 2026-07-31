import { Component, input, output } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary:
    'bg-saffron-500 text-ink-950 hover:bg-saffron-600 focus-visible:ring-white disabled:bg-ink-700 disabled:text-white/40',
  secondary:
    'bg-ink-800 text-white border border-ink-600 hover:border-saffron-500 focus-visible:ring-saffron-500 disabled:opacity-40',
  ghost: 'bg-transparent text-white/80 hover:bg-white/5 focus-visible:ring-white/40 disabled:opacity-40',
  danger: 'bg-danger text-white hover:bg-red-600 focus-visible:ring-red-300 disabled:opacity-40'
};

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'text-xs px-3 py-1.5 gap-1.5',
  md: 'text-sm px-4 py-2.5 gap-2',
  lg: 'text-base px-6 py-3 gap-2.5'
};

/**
 * Shared, brand-consistent button. Framework counterpart:
 * `@shared-ui/react` <Button> — keep the variant/size contract identical
 * between the two so the customer app and admin console never drift.
 *
 * Usage: <ui-button variant="primary" size="md" [loading]="isSaving()">Save</ui-button>
 */
@Component({
  selector: 'ui-button',
  standalone: true,
  template: `
    <button
      [type]="type()"
      [disabled]="disabled() || loading()"
      (click)="clicked.emit($event)"
      [class]="
        'inline-flex items-center justify-center font-semibold rounded-md transition-colors duration-150 focus:outline-none focus-visible:ring-2 disabled:cursor-not-allowed ' +
        variantClass() +
        ' ' +
        sizeClass()
      "
    >
      @if (loading()) {
        <span class="inline-block animate-spin rounded-full border-2 border-current border-t-transparent h-3.5 w-3.5"></span>
      }
      <ng-content />
    </button>
  `
})
export class ButtonComponent {
  readonly variant = input<ButtonVariant>('primary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit'>('button');
  readonly loading = input(false);
  readonly disabled = input(false);
  readonly clicked = output<MouseEvent>();

  protected variantClass(): string {
    return VARIANT_CLASSES[this.variant()];
  }

  protected sizeClass(): string {
    return SIZE_CLASSES[this.size()];
  }
}
