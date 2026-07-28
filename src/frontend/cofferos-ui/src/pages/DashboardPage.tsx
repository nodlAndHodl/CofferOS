import { useEffect, useState } from 'react';
import { Activity, Landmark, Wallet as WalletIcon } from 'lucide-react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { DashboardOverview, RecentActivityPage, RecentActivityItem } from '../types';
import { Badge, Card, CopyButton, Spinner } from '../components/ui';
import { Tooltip } from '../components/Tooltip';
import { formatBtc, formatDate, formatPercent, formatUsd, shorten } from '../lib/format';
import { getTagColorClass } from '../lib/tagColor';

function normalizeActivity(raw: unknown): RecentActivityPage | null {
  const page = raw as any;
  if (!page) return null;

  const normalizeItem = (t: any): RecentActivityItem => ({
    txId: String(t.txId ?? ''),
    netAmountSats: Number(t.netAmountSats ?? 0),
    blockHeight: t.blockHeight ?? null,
    timestamp: t.timestamp ?? null,
    walletName: t.walletName ?? 'Unknown',
    label: t.label ?? null,
    tags: Array.isArray(t.tags) ? t.tags : [],
  });

  if (Array.isArray(page)) {
    return {
      skip: 0,
      take: page.length,
      total: page.length,
      items: page.map(normalizeItem),
    };
  }

  if (Array.isArray(page.items)) {
    return {
      skip: typeof page.skip === 'number' ? page.skip : 0,
      take: typeof page.take === 'number' ? page.take : page.items.length,
      total: typeof page.total === 'number' ? page.total : page.items.length,
      items: page.items.map(normalizeItem),
    };
  }

  return null;
}

