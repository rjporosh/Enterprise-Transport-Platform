import { ButtonHTMLAttributes, ReactNode, forwardRef } from 'react';
import { Spinner } from './Spinner';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  leadingIcon?: ReactNode;
  trailingIcon?: ReactNode;
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary:
    'bg-saffron-500 text-ink-950 hover:bg-saffron-600 focus-visible:ring-white disabled:bg-ink-700 disabled:text-white/40',
  secondary:
    'bg-ink-800 text-white border border-ink-600 hover:border-saffron-500 focus-visible:ring-saffron-500 disabled:opacity-40',
  ghost: 'bg-transparent text-white/80 hover:bg-white/5 focus-visible:ring-white/40 disabled:opacity-40',
  danger: 'bg-danger text-white hover:bg-red-600 focus-visible:ring-red-300 disabled:opacity-40'
};

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'text-xs px-3 py-1.5 gap-1.5',
  md: 'text-sm px-4 py-2.5 gap-2',
  lg: 'text-base px-6 py-3 gap-2.5'
};

/**
 * Shared, brand-consistent button used across both the customer web app and
 * the admin console. Framework counterpart: `@shared-ui/angular` Button
 * component — keep the variant/size contract in sync between the two.
 */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  { variant = 'primary', size = 'md', loading = false, leadingIcon, trailingIcon, className = '', children, disabled, ...rest },
  ref
) {
  return (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={`inline-flex items-center justify-center font-semibold rounded-md transition-colors duration-150 focus:outline-none focus-visible:ring-2 disabled:cursor-not-allowed ${VARIANT_CLASSES[variant]} ${SIZE_CLASSES[size]} ${className}`}
      {...rest}
    >
      {loading ? <Spinner size="sm" /> : leadingIcon}
      {children}
      {!loading && trailingIcon}
    </button>
  );
});
