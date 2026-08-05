import { useEffect, useState } from 'react';
import { Coins, Landmark, Lock, Plus, Wallet, Zap, Building2, HardDrive } from 'lucide-react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { Holding, HoldingsSummary } from '../types';
import { Button, Card, Spinner } from '../components/ui';
import { formatFiat, formatPercent } from '../lib/format';
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
        {summary && summary.totalValue > 0 && (
          <div className="grid gap-1 mt-1 text-sm text-[var(--color-coffer-muted)] sm:grid-cols-3">
            <div>Value {formatFiat(summary.totalValue)}</div>
            <div>Cost Basis {formatFiat(summary.totalCostBasis)}</div>
            <div className={summary.unrealizedPnl >= 0 ? 'text-green-400' : 'text-red-400'}>
              P&L {formatFiat(summary.unrealizedPnl)} ({formatPercent(summary.unrealizedPnlPercent)})
            </div>
          </div>
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
          valueUsd={summary?.breakdown.find(b => b.category === 'Wallet Holdings')?.value}
          costBasis={summary?.breakdown.find(b => b.category === 'Wallet Holdings')?.costBasis}
          unrealizedPnl={summary?.breakdown.find(b => b.category === 'Wallet Holdings')?.unrealizedPnl}
          linkTo="/wallets"
        />

        {/* Collateral */}
        <HoldingCategoryCard
          icon={<Lock size={20} />}
          title="Collateral"
          subtitle={`${holdings.filter(h => h.type === 'LoanCollateral').length} Loan${holdings.filter(h => h.type === 'LoanCollateral').length !== 1 ? 's' : ''}`}
          btcAmount={summary?.collateralBitcoin ?? 0}
          valueUsd={summary?.breakdown.find(b => b.category === 'Collateral')?.value}
          costBasis={summary?.breakdown.find(b => b.category === 'Collateral')?.costBasis}
          unrealizedPnl={summary?.breakdown.find(b => b.category === 'Collateral')?.unrealizedPnl}
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

        {/* Retirement Accounts */}
        <HoldingCategoryCard
          icon={<Building2 size={20} />}
          title="Retirement Accounts"
          subtitle={`${holdings.filter(h => h.type === 'Retirement').length} Account${holdings.filter(h => h.type === 'Retirement').length !== 1 ? 's' : ''}`}
          btcAmount={summary?.breakdown.find(b => b.category === 'Retirement Accounts')?.bitcoinAmount ?? 0}
          valueUsd={summary?.breakdown.find(b => b.category === 'Retirement Accounts')?.value}
          costBasis={summary?.breakdown.find(b => b.category === 'Retirement Accounts')?.costBasis}
          unrealizedPnl={summary?.breakdown.find(b => b.category === 'Retirement Accounts')?.unrealizedPnl}
          linkTo="/holdings/retirement"
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
            {holdings.map((h) => {
              const getLink = () => {
                if (h.type === 'Wallet') return `/wallets/${h.id}`;
                if (h.type === 'Retirement') return `/holdings/retirement/${h.id}`;
                return `/treasury/${h.id}`;
              };

              const getIcon = () => {
                if (h.type === 'Wallet') return <Wallet size={16} />;
                if (h.type === 'Retirement') return <Building2 size={16} />;
                return <Lock size={16} />;
              };

              const getTypeLabel = () => {
                if (h.type === 'Wallet') return 'Self-Custody Wallet';
                if (h.type === 'Retirement') return 'Retirement Account';
                return 'Loan Collateral';
              };

              return (
                <Link key={`${h.type}-${h.id}`} to={getLink()}>
                  <Card className="p-4 transition hover:border-[var(--color-coffer-orange)]/50">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="grid h-8 w-8 place-items-center rounded-lg bg-[var(--color-coffer-border)]">
                          {getIcon()}
                        </div>
                        <div>
                          <div className="font-semibold">{h.name}</div>
                          <div className="text-xs text-[var(--color-coffer-muted)]">
                            {getTypeLabel()}
                            {h.institution && ` · ${h.institution}`}
                          </div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="font-bold">{h.bitcoinAmount.toFixed(8)} BTC</div>
                        {h.value > 0 && (
                          <div className="text-xs text-[var(--color-coffer-muted)]">{formatFiat(h.value)}</div>
                        )}
                      </div>
                    </div>
                  </Card>
                </Link>
              );
            })}
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
  costBasis,
  unrealizedPnl,
  linkTo,
  comingSoon,
}: {
  icon: React.ReactNode;
  title: string;
  subtitle: string;
  btcAmount: number;
  valueUsd?: number;
  costBasis?: number;
  unrealizedPnl?: number;
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
        <div className="grid gap-1 text-xs sm:grid-cols-2">
          <span className="text-[var(--color-coffer-muted)]">{formatFiat(valueUsd)} value</span>
          <span className="text-[var(--color-coffer-muted)]">{formatFiat(costBasis ?? 0)} basis</span>
          <span className={unrealizedPnl && unrealizedPnl >= 0 ? 'text-green-400' : 'text-red-400'}>
            {unrealizedPnl !== undefined ? formatFiat(unrealizedPnl) : '—'} P&L
          </span>
        </div>
      )}
    </Card>
  );

  if (linkTo && !comingSoon) {
    return <Link to={linkTo}>{content}</Link>;
  }
  return content;
}
