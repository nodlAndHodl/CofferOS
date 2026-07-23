import { useEffect, useState } from 'react';
import { Tags } from 'lucide-react';
import { api } from '../api/client';
import type { ObjectMetadata } from '../types';
import { Button, Card, Spinner } from './ui';
import { formatDate } from '../lib/format';

interface Props {
  walletId: string;
  target: string;
  reference: string;
  isOpen: boolean;
  onClose: () => void;
  onSaved?: () => void;
}

interface MetadataRow {
  key: string;
  value: string;
}

export function TransactionMetadataModal({ walletId, target, reference, isOpen, onClose, onSaved }: Props) {
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [data, setData] = useState<ObjectMetadata | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [label, setLabel] = useState('');
  const [category, setCategory] = useState('');
  const [tagsInput, setTagsInput] = useState('');
  const [metadataRows, setMetadataRows] = useState<MetadataRow[]>([]);

  useEffect(() => {
    if (!isOpen) return;

    setLoading(true);
    setError(null);
    api
      .getObjectMetadata(walletId, target, reference)
      .then((m) => {
        setData(m);
        setLabel(m.label ?? '');
        setCategory(m.category ?? '');
        setTagsInput(m.tags.join(', '));
        setMetadataRows(Object.entries(m.metadata).map(([key, value]) => ({ key, value })));
      })
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load'))
      .finally(() => setLoading(false));
  }, [isOpen, walletId, target, reference]);

  function addRow() {
    setMetadataRows((rows) => [...rows, { key: '', value: '' }]);
  }

  function updateRow(index: number, field: 'key' | 'value', value: string) {
    setMetadataRows((rows) => rows.map((r, i) => (i === index ? { ...r, [field]: value } : r)));
  }

  function removeRow(index: number) {
    setMetadataRows((rows) => rows.filter((_, i) => i !== index));
  }

  async function handleSave() {
    setSaving(true);
    try {
      const tags = tagsInput
        .split(/[,\n]+/)
        .map((t) => t.trim().toLowerCase())
        .filter((t) => t.length > 0);

      const metadata: Record<string, string> = {};
      for (const row of metadataRows) {
        const key = row.key.trim();
        if (key) metadata[key] = row.value;
      }

      await api.updateObjectMetadata(walletId, target, reference, {
        target,
        reference,
        label: label.trim() || null,
        category: category.trim() || null,
        tags,
        metadata,
      });

      onSaved?.();
      onClose();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to save');
    } finally {
      setSaving(false);
    }
  }

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onClick={onClose}>
      <div className="max-h-[90vh] w-full max-w-lg overflow-auto" onClick={(e) => e.stopPropagation()}>
        <Card className="p-4">
          <h3 className="mb-1 text-lg font-semibold">Metadata</h3>
          <p className="mb-4 text-xs text-[var(--color-coffer-muted)] font-mono break-all">{target} :: {reference}</p>

          {loading && <Spinner />}
          {error && <div className="mb-3 rounded bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</div>}

          {!loading && !error && (
            <>
              <div className="mb-3">
                <label className="mb-1 block text-xs text-[var(--color-coffer-muted)]">Label</label>
                <input
                  type="text"
                  value={label}
                  onChange={(e) => setLabel(e.target.value)}
                  placeholder="e.g. Vehicle Purchase"
                  className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
                />
              </div>

              <div className="mb-3">
                <label className="mb-1 block text-xs text-[var(--color-coffer-muted)]">Category</label>
                <input
                  type="text"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  placeholder="e.g. Expense"
                  className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
                />
              </div>

              <div className="mb-3">
                <label className="mb-1 block text-xs text-[var(--color-coffer-muted)]">Tags (comma separated)</label>
                <input
                  type="text"
                  value={tagsInput}
                  onChange={(e) => setTagsInput(e.target.value)}
                  placeholder="truck, personal, large-expense"
                  className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
                />
              </div>

              <div className="mb-3">
                <label className="mb-1 block text-xs text-[var(--color-coffer-muted)]">Custom metadata</label>
                {metadataRows.map((row, i) => (
                  <div key={i} className="mb-2 flex items-center gap-2">
                    <input
                      type="text"
                      value={row.key}
                      onChange={(e) => updateRow(i, 'key', e.target.value)}
                      placeholder="key"
                      className="flex-1 rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-2 py-1 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
                    />
                    <input
                      type="text"
                      value={row.value}
                      onChange={(e) => updateRow(i, 'value', e.target.value)}
                      placeholder="value"
                      className="flex-[2] rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-2 py-1 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
                    />
                    <Button variant="ghost" onClick={() => removeRow(i)}>
                      ×
                    </Button>
                  </div>
                ))}
                <Button variant="ghost" onClick={addRow}>
                  + Add field
                </Button>
              </div>

              {data && data.notes.length > 0 && (
                <div className="mb-4">
                  <label className="mb-1 block text-xs text-[var(--color-coffer-muted)]">Notes</label>
                  <div className="space-y-2">
                    {data.notes.map((n) => (
                      <div key={n.id} className="rounded bg-[var(--color-coffer-bg)] p-2 text-sm text-white">
                        {n.content}
                        <div className="mt-1 text-xs text-[var(--color-coffer-muted)]">
                          Updated {formatDate(n.updatedAt)}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="flex justify-end gap-2">
                <Button variant="ghost" onClick={onClose}>
                  Cancel
                </Button>
                <Button onClick={handleSave} disabled={saving}>
                  {saving ? 'Saving...' : 'Save'}
                </Button>
              </div>
            </>
          )}
        </Card>
      </div>
    </div>
  );
}

export function MetadataBadge({ walletId, target, reference }: { walletId: string; target: string; reference: string }) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        onClick={() => setOpen(true)}
        className="rounded p-1 text-[var(--color-coffer-muted)] hover:text-[var(--color-coffer-orange)]"
        title="Edit metadata"
      >
        <Tags size={16} />
      </button>
      {open && (
        <TransactionMetadataModal
          walletId={walletId}
          target={target}
          reference={reference}
          isOpen={open}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}
