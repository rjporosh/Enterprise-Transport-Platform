import { Component, HostListener, input, output } from '@angular/core';

/** Shared modal used for confirmations (e.g. cancel booking) across the app. */
@Component({
  selector: 'ui-modal',
  standalone: true,
  template: `
    @if (open()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-ink-950/60 backdrop-blur-sm animate-fade-in" (click)="close.emit()"></div>
        <div class="relative bg-ink-800 border border-ink-700 text-white rounded-xl shadow-popover w-full max-w-md animate-slide-up">
          @if (title()) {
            <div class="flex items-center justify-between px-5 py-4 border-b border-ink-700">
              <h2 class="font-display text-lg">{{ title() }}</h2>
              <button (click)="close.emit()" aria-label="Close" class="text-white/50 hover:text-white text-lg leading-none">×</button>
            </div>
          }
          <div class="px-5 py-4">
            <ng-content />
          </div>
          <div class="px-5 py-4 border-t border-ink-700 flex justify-end gap-2">
            <ng-content select="[modalFooter]" />
          </div>
        </div>
      </div>
    }
  `
})
export class ModalComponent {
  readonly open = input(false);
  readonly title = input<string>('');
  readonly close = output<void>();

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.open()) this.close.emit();
  }
}
