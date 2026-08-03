import { ReactNode } from 'react';
import { Card } from './Card';

export interface StatCardProps {
  label: string;
  value: ReactNode;
  delta?: { value: string; direction: 'up' | 'down' | 'flat' };
  icon?: ReactNode;
  tone?: 'dark' | 'light';
}

const DELTA_COLOR = {
  up: 'text-success',
  down: 'text-danger',
  flat: 'text-white/50'
};

/** KPI tile used on both the admin dashboard and the tenant overview. */
export function StatCard({ label, value, delta, icon, tone = 'dark' }: StatCardProps) {
  return (
    <Card tone={tone} className="flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <span className={`text-xs uppercase tracking-wide ${tone === 'dark' ? 'text-white/50' : 'text-ink-700/60'}`}>
          {label}
        </span>
        {icon && <span className={tone === 'dark' ? 'text-saffron-500' : 'text-saffron-600'}>{icon}</span>}
      </div>
      <span className={`font-display text-3xl ${tone === 'dark' ? 'text-white' : 'text-ink-950'}`}>{value}</span>
      {delta && (
        <span className={`text-xs font-medium ${DELTA_COLOR[delta.direction]}`}>
          {delta.direction === 'up' ? '▲' : delta.direction === 'down' ? '▼' : '•'} {delta.value}
        </span>
      )}
    </Card>
  );
}
