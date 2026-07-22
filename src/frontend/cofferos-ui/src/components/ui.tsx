import type { ReactNode } from 'react';

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div
      className={`rounded-xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] ${className}`}
    >
      {children}
    </div>
  );
}

export function Badge({ children, tone = 'default' }: { children: ReactNode; tone?: 'default' | 'orange' | 'green' | 'red' }) {
  const tones: Record<string, string> = {
    default: 'bg-[var(--color-coffer-border)] text-[var(--color-coffer-muted)]',
    orange: 'bg-[var(--color-coffer-orange)]/15 text-[var(--color-coffer-orange)]',
    green: 'bg-emerald-500/15 text-emerald-400',
    red: 'bg-red-500/15 text-red-400',
  };
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${tones[tone]}`}>
      {children}
    </span>
  );
}

export function Button({
  children,
  onClick,
  type = 'button',
  variant = 'primary',
  disabled = false,
  className = '',
}: {
  children: ReactNode;
  onClick?: () => void;
  type?: 'button' | 'submit';
  variant?: 'primary' | 'ghost';
  disabled?: boolean;
  className?: string;
}) {
  const variants: Record<string, string> = {
    primary: 'bg-[var(--color-coffer-orange)] text-black hover:brightness-110',
    ghost: 'border border-[var(--color-coffer-border)] text-[var(--color-coffer-muted)] hover:text-white',
  };
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`rounded-lg px-4 py-2 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-50 ${variants[variant]} ${className}`}
    >
      {children}
    </button>
  );
}

export function Spinner() {
  return (
    <div className="flex items-center justify-center py-16">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--color-coffer-border)] border-t-[var(--color-coffer-orange)]" />
    </div>
  );
}
