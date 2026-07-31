import { Component, forwardRef, input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

let uid = 0;

/**
 * Shared text/number/date input that plugs directly into Reactive Forms via
 * ControlValueAccessor — so feature code keeps using `formControlName="x"`
 * exactly as it would with a plain `<input>`, but every field in the app
 * gets the same label/hint/error chrome for free.
 *
 * Usage: <ui-input formControlName="origin" label="From" placeholder="Dhaka" />
 */
@Component({
  selector: 'ui-input',
  standalone: true,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => InputComponent), multi: true }],
  template: `
    <label [attr.for]="fieldId" class="flex flex-col gap-1">
      @if (label()) {
        <span class="text-xs font-medium text-white/60">{{ label() }}</span>
      }
      <input
        [id]="fieldId"
        [type]="type()"
        [placeholder]="placeholder()"
        [disabled]="disabled"
        [value]="value ?? ''"
        (input)="onInput($event)"
        (blur)="onTouched()"
        class="bg-ink-900 border border-ink-700 rounded-md px-3 py-2 text-sm text-white placeholder:text-white/30 focus:outline-none focus:ring-2 focus:ring-saffron-500 disabled:opacity-50"
        [class.ring-2]="!!error()"
        [class.ring-danger]="!!error()"
      />
      @if (error()) {
        <span class="text-xs text-danger">{{ error() }}</span>
      } @else if (hint()) {
        <span class="text-xs text-white/40">{{ hint() }}</span>
      }
    </label>
  `
})
export class InputComponent implements ControlValueAccessor {
  readonly label = input<string>('');
  readonly type = input<'text' | 'number' | 'date' | 'email' | 'password' | 'tel'>('text');
  readonly placeholder = input<string>('');
  readonly hint = input<string>('');
  readonly error = input<string>('');

  protected readonly fieldId = `ui-input-${++uid}`;
  protected value: string | number = '';
  protected disabled = false;

  private onChange: (value: string | number) => void = () => {};
  protected onTouched: () => void = () => {};

  protected onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.value = this.type() === 'number' ? target.valueAsNumber : target.value;
    this.onChange(this.value);
  }

  writeValue(value: string | number): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string | number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
