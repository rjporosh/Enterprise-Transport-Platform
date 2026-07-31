import { ReactNode } from 'react';

export type BadgeTone = 'neutral' | 'success' | 'danger' | 'warning' | 'info' | 'brand';

const TONE_CLASSES: Record<BadgeTone, string> = {
  neutral: 'bg-white/10 text-white/70',
  success: 'bg-success-bg text-success',
  danger: 'bg-danger-bg text-danger',
  warning: 'bg-warning-bg text-warning',
  info: 'bg-info-bg text-info',
  brand: 'bg-saffron-500/20 text-saffron-500'
};

export interface BadgeProps {
  children: ReactNode;
  tone?: BadgeTone;
  className?: string;
}

export function Badge({ children, tone = 'neutral', className = '' }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium whitespace-nowrap ${TONE_CLASSES[tone]} ${className}`}
    >
      {children}
    </span>
  );
}

/** Maps the platform's shared booking/trip/tenant status strings to a badge tone consistently across apps. */
export function statusToBadgeTone(status: string): BadgeTone {
  const map: Record<string, BadgeTone> = {
    Confirmed: 'success',
    Active: 'success',
    Completed: 'success',
    PendingPayment: 'warning',
    Pending: 'warning',
    Scheduled: 'info',
    InTransit: 'info',
    Cancelled: 'danger',
    Expired: 'danger',
    Suspended: 'danger',
    Refunded: 'neutral'
  };
  return map[status] ?? 'neutral';
}
