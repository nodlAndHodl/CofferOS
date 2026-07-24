import { useState, type ReactNode } from 'react';
import { Check, Copy } from 'lucide-react';

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div
      className={`rounded-xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] ${className}`}
    >
      {children}
    </div>
  );
}

export function Badge({ children, tone = 'default', className = '' }: { children: ReactNode; tone?: 'default' | 'orange' | 'green' | 'red' | 'blue' | 'purple'; className?: string }) {
  const tones: Record<string, string> = {
    default: 'bg-[var(--color-coffer-border)] text-[var(--color-coffer-muted)]',
    orange: 'bg-[var(--color-coffer-orange)]/15 text-[var(--color-coffer-orange)]',
    green: 'bg-emerald-500/15 text-emerald-400',
    red: 'bg-red-500/15 text-red-400',
    blue: 'bg-blue-500/15 text-blue-400',
    purple: 'bg-purple-500/15 text-purple-400',
  };
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${tones[tone]} ${className}`}>
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

export function CopyButton({ value, className = '' }: { value: string; className?: string }) {
  const [copied, setCopied] = useState(false);

  function handleClick(e: React.MouseEvent<HTMLButtonElement>) {
    e.stopPropagation();
    void navigator.clipboard.writeText(value).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    });
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      title={copied ? 'Copied!' : 'Copy to clipboard'}
      aria-label="Copy to clipboard"
      className={`inline-flex shrink-0 items-center text-[var(--color-coffer-muted)] hover:text-[var(--color-coffer-orange)] focus:outline-none ${className}`}
    >
      {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
    </button>
  );
}
