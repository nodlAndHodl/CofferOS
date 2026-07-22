import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, FileText } from 'lucide-react';
import { api } from '../api/client';
import type { Note, WalletDetail } from '../types';
import { Badge, Button, Card, Spinner } from '../components/ui';
import { formatBtc, formatDate, shorten } from '../lib/format';

type Tab = 'addresses' | 'utxos' | 'transactions' | 'descriptors';

const PAGE_SIZE = 20;

export function WalletDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [wallet, setWallet] = useState<WalletDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>('addresses');

  function refresh() {
    if (!id) return;
    api
      .getWallet(id)
      .then(setWallet)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load'));
  }

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setError(null);
    api
      .getWallet(id)
      .then(setWallet)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load'))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <Spinner />;
  if (error || !wallet)
    return (
      <div className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error ?? 'Wallet not found'}</div>
    );

  const tabs: Tab[] = ['addresses', 'utxos', 'transactions', 'descriptors'];

  return (
    <div>
      <Link to="/" className="mb-6 inline-flex items-center gap-2 text-sm text-[var(--color-coffer-muted)] hover:text-white">
        <ArrowLeft size={16} /> Back to dashboard
      </Link>

      <div className="mb-6 flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">{wallet.name}</h1>
            <Badge tone="orange">{wallet.network}</Badge>
            {wallet.watchOnly && <Badge tone="green">watch-only</Badge>}
          </div>
          {wallet.description && <p className="mt-1 text-sm text-[var(--color-coffer-muted)]">{wallet.description}</p>}
          <p className="mt-1 text-xs text-[var(--color-coffer-muted)]">Created {formatDate(wallet.createdAt)}</p>
        </div>
        <div className="text-right">
          <div className="text-2xl font-bold">{formatBtc(wallet.balance.totalSats)}</div>
          <div className="text-xs text-[var(--color-coffer-muted)]">
            {formatBtc(wallet.balance.confirmedSats)} confirmed
          </div>
          {wallet.balance.unconfirmedSats !== 0 && (
            <div className="text-xs text-[var(--color-coffer-muted)]">
              {formatBtc(wallet.balance.unconfirmedSats)} pending
            </div>
          )}
        </div>
      </div>

      <div className="mb-4 flex gap-1 border-b border-[var(--color-coffer-border)]">
        {tabs.map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium capitalize transition ${
              tab === t
                ? 'border-b-2 border-[var(--color-coffer-orange)] text-white'
                : 'text-[var(--color-coffer-muted)] hover:text-white'
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      <Card className="overflow-hidden">
        {tab === 'addresses' && <AddressTable wallet={wallet} />}
        {tab === 'utxos' && <UtxoTable wallet={wallet} onNoteSaved={refresh} />}
        {tab === 'transactions' && <TransactionTable wallet={wallet} onNoteSaved={refresh} />}
        {tab === 'descriptors' && <DescriptorList wallet={wallet} />}
      </Card>
    </div>
  );
}

function NoteCell({
  walletId,
  target,
  reference,
  notes,
  onSaved,
}: {
  walletId: string;
  target: string;
  reference: string;
  notes: Note[];
  onSaved: () => void;
}) {
  const note = notes.find((n) => n.target === target && n.reference === reference);
  const [isOpen, setIsOpen] = useState(false);
  const [content, setContent] = useState('');
  const [saving, setSaving] = useState(false);

  const open = () => {
    setContent(note?.content ?? '');
    setIsOpen(true);
  };
  const close = () => setIsOpen(false);

  async function handleSave() {
    setSaving(true);
    try {
      if (note) {
        await api.updateNote(note.id, { content });
      } else {
        await api.createNote(walletId, { target, reference, content });
      }
      onSaved();
      close();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to save note');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!note) return;
    setSaving(true);
    try {
      await api.deleteNote(note.id);
      onSaved();
      close();
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Failed to delete note');
    } finally {
      setSaving(false);
    }
  }

  return (
    <>
      <button
        onClick={open}
        className={`rounded p-1 ${note ? 'text-[var(--color-coffer-orange)]' : 'text-[var(--color-coffer-muted)] hover:text-white'}`}
        title={note?.content ?? 'Add note'}
      >
        <FileText size={16} />
      </button>

      {isOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4"
          onClick={close}
        >
          <div onClick={(e) => e.stopPropagation()}>
            <Card className="w-full max-w-md p-4">
              <h3 className="mb-2 text-lg font-semibold">{note ? 'Edit note' : 'Add note'}</h3>
              {note && (
                <div className="mb-3 text-xs text-[var(--color-coffer-muted)]">
                  Created {formatDate(note.createdAt)} · Updated {formatDate(note.updatedAt)}
                </div>
              )}
              <textarea
                value={content}
                onChange={(e) => setContent(e.target.value)}
                rows={5}
                className="mb-4 w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] p-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
                placeholder="Note (JSON is fine)..."
              />
              <div className="flex justify-end gap-2">
                <Button variant="ghost" onClick={close}>
                  Cancel
                </Button>
                {note && (
                  <Button variant="ghost" className="text-red-400 hover:text-red-300" onClick={handleDelete}>
                    Delete
                  </Button>
                )}
                <Button onClick={handleSave} disabled={saving}>
                  {saving ? 'Saving...' : 'Save'}
                </Button>
              </div>
            </Card>
          </div>
        </div>
      )}
    </>
  );
}

function Pagination({
  page,
  totalPages,
  onChange,
}: {
  page: number;
  totalPages: number;
  onChange: (p: number) => void;
}) {
  if (totalPages <= 1) return null;
  return (
    <div className="flex items-center justify-end gap-3 px-4 py-3 text-xs text-[var(--color-coffer-muted)]">
      <button
        disabled={page === 0}
        onClick={() => onChange(page - 1)}
        className="rounded border border-[var(--color-coffer-border)] px-2 py-1 disabled:opacity-40"
      >
        Previous
      </button>
      <span>
        Page {page + 1} of {totalPages}
      </span>
      <button
        disabled={page === totalPages - 1}
        onClick={() => onChange(page + 1)}
        className="rounded border border-[var(--color-coffer-border)] px-2 py-1 disabled:opacity-40"
      >
        Next
      </button>
    </div>
  );
}

function Empty({ text }: { text: string }) {
  return <p className="py-10 text-center text-sm text-[var(--color-coffer-muted)]">{text}</p>;
}

function AddressTable({ wallet }: { wallet: WalletDetail }) {
  const [page, setPage] = useState(0);
  const totalPages = Math.ceil(wallet.addresses.length / PAGE_SIZE);

  useEffect(() => setPage(0), [wallet.addresses.length]);

  if (wallet.addresses.length === 0) return <Empty text="No addresses derived yet." />;
  const paged = wallet.addresses.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);
  return (
    <div>
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            <th className="px-4 py-3">Index</th>
            <th className="px-4 py-3">Chain</th>
            <th className="px-4 py-3">Address</th>
            <th className="px-4 py-3">Used</th>
            <th className="px-4 py-3">Tx Count</th>
            <th className="px-4 py-3">First Tx</th>
            <th className="px-4 py-3">Last Tx</th>
            <th className="px-4 py-3">Current</th>
          </tr>
        </thead>
        <tbody>
          {paged.map((a) => (
            <tr key={a.id} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2">{a.derivationIndex}</td>
              <td className="px-4 py-2">{a.isChange ? 'change' : 'receive'}</td>
              <td className="px-4 py-2 font-mono text-xs">{a.value}</td>
              <td className="px-4 py-2">{a.isUsed ? <Badge tone="orange">used</Badge> : <Badge>unused</Badge>}</td>
              <td className="px-4 py-2">{a.useCount}</td>
              <td className="px-4 py-2 font-mono text-xs">{a.firstTxId ? shorten(a.firstTxId) : '—'}</td>
              <td className="px-4 py-2 font-mono text-xs">{a.lastTxId ? shorten(a.lastTxId) : '—'}</td>
              <td className="px-4 py-2">{formatBtc(a.currentSats)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <Pagination page={page} totalPages={totalPages} onChange={setPage} />
    </div>
  );
}

function UtxoTable({ wallet, onNoteSaved }: { wallet: WalletDetail; onNoteSaved: () => void }) {
  const [page, setPage] = useState(0);
  const totalPages = Math.ceil(wallet.utxos.length / PAGE_SIZE);

  useEffect(() => setPage(0), [wallet.utxos.length]);

  if (wallet.utxos.length === 0) return <Empty text="No UTXOs. Connect a node to discover coins." />;
  const paged = wallet.utxos.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);
  return (
    <div>
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            <th className="px-4 py-3">Outpoint</th>
            <th className="px-4 py-3">Value</th>
            <th className="px-4 py-3">Confirmations</th>
            <th className="px-4 py-3">Notes</th>
          </tr>
        </thead>
        <tbody>
          {paged.map((u) => (
            <tr key={`${u.txId}:${u.vout}`} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2 font-mono text-xs">{shorten(u.txId)}:{u.vout}</td>
              <td className="px-4 py-2">{formatBtc(u.valueSats)}</td>
              <td className="px-4 py-2">{u.confirmations > 0 ? u.confirmations : 'mempool'}</td>
              <td className="px-4 py-2">
                <NoteCell
                  walletId={wallet.id}
                  target="Utxo"
                  reference={`${u.txId}:${u.vout}`}
                  notes={wallet.notes}
                  onSaved={onNoteSaved}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <Pagination page={page} totalPages={totalPages} onChange={setPage} />
    </div>
  );
}

function TransactionTable({ wallet, onNoteSaved }: { wallet: WalletDetail; onNoteSaved: () => void }) {
  const [page, setPage] = useState(0);
  const totalPages = Math.ceil(wallet.transactions.length / PAGE_SIZE);

  useEffect(() => setPage(0), [wallet.transactions.length]);

  if (wallet.transactions.length === 0) return <Empty text="No transactions. Connect a node to sync history." />;
  const paged = wallet.transactions.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);
  return (
    <div>
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            <th className="px-4 py-3">Txid</th>
            <th className="px-4 py-3">Amount</th>
            <th className="px-4 py-3">Fee</th>
            <th className="px-4 py-3">Direction</th>
            <th className="px-4 py-3">Confirmations</th>
            <th className="px-4 py-3">Block Height</th>
            <th className="px-4 py-3">Time</th>
            <th className="px-4 py-3">Notes</th>
          </tr>
        </thead>
        <tbody>
          {paged.map((t) => (
            <tr key={t.txId} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2 font-mono text-xs">{shorten(t.txId)}</td>
              <td className="px-4 py-2">{formatBtc(t.netAmountSats)}</td>
              <td className="px-4 py-2">{formatBtc(t.feeSats)}</td>
              <td className="px-4 py-2 capitalize">{t.direction}</td>
              <td className="px-4 py-2">{t.confirmations > 0 ? t.confirmations : 'mempool'}</td>
              <td className="px-4 py-2">{t.blockHeight ?? '—'}</td>
              <td className="px-4 py-2">{formatDate(t.timestamp)}</td>
              <td className="px-4 py-2">
                <NoteCell
                  walletId={wallet.id}
                  target="Transaction"
                  reference={t.txId}
                  notes={wallet.notes}
                  onSaved={onNoteSaved}
                />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <Pagination page={page} totalPages={totalPages} onChange={setPage} />
    </div>
  );
}

function DescriptorList({ wallet }: { wallet: WalletDetail }) {
  return (
    <div className="divide-y divide-[var(--color-coffer-border)]">
      {wallet.descriptors.map((d) => (
        <div key={d.id} className="p-4">
          <div className="mb-2 flex items-center gap-2">
            <Badge tone="orange">{d.scriptType}</Badge>
            <Badge>{d.source}</Badge>
            {d.derivationPath && <span className="text-xs text-[var(--color-coffer-muted)]">{d.derivationPath}</span>}
          </div>
          <div className="break-all rounded-lg bg-[var(--color-coffer-bg)] p-3 font-mono text-xs">{d.raw}</div>
          <div className="mt-2 text-xs text-[var(--color-coffer-muted)]">{d.addressCount} address(es) derived</div>
        </div>
      ))}
    </div>
  );
}
