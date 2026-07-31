import { createContext, ReactNode, useCallback, useContext, useState } from 'react';

export type ToastTone = 'success' | 'danger' | 'info';

interface ToastItem {
  id: number;
  message: string;
  tone: ToastTone;
}

interface ToastContextValue {
  show: (message: string, tone?: ToastTone) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

const TONE_CLASSES: Record<ToastTone, string> = {
  success: 'bg-success text-white',
  danger: 'bg-danger text-white',
  info: 'bg-ink-800 text-white border border-ink-600'
};

let idCounter = 0;

/** App-wide toast provider — wrap the root once (main.tsx) and call useToast() anywhere below it. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  const show = useCallback((message: string, tone: ToastTone = 'info') => {
    const id = ++idCounter;
    setToasts((prev) => [...prev, { id, message, tone }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 4000);
  }, []);

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      <div className="fixed bottom-4 right-4 z-[100] flex flex-col gap-2 pointer-events-none">
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`pointer-events-auto rounded-lg px-4 py-2.5 text-sm font-medium shadow-popover animate-slide-up ${TONE_CLASSES[t.tone]}`}
          >
            {t.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast() must be used within <ToastProvider>');
  return ctx;
}
