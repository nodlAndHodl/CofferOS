import { useEffect, useState } from 'react';
import { CalendarPlus, Pencil, Trash2 } from 'lucide-react';
import { api } from '../api/client';
import type { TimelineEntry, WalletTimeline, CreateTimelineEventRequest, UpdateTimelineEventRequest } from '../types';
import { Button, Card, Spinner } from './ui';
import { formatBtc, formatDate, shorten } from '../lib/format';

interface Props {
  walletId: string;
}

const eventTypeColors: Record<string, string> = {
  Annotation: 'bg-purple-500/10 text-purple-400',
  TransactionReceived: 'bg-green-500/10 text-green-400',
  TransactionSent: 'bg-red-500/10 text-red-400',
  WalletImported: 'bg-blue-500/10 text-blue-400',
  CurrentHoldings: 'bg-orange-500/10 text-orange-400',
  Lightning: 'bg-yellow-500/10 text-yellow-400',
  Node: 'bg-cyan-500/10 text-cyan-400',
  Multisig: 'bg-pink-500/10 text-pink-400',
  WalletMigration: 'bg-indigo-500/10 text-indigo-400',
};

export function WalletTimeline({ walletId }: Props) {
  const [timeline, setTimeline] = useState<WalletTimeline | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState<TimelineEntry | null>(null);
  const [form, setForm] = useState<{
    occurredAt: string;
    title: string;
    description: string;
    reference: string;
  }>({ occurredAt: '', title: '', description: '', reference: '' });

  function refresh() {
    setLoading(true);
    api
      .getWalletTimeline(walletId)
      .then(setTimeline)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load timeline'))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    refresh();
  }, [walletId]);

  function startCreate() {
    setEditing(null);
    setForm({
      occurredAt: new Date().toISOString().slice(0, 16),
      title: '',
      description: '',
      reference: '',
    });
  }

  function startEdit(entry: TimelineEntry) {
    if (!entry.id) return;
    setEditing(entry);
    setForm({
      occurredAt: entry.occurredAt.slice(0, 16),
      title: entry.title,
      description: entry.description ?? '',
      reference: entry.reference ?? '',
    });
  }

  async function handleSave() {
    const payload: CreateTimelineEventRequest | UpdateTimelineEventRequest = {
      occurredAt: new Date(form.occurredAt).toISOString(),
      title: form.title,
      description: form.description || null,
      reference: form.reference || null,
    };

    try {
      if (editing?.id) {
        await api.updateTimelineEvent(editing.id, payload);
      } else {
        await api.createTimelineEvent(walletId, { ...payload, type: 'Annotation' });
      }
      setEditing(null);
      refresh();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to save');
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this timeline event?')) return;
    try {
      await api.deleteTimelineEvent(id);
      refresh();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to delete');
    }
  }

  if (loading) return <Spinner />;
  if (error || !timeline) return <div className="rounded bg-red-500/10 px-4 py-3 text-sm text-red-400">{error ?? 'Not found'}</div>;

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <div className="text-2xl font-bold">{formatBtc(timeline.currentBalance.totalSats)}</div>
          <div className="text-xs text-[var(--color-coffer-muted)]">Current balance</div>
        </div>
        <Button onClick={startCreate}>
          <CalendarPlus size={16} className="mr-2" /> Add event
        </Button>
      </div>

      {editing !== null && (
        <Card className="p-4">
          <h4 className="mb-3 font-semibold">{editing.id ? 'Edit event' : 'Add event'}</h4>
          <div className="mb-3 grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs text-[var(--color-coffer-muted)]">Date / time</label>
              <input
                type="datetime-local"
                value={form.occurredAt}
                onChange={(e) => setForm({ ...form, occurredAt: e.target.value })}
                className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
              />
            </div>
            <div>
              <label className="block text-xs text-[var(--color-coffer-muted)]">Title</label>
              <input
                type="text"
                value={form.title}
                onChange={(e) => setForm({ ...form, title: e.target.value })}
                className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
              />
            </div>
          </div>
          <div className="mb-3">
            <label className="block text-xs text-[var(--color-coffer-muted)]">Description</label>
            <textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              rows={3}
              className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
            />
          </div>
          <div className="mb-3">
            <label className="block text-xs text-[var(--color-coffer-muted)]">Reference</label>
            <input
              type="text"
              value={form.reference}
              onChange={(e) => setForm({ ...form, reference: e.target.value })}
              className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
            />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={() => setEditing(null)}>
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={!form.title || !form.occurredAt}>
              {editing.id ? 'Update' : 'Add'}
            </Button>
          </div>
        </Card>
      )}

      <div className="relative space-y-6 border-l border-[var(--color-coffer-border)] pl-6">
        {timeline.entries.map((entry, index) => {
          const color = eventTypeColors[entry.type] ?? 'bg-[var(--color-coffer-bg)] text-[var(--color-coffer-muted)]';
          return (
            <div key={index} className="relative">
              <span
                className={`absolute -left-[31px] top-1 flex h-5 w-5 items-center justify-center rounded-full border border-[var(--color-coffer-border)] ${color}`}
              >
                <span className="h-2 w-2 rounded-full bg-current" />
              </span>
              <Card className="p-4">
                <div className="flex items-start justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className={`rounded px-2 py-0.5 text-xs ${color}`}>{entry.type}</span>
                      <span className="text-xs text-[var(--color-coffer-muted)]">{formatDate(entry.occurredAt)}</span>
                    </div>
                    <div className="mt-1 font-semibold">{entry.title}</div>
                    {entry.description && <div className="text-sm text-[var(--color-coffer-muted)]">{entry.description}</div>}
                    {entry.reference && (
                      <div className="mt-1 font-mono text-xs text-[var(--color-coffer-muted)]">{shorten(entry.reference)}</div>
                    )}
                    {(entry.amountSats != null || entry.runningBalanceSats != null) && (
                      <div className="mt-2 flex gap-4 text-sm">
                        {entry.amountSats != null && (
                          <span className={entry.amountSats >= 0 ? 'text-green-400' : 'text-red-400'}>
                            {entry.amountSats >= 0 ? '+' : ''}
                            {formatBtc(entry.amountSats)}
                          </span>
                        )}
                        {entry.runningBalanceSats != null && (
                          <span className="text-[var(--color-coffer-muted)]">
                            Balance: {formatBtc(entry.runningBalanceSats)}
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                  {entry.isUserEvent && entry.id && (
                    <div className="flex gap-1">
                      <Button variant="ghost" onClick={() => startEdit(entry)}>
                        <Pencil size={14} />
                      </Button>
                      <Button variant="ghost" onClick={() => handleDelete(entry.id!)}>
                        <Trash2 size={14} className="text-red-400" />
                      </Button>
                    </div>
                  )}
                </div>
              </Card>
            </div>
          );
        })}
      </div>
    </div>
  );
}
