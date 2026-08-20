import { useRef, useState } from 'react';
import { X, Plus, Trash2, Calendar } from 'lucide-react';
import { api } from '../api/client';
import type { CreateRetirementAccountRequest, RetirementAccountType } from '../types';
import { useUserSettings } from '../contexts/UserSettingsContext';
import { SUPPORTED_CURRENCIES } from '../lib/currency';
import { Button, Card } from './ui';

interface Props {
  onClose: () => void;
  onCreated: () => void;
}

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

export function CreateRetirementAccountModal({ onClose, onCreated }: Props) {
  const { settings } = useUserSettings();
  const [name, setName] = useState('');
  const [accountType, setAccountType] = useState<RetirementAccountType>('TraditionalIra');
  const [provider, setProvider] = useState('');
  const [bitcoinAmount, setBitcoinAmount] = useState('');
  const [currency, setCurrency] = useState(settings.currency);
  const [notes, setNotes] = useState('');
  const [costBasisEntries, setCostBasisEntries] = useState<Array<{ costBasis: string; acquisitionDate: string }>>([
    { costBasis: '', acquisitionDate: new Date().toISOString().split('T')[0] },
  ]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    if (!name.trim() || !provider.trim() || !bitcoinAmount) {
      setError('Please fill in all required fields');
      return;
    }

    const btcAmount = parseFloat(bitcoinAmount);
    if (isNaN(btcAmount) || btcAmount < 0) {
      setError('Bitcoin amount must be a valid positive number');
      return;
    }

    const validCostBasis = costBasisEntries.filter(
      (entry) => entry.costBasis.trim() && entry.acquisitionDate
    );

    if (validCostBasis.length === 0) {
      setError('Please add at least one cost basis entry');
      return;
    }

    setLoading(true);
    try {
      const payload: CreateRetirementAccountRequest = {
        name: name.trim(),
        accountType,
        provider: provider.trim(),
        bitcoinAmount: btcAmount,
        currency,
        notes: notes.trim() || undefined,
        costBasisEntries: validCostBasis.map((entry) => ({
          costBasis: parseFloat(entry.costBasis),
          acquisitionDate: new Date(entry.acquisitionDate).toISOString(),
        })),
      };

      await api.createRetirementAccount(payload);
      onCreated();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create retirement account');
    } finally {
      setLoading(false);
    }
  }

  function addCostBasisEntry() {
    setCostBasisEntries([
      ...costBasisEntries,
      { costBasis: '', acquisitionDate: new Date().toISOString().split('T')[0] },
    ]);
  }

  function removeCostBasisEntry(index: number) {
    if (costBasisEntries.length > 1) {
      setCostBasisEntries(costBasisEntries.filter((_, i) => i !== index));
    }
  }

  function updateCostBasisEntry(index: number, field: 'costBasis' | 'acquisitionDate', value: string) {
    const updated = [...costBasisEntries];
    updated[index] = { ...updated[index], [field]: value };
    setCostBasisEntries(updated);
  }

  const inputClass =
    'w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm outline-none focus:border-[var(--color-coffer-orange)]';

  const dateInputRefs = useRef<(HTMLInputElement | null)[]>([]);

  const openDatePicker = (index: number) => {
    const el = dateInputRefs.current[index];
    if (el) {
      if (typeof (el as any).showPicker === 'function') {
        (el as any).showPicker();
      } else {
        el.focus();
        el.click();
      }
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 overflow-y-auto">
      <div className="w-full max-w-2xl rounded-2xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] p-6 my-8">
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-bold">Add Retirement Account</h2>
          <button onClick={onClose} className="text-[var(--color-coffer-muted)] hover:text-white">
            <X size={20} />
          </button>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Account Name */}
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Account Name *</label>
            <input
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g., My Roth IRA"
              className={inputClass}
            />
          </div>

          {/* Account Type */}
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Account Type *</label>
            <select
              value={accountType}
              onChange={(e) => setAccountType(e.target.value as RetirementAccountType)}
              className={inputClass}
            >
              {ACCOUNT_TYPES.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          {/* Provider */}
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Provider *</label>
            <input
              type="text"
              value={provider}
              onChange={(e) => setProvider(e.target.value)}
              placeholder="e.g., Fidelity, Schwab, Self-Custodied"
              className={inputClass}
            />
          </div>

          {/* Bitcoin Amount */}
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Bitcoin Amount *</label>
            <input
              type="number"
              step="0.00000001"
              min="0"
              value={bitcoinAmount}
              onChange={(e) => setBitcoinAmount(e.target.value)}
              placeholder="0.00000000"
              className={inputClass}
            />
          </div>

          {/* Currency */}
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Account Currency</label>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className={inputClass}
            >
              {SUPPORTED_CURRENCIES.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.code} — {c.label}
                </option>
              ))}
            </select>
          </div>

          {/* Notes */}
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Notes</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Optional notes about this account..."
              rows={2}
              className={`${inputClass} resize-none`}
            />
          </div>

          {/* Cost Basis Entries */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <label className="block text-xs font-medium text-[var(--color-coffer-muted)]">Cost Basis Entries *</label>
              <button
                type="button"
                onClick={addCostBasisEntry}
                className="flex items-center gap-1 text-xs text-[var(--color-coffer-orange)] hover:text-[var(--color-coffer-orange)]/80"
              >
                <Plus size={14} /> Add Entry
              </button>
            </div>

            <div className="space-y-2">
              {costBasisEntries.map((entry, idx) => (
                <Card key={idx} className="p-3 flex gap-3 items-end">
                  <div className="flex-1">
                    <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Cost Basis (USD)</label>
                    <input
                      type="number"
                      step="0.01"
                      min="0"
                      value={entry.costBasis}
                      onChange={(e) => updateCostBasisEntry(idx, 'costBasis', e.target.value)}
                      placeholder="0.00"
                      className={inputClass}
                    />
                  </div>
                  <div className="flex-1">
                    <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Acquisition Date</label>
                    <div
                      className="group relative cursor-pointer rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] pr-2 focus-within:border-[var(--color-coffer-orange)] hover:border-[var(--color-coffer-orange)]/60"
                      title="Click to pick a date"
                      onClick={() => openDatePicker(idx)}
                    >
                      <input
                        ref={(el) => {
                          dateInputRefs.current[idx] = el;
                        }}
                        type="date"
                        value={entry.acquisitionDate}
                        onChange={(e) => updateCostBasisEntry(idx, 'acquisitionDate', e.target.value)}
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
                          openDatePicker(idx);
                        }}
                        className="absolute right-1 top-1/2 -translate-y-1/2 rounded p-1 text-[var(--color-coffer-muted)] hover:bg-[var(--color-coffer-border)] hover:text-[var(--color-coffer-orange)] focus:outline-none focus:ring-1 focus:ring-[var(--color-coffer-orange)]"
                        aria-label="Open date picker"
                      >
                        <Calendar size={18} />
                      </button>
                    </div>
                  </div>
                  {costBasisEntries.length > 1 && (
                    <button
                      type="button"
                      onClick={() => removeCostBasisEntry(idx)}
                      className="text-[var(--color-coffer-muted)] hover:text-red-400 transition"
                    >
                      <Trash2 size={16} />
                    </button>
                  )}
                </Card>
              ))}
            </div>
          </div>

          {/* Submit */}
          <div className="mt-6 flex justify-end gap-3">
            <Button variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={loading}>
              {loading ? 'Creating...' : 'Create Account'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
