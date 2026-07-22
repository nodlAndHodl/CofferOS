import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Activity, Plus, Server, Trash2, Wallet as WalletIcon, Zap } from 'lucide-react';
import { api } from '../api/client';
import type { Dashboard, ElectrumStatus, NodeStatus } from '../types';
import { Badge, Button, Card, Spinner } from '../components/ui';
import { ImportWalletModal } from '../components/ImportWalletModal';
import { formatBtc, formatDate, shorten } from '../lib/format';

export function DashboardPage() {
  const [data, setData] = useState<Dashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [node, setNode] = useState<NodeStatus | null>(null);
  const [nodeLoading, setNodeLoading] = useState(true);
  const [electrum, setElectrum] = useState<ElectrumStatus | null>(null);
  const [electrumLoading, setElectrumLoading] = useState(true);
  const [showImport, setShowImport] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      setError(null);
      setData(await api.getDashboard());
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function loadNode() {
    setNodeLoading(true);
    try {
      setNode(await api.getNodeStatus());
    } catch (e) {
      setNode({ connected: false, providerId: 'none', error: e instanceof Error ? e.message : 'Failed to load node status' });
    } finally {
      setNodeLoading(false);
    }
  }

  async function loadElectrum() {
    setElectrumLoading(true);
    try {
      setElectrum(await api.getElectrumStatus());
    } catch (e) {
      setElectrum({ connected: false, providerId: 'electrum', host: '', port: 0, error: e instanceof Error ? e.message : 'Failed to load electrum status' });
    } finally {
      setElectrumLoading(false);
    }
  }

  useEffect(() => {
    loadNode();
  }, []);

  useEffect(() => {
    loadElectrum();
  }, []);

  if (loading) return <Spinner />;

  return (
    <div>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Dashboard</h1>
          <p className="text-sm text-[var(--color-coffer-muted)]">Your Bitcoin treasury at a glance</p>
        </div>
        <Button onClick={() => setShowImport(true)}>
          <span className="flex items-center gap-2">
            <Plus size={16} /> Import wallet
          </span>
        </Button>
      </div>

      {error && <div className="mb-6 rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>}

      <div className="mb-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard icon={<WalletIcon size={18} />} label="Total balance" value={formatBtc(data?.totalBalance.totalSats ?? 0)} />
        <StatCard icon={<Activity size={18} />} label="Wallets" value={String(data?.walletCount ?? 0)} />
        <NodeCard node={node} loading={nodeLoading} />
        <ElectrumCard electrum={electrum} loading={electrumLoading} />
      </div>

      <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Wallets</h2>
      {data && data.wallets.length > 0 ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {data.wallets.map((w) => (
            <Link key={w.id} to={`/wallets/${w.id}`}>
              <Card className="relative p-4 transition hover:border-[var(--color-coffer-orange)]/50">
                <div className="mb-2 flex items-center justify-between">
                  <span className="font-semibold">{w.name}</span>
                  <Badge tone="orange">{w.network}</Badge>
                </div>
                <div className="text-xl font-bold">{formatBtc(w.balance.totalSats)}</div>
                <div className="mt-2 flex items-center gap-3 text-xs text-[var(--color-coffer-muted)]">
                  <span>{w.descriptorCount} descriptor(s)</span>
                  <span>{w.transactionCount} tx</span>
                  {w.watchOnly && <Badge tone="green">watch-only</Badge>}
                </div>
                <button
                  onClick={(e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    if (confirm(`Delete "${w.name}"? This cannot be undone.`)) {
                      api.deleteWallet(w.id).then(load).catch((e) => setError(e instanceof Error ? e.message : 'Failed to delete wallet'));
                    }
                  }}
                  className="absolute right-2 top-2 rounded p-1 text-[var(--color-coffer-muted)] hover:text-red-400"
                  aria-label="Delete wallet"
                >
                  <Trash2 size={16} />
                </button>
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <Card className="p-10 text-center">
          <WalletIcon className="mx-auto mb-3 text-[var(--color-coffer-muted)]" />
          <p className="mb-4 text-[var(--color-coffer-muted)]">No wallets yet. Import an xpub or descriptor to get started.</p>
          <Button onClick={() => setShowImport(true)}>Import your first wallet</Button>
        </Card>
      )}

      <h2 className="mb-3 mt-8 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Recent activity</h2>
      <Card className="p-4">
        {data && data.recentActivity.length > 0 ? (
          <ul className="divide-y divide-[var(--color-coffer-border)]">
            {data.recentActivity.map((t) => (
              <li key={t.txId} className="flex items-center justify-between py-2 text-sm">
                <span className="font-mono text-xs">{shorten(t.txId)}</span>
                <span>
                  {formatBtc(t.netAmountSats)} <span className="text-[var(--color-coffer-muted)]">{t.direction}</span>
                </span>
                <span className="text-[var(--color-coffer-muted)]">{formatDate(t.timestamp)}</span>
              </li>
            ))}
          </ul>
        ) : (
          <p className="py-4 text-center text-sm text-[var(--color-coffer-muted)]">
            No activity yet. Connect a node to sync transactions.
          </p>
        )}
      </Card>

      {showImport && <ImportWalletModal onClose={() => setShowImport(false)} onImported={load} />}
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

function NodeCard({ node, loading }: { node: NodeStatus | null; loading: boolean }) {
  return (
    <Card className="p-4">
      <div className="mb-2 flex items-center gap-2 text-[var(--color-coffer-muted)]">
        <Server size={18} />
        <span className="text-xs font-medium uppercase tracking-wide">Node</span>
      </div>
      {loading || node === null ? (
        <div className="text-sm text-[var(--color-coffer-muted)]">Checking...</div>
      ) : (
        <div className="flex items-center gap-2">
          <span className={`h-2 w-2 rounded-full ${node.connected ? 'bg-emerald-400' : 'bg-red-400'}`} />
          <span className="text-sm">
            {node.connected ? `${node.chain} · ${node.blocks?.toLocaleString()} blocks` : (node.error ?? 'Not connected')}
          </span>
        </div>
      )}
    </Card>
  );
}

function ElectrumCard({ electrum, loading }: { electrum: ElectrumStatus | null; loading: boolean }) {
  return (
    <Card className="p-4">
      <div className="mb-2 flex items-center gap-2 text-[var(--color-coffer-muted)]">
        <Zap size={18} />
        <span className="text-xs font-medium uppercase tracking-wide">Electrum</span>
      </div>
      {loading || electrum === null ? (
        <div className="text-sm text-[var(--color-coffer-muted)]">Checking...</div>
      ) : (
        <div className="flex items-center gap-2">
          <span className={`h-2 w-2 rounded-full ${electrum.connected ? 'bg-emerald-400' : 'bg-red-400'}`} />
          <span className="text-sm">
            {electrum.connected
              ? `${electrum.blockHeight?.toLocaleString()} blocks`
              : (electrum.error ?? 'Not connected')}
          </span>
        </div>
      )}
    </Card>
  );
}
