import { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, forwardRef } from 'react';

interface FieldShellProps {
  label?: string;
  hint?: string;
  error?: string;
  children: ReactNode;
  htmlFor?: string;
}

function FieldShell({ label, hint, error, children, htmlFor }: FieldShellProps) {
  return (
    <label htmlFor={htmlFor} className="flex flex-col gap-1">
      {label && <span className="text-xs font-medium text-white/60">{label}</span>}
      {children}
      {error ? (
        <span className="text-xs text-danger">{error}</span>
      ) : hint ? (
        <span className="text-xs text-white/40">{hint}</span>
      ) : null}
    </label>
  );
}

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  hint?: string;
  error?: string;
}

const FIELD_BASE =
  'bg-ink-900 border border-ink-700 rounded-md px-3 py-2 text-sm text-white placeholder:text-white/30 focus:outline-none focus:ring-2 focus:ring-saffron-500 disabled:opacity-50 transition-shadow';

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, hint, error, id, className = '', ...rest },
  ref
) {
  return (
    <FieldShell label={label} hint={hint} error={error} htmlFor={id}>
      <input
        ref={ref}
        id={id}
        className={`${FIELD_BASE} ${error ? 'ring-2 ring-danger' : ''} ${className}`}
        {...rest}
      />
    </FieldShell>
  );
});

export interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  hint?: string;
  error?: string;
}

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { label, hint, error, id, className = '', children, ...rest },
  ref
) {
  return (
    <FieldShell label={label} hint={hint} error={error} htmlFor={id}>
      <select ref={ref} id={id} className={`${FIELD_BASE} ${className}`} {...rest}>
        {children}
      </select>
    </FieldShell>
  );
});
