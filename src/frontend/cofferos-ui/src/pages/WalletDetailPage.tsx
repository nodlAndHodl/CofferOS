import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, FileText } from 'lucide-react';
import { api } from '../api/client';
import type { Address, Note, WalletDetail } from '../types';
import { WalletTimeline } from '../components/WalletTimeline';
import { WalletValueSparkline } from '../components/WalletValueSparkline';
import { InlineMetadataEditor } from '../components/InlineMetadataEditor';
import { Tooltip } from '../components/Tooltip';
import { Badge, Button, Card, CopyButton, Spinner } from '../components/ui';
import { formatBtc, formatDate, shorten } from '../lib/format';

type Tab = 'addresses' | 'utxos' | 'transactions' | 'descriptors' | 'timeline';

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
    const walletId = id;

    async function load() {
      setLoading(true);
      setError(null);
      try {
        const initial = await api.getWallet(walletId);
        setWallet(initial);
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Failed to load');
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [id]);

  if (loading) return <Spinner />;
  if (error || !wallet)
    return (
      <div className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error ?? 'Wallet not found'}</div>
    );

  const tabs: Tab[] = ['addresses', 'utxos', 'transactions', 'descriptors', 'timeline'];

  return (
    <div>
      <Link to="/wallets" className="mb-6 inline-flex items-center gap-2 text-sm text-[var(--color-coffer-muted)] hover:text-white">
        <ArrowLeft size={16} /> Back to wallets
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
        <div className="flex items-center gap-6 text-right">
          <WalletValueSparkline
            transactions={wallet.transactions}
            currentSats={wallet.balance.totalSats}
          />
          <div>
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
        {tab === 'timeline' && <WalletTimeline walletId={wallet.id} />}
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
  const receive = wallet.addresses.filter((a) => !a.isChange);
  const change = wallet.addresses.filter((a) => a.isChange);
  return (
    <div>
      <AddressSection title="Receive addresses" addresses={receive} />
      <AddressSection title="Change addresses" addresses={change} />
    </div>
  );
}

