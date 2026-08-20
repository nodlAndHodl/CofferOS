import { useEffect, useState } from 'react';
import { Building2, ChevronLeft, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { RetirementAccount } from '../types';
import { Button, Card, Spinner } from '../components/ui';
import { formatForDisplay } from '../lib/currency';
import { useBitcoinPrice } from '../hooks/useBitcoinPrice';
import { useUserSettings } from '../contexts/UserSettingsContext';
import { CreateRetirementAccountModal } from '../components/CreateRetirementAccountModal';

export function RetirementAccountsPage() {
  const [accounts, setAccounts] = useState<RetirementAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const { settings } = useUserSettings();
  const { exchangeRates } = useBitcoinPrice();
  const displayCurrency = settings.currency;
  const fmt = (value: number, valueCurrency: string) => formatForDisplay(value, valueCurrency, displayCurrency, exchangeRates);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await api.listRetirementAccounts();
      setAccounts(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load retirement accounts');
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
      <div className="mb-6 flex items-center gap-3">
        <Link to="/holdings" className="text-[var(--color-coffer-muted)] hover:text-white">
          <ChevronLeft size={20} />
        </Link>
        <h1 className="text-2xl font-bold">Retirement Accounts</h1>
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</div>}

      <div className="mb-6 flex justify-end">
        <Button onClick={() => setShowCreate(true)}>
          <span className="flex items-center gap-2">
            <Plus size={16} /> Add Account
          </span>
        </Button>
      </div>

      {accounts.length === 0 && !loading && (
        <Card className="p-10 text-center">
          <Building2 className="mx-auto mb-3 text-[var(--color-coffer-muted)]" size={32} />
          <p className="mb-4 text-[var(--color-coffer-muted)]">No retirement accounts yet.</p>
          <Button onClick={() => setShowCreate(true)}>Add your first retirement account</Button>
        </Card>
      )}

      {accounts.length > 0 && (
        <div className="grid gap-3">
          {accounts.map((account) => (
            <Link key={account.id} to={`/holdings/retirement/${account.id}`}>
              <Card className="p-4 transition hover:border-[var(--color-coffer-orange)]/50">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="grid h-8 w-8 place-items-center rounded-lg bg-[var(--color-coffer-border)]">
                      <Building2 size={16} />
                    </div>
                    <div>
                      <div className="font-semibold">{account.name}</div>
                      <div className="text-xs text-[var(--color-coffer-muted)]">
                        {account.accountType} · {account.provider}
                      </div>
                    </div>
                  </div>
                  <div className="text-right">
                    <div className="font-bold">{account.bitcoinAmount.toFixed(8)} BTC</div>
                    <div className="text-xs text-[var(--color-coffer-muted)]">
                      Cost basis {fmt(account.totalCostBasis, account.currency ?? 'USD')}
                    </div>
                  </div>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      )}

      {showCreate && (
        <CreateRetirementAccountModal
          onClose={() => setShowCreate(false)}
          onCreated={() => {
            load();
            setShowCreate(false);
          }}
        />
      )}
    </div>
  );
}
