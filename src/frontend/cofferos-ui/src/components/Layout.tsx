import type { ReactNode } from 'react';
import { NavLink } from 'react-router-dom';
import { Landmark, LayoutDashboard, Shield, Wallet } from 'lucide-react';

export function Layout({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-64 shrink-0 border-r border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] p-5 md:block">
        <NavLink to="/" className="mb-8 flex items-center gap-2">
          <div className="grid h-9 w-9 place-items-center rounded-lg bg-[var(--color-coffer-orange)] text-black font-black">
            ₿
          </div>
          <div>
            <div className="text-lg font-bold leading-none">CofferOS</div>
            <div className="text-xs text-[var(--color-coffer-muted)]">Treasury Intelligence</div>
          </div>
        </NavLink>

        <nav className="space-y-1 text-sm">
          <NavLink
            to="/"
            className={({ isActive }: { isActive: boolean }) =>
              isActive
                ? 'flex items-center gap-3 rounded-lg bg-[var(--color-coffer-border)] px-3 py-2 text-white'
                : 'flex items-center gap-3 rounded-lg px-3 py-2 text-[var(--color-coffer-muted)] hover:bg-[var(--color-coffer-border)] hover:text-white'
            }
          >
            <LayoutDashboard size={18} /> Dashboard
          </NavLink>
          <NavLink
            to="/wallets"
            className={({ isActive }: { isActive: boolean }) =>
              isActive
                ? 'flex items-center gap-3 rounded-lg bg-[var(--color-coffer-border)] px-3 py-2 text-white'
                : 'flex items-center gap-3 rounded-lg px-3 py-2 text-[var(--color-coffer-muted)] hover:bg-[var(--color-coffer-border)] hover:text-white'
            }
          >
            <Wallet size={18} /> Wallets
          </NavLink>
          <NavLink
            to="/treasury"
            className={({ isActive }: { isActive: boolean }) =>
              isActive
                ? 'flex items-center gap-3 rounded-lg bg-[var(--color-coffer-border)] px-3 py-2 text-white'
                : 'flex items-center gap-3 rounded-lg px-3 py-2 text-[var(--color-coffer-muted)] hover:bg-[var(--color-coffer-border)] hover:text-white'
            }
          >
            <Landmark size={18} /> Loans
          </NavLink>
        </nav>

        <div className="mt-8 flex items-start gap-2 rounded-lg border border-[var(--color-coffer-border)] p-3 text-xs text-[var(--color-coffer-muted)]">
          <Shield size={16} className="mt-0.5 shrink-0 text-emerald-400" />
          <span>Watch-only. No keys, no seeds, no signing. All data stays on this machine.</span>
        </div>
      </aside>

      <main className="flex-1 overflow-x-hidden">
        <div className="mx-auto max-w-6xl px-6 py-8">{children}</div>
      </main>
    </div>
  );
}
