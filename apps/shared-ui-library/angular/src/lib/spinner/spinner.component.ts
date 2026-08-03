import { Component, input } from '@angular/core';

const SIZE_CLASSES: Record<'sm' | 'md' | 'lg', string> = {
  sm: 'h-3.5 w-3.5 border-2',
  md: 'h-5 w-5 border-2',
  lg: 'h-8 w-8 border-[3px]'
};

@Component({
  selector: 'ui-spinner',
  standalone: true,
  template: `
    <span
      role="status"
      [attr.aria-label]="label()"
      [class]="'inline-block animate-spin rounded-full border-current border-t-transparent ' + sizeClass()"
    ></span>
  `
})
export class SpinnerComponent {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly label = input('Loading');

  protected sizeClass(): string {
    return SIZE_CLASSES[this.size()];
  }
}
