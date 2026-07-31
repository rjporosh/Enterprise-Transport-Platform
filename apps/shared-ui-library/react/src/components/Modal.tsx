import { ReactNode, useEffect } from 'react';
import { createPortal } from 'react-dom';

export interface ModalProps {
  open: boolean;
  title?: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
}

/** Portal-rendered modal shared by both apps (e.g. cancel-booking confirmation, add-user forms). */
export function Modal({ open, title, onClose, children, footer }: ModalProps) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open, onClose]);

  if (!open) return null;

  return createPortal(
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-ink-950/60 backdrop-blur-sm animate-fade-in" onClick={onClose} />
      <div className="relative bg-white text-ink-950 rounded-xl shadow-popover w-full max-w-md animate-slide-up">
        {title && (
          <div className="flex items-center justify-between px-5 py-4 border-b border-slate-200">
            <h2 className="font-display text-lg">{title}</h2>
            <button
              onClick={onClose}
              aria-label="Close"
              className="text-ink-700/50 hover:text-ink-950 text-lg leading-none"
            >
              ×
            </button>
          </div>
        )}
        <div className="px-5 py-4">{children}</div>
        {footer && <div className="px-5 py-4 border-t border-slate-200 flex justify-end gap-2">{footer}</div>}
      </div>
    </div>,
    document.body
  );
}
