import { ReactNode } from 'react';

export interface PageHeaderProps {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
}

/** Consistent title block for every admin module page (Bookings, Trips, Buses, ...). */
export function PageHeader({ eyebrow, title, description, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-col md:flex-row md:items-end md:justify-between gap-4 mb-6">
      <div>
        {eyebrow && <p className="text-saffron-600 text-xs font-semibold tracking-[0.18em] uppercase mb-1">{eyebrow}</p>}
        <h1 className="font-display text-2xl md:text-3xl text-ink-950">{title}</h1>
        {description && <p className="text-sm text-ink-700/70 mt-1 max-w-2xl">{description}</p>}
      </div>
      {actions && <div className="flex items-center gap-2">{actions}</div>}
    </div>
  );
}
