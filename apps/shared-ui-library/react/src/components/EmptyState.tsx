import { ReactNode } from 'react';

export interface EmptyStateProps {
  title: string;
  description?: string;
  icon?: ReactNode;
  action?: ReactNode;
}

/** Consistent "nothing here yet" / "no results" block for lists, tables and search results. */
export function EmptyState({ title, description, icon, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center text-center gap-3 py-14 px-6">
      {icon && <div className="text-white/30 text-4xl">{icon}</div>}
      <p className="font-display text-lg text-white/80">{title}</p>
      {description && <p className="text-sm text-white/50 max-w-sm">{description}</p>}
      {action}
    </div>
  );
}
