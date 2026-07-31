export interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  label?: string;
}

const SIZE_PX: Record<NonNullable<SpinnerProps['size']>, string> = {
  sm: 'h-3.5 w-3.5 border-2',
  md: 'h-5 w-5 border-2',
  lg: 'h-8 w-8 border-[3px]'
};

/** Minimal, dependency-free spinner — an SVG import would be overkill for a single ring. */
export function Spinner({ size = 'md', className = '', label = 'Loading' }: SpinnerProps) {
  return (
    <span
      role="status"
      aria-label={label}
      className={`inline-block animate-spin rounded-full border-current border-t-transparent ${SIZE_PX[size]} ${className}`}
    />
  );
}
