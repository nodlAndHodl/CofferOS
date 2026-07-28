import { useEffect, useState } from 'react';
import { Coins, Landmark, Lock, Plus, Wallet, Zap, Building2, HardDrive } from 'lucide-react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Holding, HoldingsSummary } from '../types';
import { Button, Card, Spinner } from '../components/ui';
import { formatUsd } from '../lib/format';
import { AddHoldingWizard } from '../components/AddHoldingWizard';

export function HoldingsPage() {
  const [summary, setSummary] = useState<HoldingsSummary | null>(null);
  const [holdings, setHoldings] = useState<Holding[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddHolding, setShowAddHolding] = useState(false);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [s, h] = await Promise.all([api.getHoldingsSummary(), api.listHoldings()]);
      setSummary(s);
      setHoldings(h);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load holdings');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  if (loading) return <Spinner />;

  return (
    <div>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">My Bitcoin Holdings</h1>
          <p className="text-sm text-[var(--color-coffer-muted)]">All Bitcoin ownership in one place</p>
        </div>
        <Button onClick={() => setShowAddHolding(true)}>
          <span className="flex items-center gap-2">
            <Plus size={16} /> Add Holding
          </span>
        </Button>
      </div>

      {error && <div className="mb-6 rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>}

      {/* Total Holdings */}
      <Card className="mb-8 p-6">
        <div className="text-sm text-[var(--color-coffer-muted)] mb-1">Total Holdings</div>
        <div className="text-3xl font-bold">{summary?.totalBitcoin.toFixed(8) ?? '0.00000000'} BTC</div>
        {summary && summary.totalValueUsd > 0 && (
          <div className="text-sm text-[var(--color-coffer-muted)] mt-1">{formatUsd(summary.totalValueUsd)}</div>
        )}
      </Card>

      {/* Breakdown sections */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {/* Wallet Holdings */}
        <HoldingCategoryCard
          icon={<Wallet size={20} />}
          title="Wallet Holdings"
          subtitle={`${holdings.filter(h => h.type === 'Wallet').length} Wallet${holdings.filter(h => h.type === 'Wallet').length !== 1 ? 's' : ''}`}
          btcAmount={summary?.breakdown.find(b => b.category === 'Wallet Holdings')?.bitcoinAmount ?? summary?.availableBitcoin ?? 0}
          valueUsd={summary?.breakdown.find(b => b.category === 'Wallet Holdings')?.valueUsd}
          linkTo="/holdings/wallets"
        />

        {/* Collateral */}
        <HoldingCategoryCard
          icon={<Lock size={20} />}
          title="Collateral"
          subtitle={`${holdings.filter(h => h.type === 'LoanCollateral').length} Loan${holdings.filter(h => h.type === 'LoanCollateral').length !== 1 ? 's' : ''}`}
          btcAmount={summary?.collateralBitcoin ?? 0}
          valueUsd={summary?.breakdown.find(b => b.category === 'Collateral')?.valueUsd}
          linkTo="/treasury"
        />

        {/* Lightning - Coming Soon */}
        <HoldingCategoryCard
          icon={<Zap size={20} />}
          title="Lightning"
          subtitle="Coming Soon"
          btcAmount={0}
          comingSoon
        />

        {/* Retirement - Coming Soon */}
        <HoldingCategoryCard
          icon={<Building2 size={20} />}
          title="Retirement"
          subtitle="Coming Soon"
          btcAmount={0}
          comingSoon
        />

        {/* ETF Holdings - Coming Soon */}
        <HoldingCategoryCard
          icon={<Landmark size={20} />}
          title="ETF Holdings"
          subtitle="Coming Soon"
          btcAmount={0}
          comingSoon
        />

        {/* Mining - Coming Soon */}
        <HoldingCategoryCard
          icon={<HardDrive size={20} />}
          title="Mining"
          subtitle="Coming Soon"
          btcAmount={0}
          comingSoon
        />
      </div>

      {/* Individual holdings list */}
      {holdings.length > 0 && (
        <div className="mt-8">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">All Holdings</h2>
          <div className="grid gap-3">
            {holdings.map((h) => (
              <Link
                key={`${h.type}-${h.id}`}
                to={h.type === 'Wallet' ? `/wallets/${h.id}` : `/treasury/${h.id}`}
              >
                <Card className="p-4 transition hover:border-[var(--color-coffer-orange)]/50">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="grid h-8 w-8 place-items-center rounded-lg bg-[var(--color-coffer-border)]">
                        {h.type === 'Wallet' ? <Wallet size={16} /> : <Lock size={16} />}
                      </div>
                      <div>
                        <div className="font-semibold">{h.name}</div>
                        <div className="text-xs text-[var(--color-coffer-muted)]">
                          {h.type === 'Wallet' ? 'Self-Custody Wallet' : 'Loan Collateral'}
                          {h.institution && ` · ${h.institution}`}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="font-bold">{h.bitcoinAmount.toFixed(8)} BTC</div>
                      {h.valueUsd > 0 && (
                        <div className="text-xs text-[var(--color-coffer-muted)]">{formatUsd(h.valueUsd)}</div>
                      )}
                    </div>
                  </div>
                </Card>
              </Link>
            ))}
          </div>
        </div>
      )}

      {holdings.length === 0 && !loading && (
        <Card className="mt-8 p-10 text-center">
          <Coins className="mx-auto mb-3 text-[var(--color-coffer-muted)]" />
          <p className="mb-4 text-[var(--color-coffer-muted)]">No holdings yet. Add a wallet or loan to get started.</p>
          <Button onClick={() => setShowAddHolding(true)}>Add your first holding</Button>
        </Card>
      )}

      {showAddHolding && <AddHoldingWizard onClose={() => setShowAddHolding(false)} onComplete={load} />}
    </div>
  );
}

function HoldingCategoryCard({
  icon,
  title,
  subtitle,
  btcAmount,
  valueUsd,
  linkTo,
  comingSoon,
}: {
  icon: React.ReactNode;
  title: string;
  subtitle: string;
  btcAmount: number;
  valueUsd?: number;
  linkTo?: string;
  comingSoon?: boolean;
}) {
  const content = (
    <Card className={`p-4 ${comingSoon ? 'opacity-50' : 'transition hover:border-[var(--color-coffer-orange)]/50'}`}>
      <div className="mb-3 flex items-center gap-3">
        <div className="grid h-9 w-9 place-items-center rounded-lg bg-[var(--color-coffer-border)] text-[var(--color-coffer-muted)]">
          {icon}
        </div>
        <div>
          <div className="font-semibold text-sm">{title}</div>
          <div className="text-xs text-[var(--color-coffer-muted)]">{subtitle}</div>
        </div>
      </div>
      <div className="text-xl font-bold">
        {comingSoon ? '—' : `${btcAmount.toFixed(8)} BTC`}
      </div>
      {valueUsd != null && valueUsd > 0 && !comingSoon && (
        <div className="text-xs text-[var(--color-coffer-muted)] mt-1">{formatUsd(valueUsd)}</div>
      )}
    </Card>
  );

  if (linkTo && !comingSoon) {
    return <Link to={linkTo}>{content}</Link>;
  }
  return content;
}
