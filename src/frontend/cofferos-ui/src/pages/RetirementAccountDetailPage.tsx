import { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Building2, Calendar, ChevronLeft, Plus, Trash2, X } from 'lucide-react';
import { api } from '../api/client';
import type { CostBasisEntryInput, RetirementAccount, RetirementAccountType } from '../types';
import { Button, Card, Spinner } from '../components/ui';
import { formatForDisplay, SUPPORTED_CURRENCIES } from '../lib/currency';
import { useBitcoinPrice } from '../hooks/useBitcoinPrice';
import { useUserSettings } from '../contexts/UserSettingsContext';

const ACCOUNT_TYPES: { value: RetirementAccountType; label: string }[] = [
  { value: 'TraditionalIra', label: 'Traditional IRA' },
  { value: 'RothIra', label: 'Roth IRA' },
  { value: 'SepIra', label: 'SEP IRA' },
  { value: 'SimpleIra', label: 'SIMPLE IRA' },
  { value: 'Solo401k', label: 'Solo 401(k)' },
  { value: 'Traditional401k', label: 'Traditional 401(k)' },
  { value: 'Roth401k', label: 'Roth 401(k)' },
  { value: 'Other', label: 'Other' },
];

export function RetirementAccountDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [account, setAccount] = useState<RetirementAccount | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { settings } = useUserSettings();
  const { exchangeRates } = useBitcoinPrice();
  const displayCurrency = settings.currency;
  const fmt = (value: number, valueCurrency: string) => formatForDisplay(value, valueCurrency, displayCurrency, exchangeRates);

  const [name, setName] = useState('');
  const [provider, setProvider] = useState('');
  const [bitcoinAmount, setBitcoinAmount] = useState('');
  const [currency, setCurrency] = useState(settings.currency);
  const [notes, setNotes] = useState('');

  const [newCostBasis, setNewCostBasis] = useState('');
  const [newAcquisitionDate, setNewAcquisitionDate] = useState(new Date().toISOString().slice(0, 10));
  const newDateInputRef = useRef<HTMLInputElement>(null);

  async function load() {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const data = await api.getRetirementAccount(id);
      setAccount(data);
      setName(data.name);
      setProvider(data.provider);
      setBitcoinAmount(data.bitcoinAmount.toString());
      setCurrency(data.currency);
      setNotes(data.notes ?? '');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load retirement account');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [id]);

  async function handleUpdate() {
    if (!id || !account) return;
    if (!name.trim() || !provider.trim() || !bitcoinAmount) {
      setError('Please fill in all required fields');
      return;
    }
    const btcAmount = parseFloat(bitcoinAmount);
    if (isNaN(btcAmount) || btcAmount < 0) {
      setError('Bitcoin amount must be a valid positive number');
      return;
    }
    setSaving(true);
    try {
      await api.updateRetirementAccount(id, {
        name: name.trim(),
        provider: provider.trim(),
        bitcoinAmount: btcAmount,
        currency,
        notes: notes.trim() || undefined,
      });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update account');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!id || !account) return;
    if (!window.confirm('Are you sure you want to delete this retirement account?')) return;
    try {
      await api.deleteRetirementAccount(id);
      navigate('/holdings/retirement');
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to delete account');
    }
  }

  async function addCostBasis() {
    if (!id || !account) return;
    const amount = parseFloat(newCostBasis);
    if (isNaN(amount) || amount < 0) {
      setError('Cost basis must be a valid positive number');
      return;
    }
    try {
      const entry: CostBasisEntryInput = {
        costBasis: amount,
        acquisitionDate: new Date(newAcquisitionDate).toISOString(),
      };
      await api.addCostBasisEntry(id, entry);
      setNewCostBasis('');
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to add cost basis');
    }
  }

  async function removeCostBasis(entryId: string) {
    if (!id) return;
    try {
      await api.removeCostBasisEntry(id, entryId);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to remove cost basis');
    }
  }

  const openNewDatePicker = () => {
    const el = newDateInputRef.current;
    if (el) {
      if (typeof (el as any).showPicker === 'function') {
        (el as any).showPicker();
      } else {
        el.focus();
        el.click();
      }
    }
  };

  if (loading) return <Spinner />;

  if (!account) {
    return <div className="text-[var(--color-coffer-muted)]">Account not found.</div>;
  }

  const inputClass =
    'w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm outline-none focus:border-[var(--color-coffer-orange)]';

  return (
    <div>
      <div className="mb-6 flex items-center gap-3">
        <button onClick={() => navigate(-1)} className="text-[var(--color-coffer-muted)] hover:text-white">
          <ChevronLeft size={20} />
        </button>
        <h1 className="text-2xl font-bold">{account.name}</h1>
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</div>}

      <Card className="mb-6 p-6">
        <div className="mb-4 flex items-center gap-3">
          <div className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--color-coffer-border)]">
            <Building2 size={20} />
          </div>
          <div>
            <div className="font-semibold">
              {ACCOUNT_TYPES.find((t) => t.value === account.accountType)?.label ?? account.accountType}
            </div>
            <div className="text-sm text-[var(--color-coffer-muted)]">{account.provider}</div>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-3 text-sm mb-4">
          <div>
            <div className="text-[var(--color-coffer-muted)]">Bitcoin</div>
            <div className="font-bold">{account.bitcoinAmount.toFixed(8)} BTC</div>
          </div>
          <div>
            <div className="text-[var(--color-coffer-muted)]">Total Cost Basis ({displayCurrency})</div>
            <div className="font-bold">{fmt(account.totalCostBasis, account.currency)}</div>
          </div>
          <div>
            <div className="text-[var(--color-coffer-muted)]">Created</div>
            <div className="font-bold">{new Date(account.createdAt).toLocaleDateString()}</div>
          </div>
        </div>
      </Card>

      <Card className="mb-6 p-5">
        <h2 className="mb-4 text-lg font-semibold">Edit Account</h2>
        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Account Name</label>
            <input type="text" value={name} onChange={(e) => setName(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Provider</label>
            <input type="text" value={provider} onChange={(e) => setProvider(e.target.value)} className={inputClass} />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Bitcoin Amount</label>
            <input
              type="number"
              step="0.00000001"
              min="0"
              value={bitcoinAmount}
              onChange={(e) => setBitcoinAmount(e.target.value)}
              className={inputClass}
            />
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Account Currency</label>
            <select value={currency} onChange={(e) => setCurrency(e.target.value)} className={inputClass}>
              {SUPPORTED_CURRENCIES.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.code} — {c.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Notes</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={2}
              className={`${inputClass} resize-none`}
            />
          </div>
          <div className="flex gap-3">
            <Button onClick={handleUpdate} disabled={saving}>
              {saving ? 'Saving...' : 'Save Changes'}
            </Button>
            <Button variant="ghost" onClick={handleDelete}>
              <span className="flex items-center gap-2 text-red-400">
                <Trash2 size={16} /> Delete
              </span>
            </Button>
          </div>
        </div>
      </Card>

      <Card className="p-5">
        <h2 className="mb-4 text-lg font-semibold">Cost Basis Entries</h2>
        <div className="space-y-3">
          {account.costBasisEntries.map((entry) => (
            <div key={entry.id} className="flex items-center justify-between rounded-lg border border-[var(--color-coffer-border)] p-3">
              <div>
                <div className="font-semibold">{fmt(entry.costBasis, account.currency)}</div>
                <div className="text-xs text-[var(--color-coffer-muted)]">
                  {new Date(entry.acquisitionDate).toLocaleDateString()}
                </div>
              </div>
              <button
                onClick={() => removeCostBasis(entry.id)}
                className="text-[var(--color-coffer-muted)] hover:text-red-400"
              >
                <X size={16} />
              </button>
            </div>
          ))}

          {account.costBasisEntries.length === 0 && (
            <p className="text-sm text-[var(--color-coffer-muted)]">No cost basis entries.</p>
          )}

          <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-end">
            <div className="flex-1">
              <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Cost Basis ({account.currency})</label>
              <input
                type="number"
                step="0.01"
                min="0"
                value={newCostBasis}
                onChange={(e) => setNewCostBasis(e.target.value)}
                placeholder="0.00"
                className={inputClass}
              />
            </div>
            <div className="flex-1">
              <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Acquisition Date</label>
              <div
                className="group relative cursor-pointer rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] pr-2 focus-within:border-[var(--color-coffer-orange)] hover:border-[var(--color-coffer-orange)]/60"
                onClick={openNewDatePicker}
              >
                <input
                  ref={newDateInputRef}
                  type="date"
                  value={newAcquisitionDate}
                  onChange={(e) => setNewAcquisitionDate(e.target.value)}
                  className="w-full cursor-pointer bg-transparent px-3 py-2 pr-8 text-sm outline-none text-[var(--color-coffer-text)] [color-scheme:dark] accent-[var(--color-coffer-orange)] caret-[var(--color-coffer-orange)] [&::-webkit-calendar-picker-indicator]:hidden"
                  onClick={(e) => {
                    e.stopPropagation();
                    const el = e.currentTarget;
                    if (typeof (el as any).showPicker === 'function') {
                      (el as any).showPicker();
                    } else {
                      el.focus();
                    }
                  }}
                />
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    openNewDatePicker();
                  }}
                  className="absolute right-1 top-1/2 -translate-y-1/2 rounded p-1 text-[var(--color-coffer-muted)] hover:bg-[var(--color-coffer-border)] hover:text-[var(--color-coffer-orange)] focus:outline-none focus:ring-1 focus:ring-[var(--color-coffer-orange)]"
                  aria-label="Open date picker"
                >
                  <Calendar size={18} />
                </button>
              </div>
            </div>
            <Button onClick={addCostBasis}>
              <span className="flex items-center gap-2">
                <Plus size={16} /> Add
              </span>
            </Button>
          </div>
        </div>
      </Card>
    </div>
  );
}