export function DashboardPage() {
  const [overview, setOverview] = useState<DashboardOverview | null>(null);
  const [activity, setActivity] = useState<RecentActivityPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadOverview() {
    try {
      setError(null);
      // Single merged call for holdings + treasury + wallet summary + recent activity
      const o = await api.getDashboardOverview();
      setOverview(o);
      setActivity(normalizeActivity(o.recentActivity));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadOverview();
  }, []);

  if (loading) return <Spinner />;

  return (
    <div>
      <div className="mb-8">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-sm text-[var(--color-coffer-muted)]">Your Bitcoin treasury at a glance</p>
      </div>

      {error && <div className="mb-6 rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>}

      {/* Bitcoin Holdings (from merged overview) */}
      {overview && (
        <div className="mb-8">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Bitcoin Holdings</h2>
            <Link to="/holdings" className="text-xs text-[var(--color-coffer-muted)] hover:text-[var(--color-coffer-orange)]">View holdings →</Link>
          </div>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard icon={<WalletIcon size={18} />} label="Total Bitcoin" value={`${overview.totalBitcoin.toFixed(8)} BTC`} />
            <StatCard icon={<Activity size={18} />} label="Available Bitcoin" value={`${overview.availableBitcoin.toFixed(8)} BTC`} />
            <StatCard icon={<Landmark size={18} />} label="Locked as Collateral" value={`${overview.collateralBitcoin.toFixed(8)} BTC`} />
            <StatCard icon={<WalletIcon size={18} />} label="Current USD Value" value={formatUsd(overview.totalValueUsd)} />
          </div>
        </div>
      )}

      {/* Treasury summary */}
      {overview && (
        <>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Treasury</h2>
            <Link to="/treasury" className="text-xs text-[var(--color-coffer-muted)] hover:text-[var(--color-coffer-orange)]">View Treasury →</Link>
          </div>
          <div className="mb-8">
            {overview.activeLoanCount > 0 ? (
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
                <StatCard icon={<Landmark size={18} />} label="Active loans" value={String(overview.activeLoanCount)} />
                <StatCard icon={<Landmark size={18} />} label="Total loan balance" value={formatUsd(overview.outstandingLoanBalanceUsd)} />
                <StatCard icon={<Landmark size={18} />} label="Total collateral" value={`${overview.collateralBitcoin.toFixed(4)} BTC`} />
                <StatCard icon={<Landmark size={18} />} label="Collateral value" value={formatUsd(overview.collateralBitcoin * overview.bitcoinPriceUsd)} />
                <StatCard icon={<Landmark size={18} />} label="Avg LTV" value={formatPercent(overview.weightedAverageLtv)} />
              </div>
            ) : (
              <Card className="p-4 text-sm text-[var(--color-coffer-muted)]">
                No active loans. <Link to="/treasury" className="text-[var(--color-coffer-orange)] hover:underline">Create one</Link> to start tracking Bitcoin-backed loans.
              </Card>
            )}
            {overview.highestRiskLoan && (
              <div className="mt-2 text-xs text-[var(--color-coffer-muted)]">
                Highest risk: <Link to={`/treasury/${overview.highestRiskLoan.id}`} className="text-[var(--color-coffer-orange)] hover:underline">{overview.highestRiskLoan.name}</Link> — LTV {formatPercent(overview.highestRiskLoan.currentLtv)}
              </div>
            )}
          </div>
        </>
      )}

      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Recent activity</h2>
      <Card className="p-4">
        {activity && activity.items.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
                <tr className="border-b border-[var(--color-coffer-border)]">
                  <th className="px-2 py-2">Transaction</th>
                  <th className="px-2 py-2">Amount</th>
                  <th className="px-2 py-2">Wallet</th>
                  <th className="px-2 py-2">Label / Tags</th>
                  <th className="px-2 py-2">Block</th>
                  <th className="px-2 py-2">Time</th>
                </tr>
              </thead>
              <tbody>
                {activity.items.map((t) => (
                  <tr key={t.txId} className="border-b border-[var(--color-coffer-border)]/50">
                    <td className="px-2 py-2 font-mono text-xs">
                      <span className="inline-flex items-center gap-1">
                        <Tooltip content={t.txId}>
                          <span>{shorten(t.txId)}</span>
                        </Tooltip>
                        <CopyButton value={t.txId} />
                      </span>
                    </td>
                    <td className="px-2 py-2">
                      <span className={t.netAmountSats >= 0 ? 'text-green-400' : 'text-red-400'}>
                        {formatBtc(t.netAmountSats)}
                      </span>
                    </td>
                    <td className="px-2 py-2">
                      <Badge tone="orange">{t.walletName}</Badge>
                    </td>
                    <td className="px-2 py-2">
                      <div className="flex flex-wrap gap-1">
                        {t.label && <Badge tone="blue">{t.label}</Badge>}
                        {t.tags.map((tag) => (
                          <Badge key={tag} className={getTagColorClass(tag)}>
                            {tag}
                          </Badge>
                        ))}
                      </div>
                    </td>
                    <td className="px-2 py-2">{t.blockHeight ?? '—'}</td>
                    <td className="px-2 py-2 text-[var(--color-coffer-muted)]">{formatDate(t.timestamp)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {activity.total > activity.take && (
              <div className="mt-3 flex items-center justify-end gap-2 text-xs text-[var(--color-coffer-muted)]">
                {(() => {
                  const pageSize = activity.take;
                  const currentPage = pageSize > 0 ? activity.skip / pageSize + 1 : 1;
                  const totalPages = Math.ceil(activity.total / pageSize);
                  function goToPage(p: number) {
                    const next = Math.min(Math.max(p, 1), totalPages);
                    api.getRecentActivity(next, pageSize)
                      .then((page) => setActivity(normalizeActivity(page)))
                      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load activity'));
                  }
                  return (
                    <>
                      <button
                        disabled={currentPage <= 1}
                        onClick={() => goToPage(currentPage - 1)}
                        className="rounded border border-[var(--color-coffer-border)] px-2 py-1 disabled:opacity-40"
                      >
                        Previous
                      </button>
                      <span>
                        Page {currentPage} of {totalPages}
                      </span>
                      <button
                        disabled={currentPage >= totalPages}
                        onClick={() => goToPage(currentPage + 1)}
                        className="rounded border border-[var(--color-coffer-border)] px-2 py-1 disabled:opacity-40"
                      >
                        Next
                      </button>
                    </>
                  );
                })()}
              </div>
            )}
          </div>
        ) : (
          <p className="py-4 text-center text-sm text-[var(--color-coffer-muted)]">
            No activity yet. Connect a node to sync transactions.
          </p>
        )}
      </Card>

    </div>
  );
}

function StatCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return (
    <Card className="p-4">
      <div className="mb-2 flex items-center gap-2 text-[var(--color-coffer-muted)]">
        {icon}
        <span className="text-xs font-medium uppercase tracking-wide">{label}</span>
      </div>
      <div className="text-xl font-bold">{value}</div>
    </Card>
  );
}

