import { useState } from 'react';
import { Check, Edit2, X } from 'lucide-react';
import { api } from '../api/client';
import type { Utxo } from '../types';
import { formatFiat } from '../lib/format';

interface UtxoCostBasisEditorProps {
  utxo: Utxo;
  onSaved?: () => void;
}

export function UtxoCostBasisEditor({ utxo, onSaved }: UtxoCostBasisEditorProps) {
  const reference = `${utxo.txId}:${utxo.vout}`;
  const initial = utxo.costBasis ?? 0;
  const [editing, setEditing] = useState(false);
  const [amount, setAmount] = useState(initial);
  const [saving, setSaving] = useState(false);

  async function handleSave() {
    setSaving(true);
    try {
      if (amount <= 0) {
        await api.clearCostBasis('Utxo', reference);
      } else {
        await api.setCostBasis('Utxo', reference, amount);
      }
      setEditing(false);
      onSaved?.();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to save');
    } finally {
      setSaving(false);
    }
  }

  function handleCancel() {
    setAmount(initial);
    setEditing(false);
  }

  if (editing) {
    return (
      <div className="flex items-center gap-1">
        <input
          type="number"
          value={amount}
          onChange={(e) => setAmount(parseFloat(e.target.value) || 0)}
          className="w-24 rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-2 py-1 text-xs text-white outline-none focus:border-[var(--color-coffer-orange)]"
          disabled={saving}
        />
        <button onClick={handleSave} disabled={saving} className="text-green-400 hover:text-green-300" title="Save"><Check size={14} /></button>
        <button onClick={handleCancel} disabled={saving} className="text-red-400 hover:text-red-300" title="Cancel"><X size={14} /></button>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-1">
      <span className="text-xs">{initial > 0 ? formatFiat(initial) : '—'}</span>
      <button onClick={() => setEditing(true)} className="text-[var(--color-coffer-muted)] hover:text-[var(--color-coffer-orange)]" title={initial > 0 ? 'Edit cost basis' : 'Add cost basis'}>
        <Edit2 size={14} />
      </button>
    </div>
  );
}
