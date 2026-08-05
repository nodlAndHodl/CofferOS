import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Landmark, Plus, Trash2 } from 'lucide-react';
import { api } from '../api/client';
import type { LoanSummary, TreasurySummary } from '../types';
import { Badge, Button, Card, Spinner } from '../components/ui';
import { formatPercent, formatUsd } from '../lib/format';
import { CreateLoanModal } from '../components/CreateLoanModal';

export function TreasuryPage() {
  const [summary, setSummary] = useState<TreasurySummary | null>(null);
  const [loans, setLoans] = useState<LoanSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [s, list] = await Promise.all([api.getTreasurySummary(), api.listLoans()]);
      setSummary(s);
      setLoans(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load treasury');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function handleDelete(id: string, name: string) {
    if (!confirm(`Delete loan "${name}"? This cannot be undone.`)) return;
    try {
      await api.deleteLoan(id);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to delete');
    }
  }

  if (loading) return <Spinner />;

  const hasLoans = loans.length > 0;

  return (
    <div>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Treasury</h1>
          <p className="text-sm text-[var(--color-coffer-muted)]">Bitcoin-collateralized loans and liabilities</p>
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <span className="flex items-center gap-2">
            <Plus size={16} /> New loan
          </span>
        </Button>
      </div>

      {error && <div className="mb-6 rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>}

      {/* Summary cards */}
      <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <Stat label="Active loans" value={String(summary?.activeLoanCount ?? 0)} />
        <Stat label="Total balance" value={formatUsd(summary?.totalLoanBalance ?? 0)} />
        <Stat label="Total collateral" value={`${(summary?.totalCollateralBtc ?? 0).toFixed(4)} BTC`} />
        <Stat label="Collateral value" value={formatUsd(summary?.totalCollateralValue ?? 0)} />
        <Stat label="Avg LTV" value={formatPercent(summary?.averageLtv ?? 0)} />
      </div>

      {summary?.highestRiskLoan && (
        <div className="mb-8">
          <h2 className="mb-2 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Highest risk loan</h2>
          <Card className="p-4">
            <div className="flex items-center justify-between">
              <div>
                <div className="font-semibold">{summary.highestRiskLoan.name}</div>
                <div className="text-xs text-[var(--color-coffer-muted)]">
                  Balance {formatUsd(summary.highestRiskLoan.currentBalance)} · LTV {formatPercent(summary.highestRiskLoan.currentLtv)}
                </div>
              </div>
              <Link to={`/treasury/${summary.highestRiskLoan.id}`}>
                <Button variant="ghost">View</Button>
              </Link>
            </div>
          </Card>
        </div>
      )}

      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Loans</h2>
        {summary?.currentBtcPrice && (
          <div className="text-xs text-[var(--color-coffer-muted)]">
            BTC price: {formatUsd(summary.currentBtcPrice)} ({summary.priceProvider})
          </div>
        )}
      </div>

      {hasLoans ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {loans.map((l) => (
            <Link key={l.id} to={`/treasury/${l.id}`}>
              <Card className="relative p-4 transition hover:border-[var(--color-coffer-orange)]/50">
                <div className="mb-2 flex items-center justify-between">
                  <span className="font-semibold">{l.name}</span>
                  <div className="flex items-center gap-2">
                    <Badge tone={l.status === 'Active' ? 'orange' : 'default'}>{l.status}</Badge>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        handleDelete(l.id, l.name);
                      }}
                      className="rounded p-2 text-[var(--color-coffer-muted)] hover:text-red-400"
                      aria-label="Delete loan"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </div>
                <div className="text-xl font-bold">{formatUsd(l.currentBalance)}</div>
                <div className="mt-2 grid grid-cols-2 gap-2 text-xs text-[var(--color-coffer-muted)]">
                  <div>Collateral: {(l.collateralAmountBtc).toFixed(4)} BTC</div>
                  <div>Value: {formatUsd(l.currentCollateralValue)}</div>
                  <div>LTV: <span className={l.currentLtv >= l.liquidationLtv ? 'text-red-400' : l.currentLtv >= l.warningLtv ? 'text-yellow-400' : ''}>{formatPercent(l.currentLtv)}</span></div>
                  <div>Buffer: {formatPercent(Math.max(0, l.distanceToWarning))}</div>
                </div>
                {l.lender && <div className="mt-1 text-xs text-[var(--color-coffer-muted)]">Lender: {l.lender}</div>}
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <Card className="p-10 text-center">
          <Landmark className="mx-auto mb-3 text-[var(--color-coffer-muted)]" />
          <p className="mb-4 text-[var(--color-coffer-muted)]">No loans yet. Track your Bitcoin-backed loans manually.</p>
          <Button onClick={() => setShowCreate(true)}>Create your first loan</Button>
        </Card>
      )}

      {showCreate && <CreateLoanModal onClose={() => setShowCreate(false)} onCreated={load} />}
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <Card className="p-4">
      <div className="mb-1 text-xs font-medium uppercase tracking-wide text-[var(--color-coffer-muted)]">{label}</div>
      <div className="text-xl font-bold">{value}</div>
    </Card>
  );
}
