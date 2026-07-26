import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Plus, Trash2, Wallet as WalletIcon } from 'lucide-react';
import { api } from '../api/client';
import type { WalletSummary } from '../types';
import { Badge, Button, Card, Spinner } from '../components/ui';
import { ImportWalletModal } from '../components/ImportWalletModal';
import { formatBtc } from '../lib/format';
import { useWalletNotifications } from '../hooks/useWalletNotifications';

export function WalletsPage() {
  const [wallets, setWallets] = useState<WalletSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showImport, setShowImport] = useState(false);
  const [rescanningWallets, setRescanningWallets] = useState<Set<string>>(new Set());

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const list = await api.listWallets();
      setWallets(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load wallets');
    } finally {
      setLoading(false);
    }
  }

  useWalletNotifications({
    onRescanStarted: (walletId: string) => {
      console.log('Rescan started for wallet:', walletId);
      setRescanningWallets((prev) => new Set(prev).add(walletId));
    },
    onRescanCompleted: (walletId: string) => {
      console.log('Rescan completed for wallet:', walletId);
      setRescanningWallets((prev) => {
        const next = new Set(prev);
        next.delete(walletId);
        return next;
      });
      console.log('Reloading wallet list...');
      load();
    },
    onRescanFailed: (walletId: string, error: string) => {
      console.log('Rescan failed for wallet:', walletId, 'Error:', error);
      setRescanningWallets((prev) => {
        const next = new Set(prev);
        next.delete(walletId);
        return next;
      });
      setError(`Rescan failed for wallet: ${error}`);
    },
  });

  useEffect(() => {
    load();
  }, []);

  if (loading) return <Spinner />;

  return (
    <div>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Wallets</h1>
          <p className="text-sm text-[var(--color-coffer-muted)]">Manage your watch-only wallets</p>
        </div>
        <Button onClick={() => setShowImport(true)}>
          <span className="flex items-center gap-2">
            <Plus size={16} /> Import wallet
          </span>
        </Button>
      </div>

      {error && <div className="mb-6 rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>}

      {wallets.length > 0 ? (
        <div className="grid gap-3 sm:grid-cols-2">
          {wallets.map((w) => (
            <Link key={w.id} to={`/wallets/${w.id}`}>
              <Card className="relative p-4 transition hover:border-[var(--color-coffer-orange)]/50">
                <div className="mb-2 flex items-center justify-between">
                  <span className="font-semibold">{w.name}</span>
                  <div className="flex items-center gap-2">
                    <Badge tone="orange">{w.network}</Badge>
                    {rescanningWallets.has(w.id) && <Badge tone="default">rescanning...</Badge>}
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        if (confirm(`Delete "${w.name}"? This cannot be undone.`)) {
                          api.deleteWallet(w.id).then(load).catch((e) => setError(e instanceof Error ? e.message : 'Failed to delete wallet'));
                        }
                      }}
                      className="rounded p-2 text-[var(--color-coffer-muted)] hover:text-red-400"
                      aria-label="Delete wallet"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                </div>
                <div className="text-xl font-bold">{formatBtc(w.balance.totalSats)}</div>
                <div className="mt-2 flex items-center gap-3 text-xs text-[var(--color-coffer-muted)]">
                  <span>{w.descriptorCount} descriptor(s)</span>
                  <span>{w.transactionCount} tx</span>
                  {w.watchOnly && <Badge tone="green">watch-only</Badge>}
                </div>
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

      {showImport && <ImportWalletModal onClose={() => setShowImport(false)} onImported={load} />}
    </div>
  );
}
