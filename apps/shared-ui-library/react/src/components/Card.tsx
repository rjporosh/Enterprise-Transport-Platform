import { HTMLAttributes, ReactNode } from 'react';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  children: ReactNode;
  padded?: boolean;
  tone?: 'dark' | 'light';
}

/** Base surface used everywhere a bordered, elevated block of content is needed. */
export function Card({ children, padded = true, tone = 'dark', className = '', ...rest }: CardProps) {
  const toneClasses =
    tone === 'dark' ? 'bg-ink-800 border-ink-700 text-white' : 'bg-white border-slate-200 text-ink-950 shadow-sm';

  return (
    <div className={`rounded-xl border ${toneClasses} ${padded ? 'p-5' : ''} ${className}`} {...rest}>
      {children}
    </div>
  );
}

export function CardHeader({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`flex items-center justify-between mb-4 ${className}`}>{children}</div>;
}