function AddressSection({ title, addresses }: { title: string; addresses: Address[] }) {
  const [page, setPage] = useState(0);
  const [filter, setFilter] = useState('');
  const [sortKey, setSortKey] = useState<'index' | 'address' | 'uses' | 'current'>('index');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');

  useEffect(() => setPage(0), [addresses.length, filter, sortKey, sortDir]);

  const filtered = addresses.filter(
    (a) =>
      a.value.toLowerCase().includes(filter.toLowerCase()) ||
      String(a.derivationIndex).includes(filter) ||
      (a.firstTxId ?? '').toLowerCase().includes(filter.toLowerCase()) ||
      (a.lastTxId ?? '').toLowerCase().includes(filter.toLowerCase())
  );

  const sorted = [...filtered].sort((a, b) => {
    let cmp = 0;
    switch (sortKey) {
      case 'index':
        cmp = a.derivationIndex - b.derivationIndex;
        break;
      case 'address':
        cmp = a.value.localeCompare(b.value);
        break;
      case 'uses':
        cmp = a.useCount - b.useCount;
        break;
      case 'current':
        cmp = a.currentSats - b.currentSats;
        break;
    }
    return sortDir === 'asc' ? cmp : -cmp;
  });

  const totalPages = Math.ceil(sorted.length / PAGE_SIZE);
  const paged = sorted.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

  const header = (key: typeof sortKey, label: string) => (
    <th
      className="cursor-pointer select-none px-4 py-3"
      onClick={() => {
        if (sortKey === key) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
        else {
          setSortKey(key);
          setSortDir('asc');
        }
      }}
    >
      <span className="flex items-center gap-1">
        {label}
        {sortKey === key && <span className="text-[var(--color-coffer-orange)]">{sortDir === 'asc' ? '↑' : '↓'}</span>}
      </span>
    </th>
  );

  if (addresses.length === 0) return <Empty text={`No ${title.toLowerCase()} derived yet.`} />;
  return (
    <div className="border-b border-[var(--color-coffer-border)]">
      <h3 className="px-4 py-3 text-sm font-semibold uppercase text-[var(--color-coffer-muted)]">{title}</h3>
      <div className="p-4">
        <input
          type="text"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Filter by address, index, or txid..."
          className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
      </div>
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            {header('index', 'Index')}
            {header('address', 'Address')}
            <th className="px-4 py-3">Used</th>
            {header('uses', 'Uses')}
            <th className="px-4 py-3">First Tx</th>
            <th className="px-4 py-3">Last Tx</th>
            {header('current', 'Current')}
          </tr>
        </thead>
        <tbody>
          {paged.map((a) => (
            <tr key={a.id} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2">{a.derivationIndex}</td>
              <td className="px-4 py-2 font-mono text-xs">
                <span className="inline-flex items-center gap-1">
                  <Tooltip content={a.value}>
                    <span>{a.value}</span>
                  </Tooltip>
                  <CopyButton value={a.value} />
                </span>
              </td>
              <td className="px-4 py-2">{a.isUsed ? <Badge tone="orange">used</Badge> : <Badge>unused</Badge>}</td>
              <td className="px-4 py-2">{a.useCount}</td>
              <td className="px-4 py-2 font-mono text-xs">
                {a.firstTxId ? (
                  <span className="inline-flex items-center gap-1">
                    <Tooltip content={a.firstTxId}>
                      <span>{shorten(a.firstTxId)}</span>
                    </Tooltip>
                    <CopyButton value={a.firstTxId} />
                  </span>
                ) : '—'}
              </td>
              <td className="px-4 py-2 font-mono text-xs">
                {a.lastTxId ? (
                  <span className="inline-flex items-center gap-1">
                    <Tooltip content={a.lastTxId}>
                      <span>{shorten(a.lastTxId)}</span>
                    </Tooltip>
                    <CopyButton value={a.lastTxId} />
                  </span>
                ) : '—'}
              </td>
              <td className="px-4 py-2">
                <span className={a.currentSats > 0 ? 'text-green-400' : ''}>{formatBtc(a.currentSats)}</span>
              </td>
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
  const [filter, setFilter] = useState('');
  const [sortKey, setSortKey] = useState<'outpoint' | 'address' | 'value' | 'confirmations'>('outpoint');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');

  const [metaFilter, setMetaFilter] = useState('');
  const normalizedMetaFilter = metaFilter.trim().toLowerCase();

  useEffect(() => setPage(0), [wallet.utxos.length, filter, normalizedMetaFilter, sortKey, sortDir]);

  const matchesMetadata = (reference: string) => {
    if (!normalizedMetaFilter) return true;
    return wallet.labels.some((l) => l.target === 'Utxo' && l.reference === reference && l.text.toLowerCase().includes(normalizedMetaFilter)) ||
      wallet.tags.some((t) => t.target === 'Utxo' && t.reference === reference && t.value.toLowerCase().includes(normalizedMetaFilter)) ||
      wallet.categories.some((c) => c.target === 'Utxo' && c.reference === reference && c.name.toLowerCase().includes(normalizedMetaFilter));
  };

  const filtered = wallet.utxos.filter(
    (u) =>
      (u.txId.toLowerCase().includes(filter.toLowerCase()) ||
        `${u.txId}:${u.vout}`.toLowerCase().includes(filter.toLowerCase()) ||
        (u.address ?? '').toLowerCase().includes(filter.toLowerCase())) &&
      matchesMetadata(`${u.txId}:${u.vout}`)
  );

  const sorted = [...filtered].sort((a, b) => {
    let cmp = 0;
    switch (sortKey) {
      case 'outpoint':
        cmp = a.txId.localeCompare(b.txId) || a.vout - b.vout;
        break;
      case 'address':
        cmp = (a.address ?? '').localeCompare(b.address ?? '');
        break;
      case 'value':
        cmp = a.valueSats - b.valueSats;
        break;
      case 'confirmations':
        cmp = a.confirmations - b.confirmations;
        break;
    }
    return sortDir === 'asc' ? cmp : -cmp;
  });

  const totalPages = Math.ceil(sorted.length / PAGE_SIZE);
  const paged = sorted.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

  const header = (key: typeof sortKey, label: string) => (
    <th
      className="cursor-pointer select-none px-4 py-3"
      onClick={() => {
        if (sortKey === key) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
        else {
          setSortKey(key);
          setSortDir('asc');
        }
      }}
    >
      <span className="flex items-center gap-1">
        {label}
        {sortKey === key && <span className="text-[var(--color-coffer-orange)]">{sortDir === 'asc' ? '↑' : '↓'}</span>}
      </span>
    </th>
  );

  if (wallet.utxos.length === 0) return <Empty text="No UTXOs. Connect a node to discover coins." />;
  return (
    <div>
      <div className="p-4">
        <input
          type="text"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Filter by outpoint or address..."
          className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
        <input
          type="text"
          value={metaFilter}
          onChange={(e) => setMetaFilter(e.target.value)}
          placeholder="Filter metadata (label/category/tag)"
          className="mt-2 w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
      </div>
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            {header('outpoint', 'Outpoint')}
            {header('address', 'Address')}
            {header('value', 'Value')}
            {header('confirmations', 'Confirmations')}
            <th className="px-4 py-3">Metadata</th>
            <th className="px-4 py-3">Notes</th>
          </tr>
        </thead>
        <tbody>
          {paged.map((u) => (
            <tr key={`${u.txId}:${u.vout}`} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2 font-mono text-xs">
                <span className="inline-flex items-center gap-1">
                  <Tooltip content={`${u.txId}:${u.vout}`}>
                    <span>{shorten(u.txId)}:{u.vout}</span>
                  </Tooltip>
                  <CopyButton value={`${u.txId}:${u.vout}`} />
                </span>
              </td>
              <td className="px-4 py-2 font-mono text-xs">
                {u.address ? (
                  <span className="inline-flex items-center gap-1">
                    <Tooltip content={u.address}>
                      <span>{shorten(u.address)}</span>
                    </Tooltip>
                    <CopyButton value={u.address} />
                  </span>
                ) : (
                  '—'
                )}
              </td>
              <td className="px-4 py-2">
                <span className="text-green-400">{formatBtc(u.valueSats)}</span>
              </td>
              <td className="px-4 py-2">
                {u.confirmations > 0 ? u.confirmations : <Badge tone="default">mempool</Badge>}
              </td>
              <td className="px-4 py-2">
                <UtxoMetadata wallet={wallet} txId={u.txId} vout={u.vout} onSaved={onNoteSaved} />
              </td>
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
  const [filter, setFilter] = useState('');
  const [metaFilter, setMetaFilter] = useState('');
  const [sortKey, setSortKey] = useState<'txid' | 'amount' | 'fee' | 'direction' | 'confirmations' | 'height' | 'time'>('time');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');

  const normalizedMetaFilter = metaFilter.trim().toLowerCase();

  const matchesMetadata = (reference: string) => {
    if (!normalizedMetaFilter) return true;
    return wallet.labels.some((l) => l.target === 'Transaction' && l.reference === reference && l.text.toLowerCase().includes(normalizedMetaFilter)) ||
      wallet.tags.some((t) => t.target === 'Transaction' && t.reference === reference && t.value.toLowerCase().includes(normalizedMetaFilter)) ||
      wallet.categories.some((c) => c.target === 'Transaction' && c.reference === reference && c.name.toLowerCase().includes(normalizedMetaFilter));
  };

  useEffect(() => setPage(0), [wallet.transactions.length, filter, normalizedMetaFilter, sortKey, sortDir]);

  const filtered = wallet.transactions.filter(
    (t) =>
      (t.txId.toLowerCase().includes(filter.toLowerCase()) ||
        t.direction.toLowerCase().includes(filter.toLowerCase()) ||
        String(t.blockHeight ?? '').includes(filter)) &&
      matchesMetadata(t.txId)
  );

  const sorted = [...filtered].sort((a, b) => {
    let cmp = 0;
    switch (sortKey) {
      case 'txid':
        cmp = a.txId.localeCompare(b.txId);
        break;
      case 'amount':
        cmp = a.netAmountSats - b.netAmountSats;
        break;
      case 'fee':
        cmp = a.feeSats - b.feeSats;
        break;
      case 'direction':
        cmp = a.direction.localeCompare(b.direction);
        break;
      case 'confirmations':
        cmp = a.confirmations - b.confirmations;
        break;
      case 'height':
        cmp = (a.blockHeight ?? 0) - (b.blockHeight ?? 0);
        break;
      case 'time':
        cmp = new Date(a.timestamp ?? 0).getTime() - new Date(b.timestamp ?? 0).getTime();
        break;
    }
    return sortDir === 'asc' ? cmp : -cmp;
  });

  const totalPages = Math.ceil(sorted.length / PAGE_SIZE);
  const paged = sorted.slice(page * PAGE_SIZE, (page + 1) * PAGE_SIZE);

  const header = (key: typeof sortKey, label: string) => (
    <th
      className="cursor-pointer select-none px-4 py-3"
      onClick={() => {
        if (sortKey === key) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
        else {
          setSortKey(key);
          setSortDir('asc');
        }
      }}
    >
      <span className="flex items-center gap-1">
        {label}
        {sortKey === key && <span className="text-[var(--color-coffer-orange)]">{sortDir === 'asc' ? '↑' : '↓'}</span>}
      </span>
    </th>
  );

  if (wallet.transactions.length === 0) return <Empty text="No transactions. Connect a node to sync history." />;
  return (
    <div>
      <div className="p-4">
        <input
          type="text"
          value={filter}
          onChange={(e) => setFilter(e.target.value)}
          placeholder="Filter by txid, direction, or block height..."
          className="w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
        <input
          type="text"
          value={metaFilter}
          onChange={(e) => setMetaFilter(e.target.value)}
          placeholder="Filter metadata (label/category/tag)"
          className="mt-2 w-full rounded border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm text-white outline-none focus:border-[var(--color-coffer-orange)]"
        />
      </div>
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            {header('txid', 'Txid')}
            {header('amount', 'Amount')}
            {header('fee', 'Fee')}
            {header('direction', 'Direction')}
            {header('confirmations', 'Confirmations')}
            {header('height', 'Block Height')}
            {header('time', 'Time')}
            <th className="px-4 py-3">Metadata</th>
            <th className="px-4 py-3">Notes</th>
          </tr>
        </thead>
        <tbody>
          {paged.map((t) => (
            <tr key={t.txId} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2 font-mono text-xs">
                <span className="inline-flex items-center gap-1">
                  <Tooltip content={t.txId}>
                    <span>{shorten(t.txId)}</span>
                  </Tooltip>
                  <CopyButton value={t.txId} />
                </span>
              </td>
              <td className="px-4 py-2">
                <span className={t.netAmountSats >= 0 ? 'text-green-400' : 'text-red-400'}>{formatBtc(t.netAmountSats)}</span>
              </td>
              <td className="px-4 py-2">
                <span className="text-red-400">{formatBtc(t.feeSats)}</span>
              </td>
              <td className="px-4 py-2">
                <Badge tone={t.direction.toLowerCase() === 'incoming' ? 'green' : 'red'}>{t.direction}</Badge>
              </td>
              <td className="px-4 py-2">
                {t.confirmations > 0 ? t.confirmations : <Badge tone="default">mempool</Badge>}
              </td>
              <td className="px-4 py-2">{t.blockHeight ?? '—'}</td>
              <td className="px-4 py-2">{formatDate(t.timestamp)}</td>
              <td className="px-4 py-2">
                <TransactionMetadata wallet={wallet} txId={t.txId} onSaved={onNoteSaved} />
              </td>
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

function UtxoMetadata({ wallet, txId, vout, onSaved }: { wallet: WalletDetail; txId: string; vout: number; onSaved: () => void }) {
  const reference = `${txId}:${vout}`;
  const label =
    wallet.labels
      .filter((l) => l.target === 'Utxo' && l.reference === reference)
      .map((l) => l.text)
      .join(', ') || null;
  const category =
    wallet.categories
      .filter((c) => c.target === 'Utxo' && c.reference === reference)
      .map((c) => c.name)
      .join(', ') || null;
  const tags = wallet.tags.filter((t) => t.target === 'Utxo' && t.reference === reference).map((t) => t.value);

  return (
    <InlineMetadataEditor
      walletId={wallet.id}
      target="Utxo"
      reference={reference}
      label={label}
      category={category}
      tags={tags}
      onSaved={onSaved}
    />
  );
}

function TransactionMetadata({ wallet, txId, onSaved }: { wallet: WalletDetail; txId: string; onSaved: () => void }) {
  const label =
    wallet.labels
      .filter((l) => l.target === 'Transaction' && l.reference === txId)
      .map((l) => l.text)
      .join(', ') || null;
  const category =
    wallet.categories
      .filter((c) => c.target === 'Transaction' && c.reference === txId)
      .map((c) => c.name)
      .join(', ') || null;
  const tags = wallet.tags.filter((t) => t.target === 'Transaction' && t.reference === txId).map((t) => t.value);

  return (
    <InlineMetadataEditor
      walletId={wallet.id}
      target="Transaction"
      reference={txId}
      label={label}
      category={category}
      tags={tags}
      onSaved={onSaved}
    />
  );
}

function DescriptorList({ wallet }: { wallet: WalletDetail }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead className="text-xs uppercase text-[var(--color-coffer-muted)]">
          <tr className="border-b border-[var(--color-coffer-border)]">
            <th className="px-4 py-3">Source</th>
            <th className="px-4 py-3">Type</th>
            <th className="px-4 py-3">Path</th>
            <th className="px-4 py-3">Descriptor</th>
            <th className="px-4 py-3">Addresses</th>
          </tr>
        </thead>
        <tbody>
          {wallet.descriptors.map((d) => (
            <tr key={d.id} className="border-b border-[var(--color-coffer-border)]/50">
              <td className="px-4 py-2"><Badge>{d.source}</Badge></td>
              <td className="px-4 py-2"><Badge tone="orange">{d.scriptType}</Badge></td>
              <td className="px-4 py-2 text-xs text-[var(--color-coffer-muted)]">{d.derivationPath ?? '—'}</td>
              <td className="px-4 py-2 break-all font-mono text-xs">{d.raw}</td>
              <td className="px-4 py-2">{d.addressCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
