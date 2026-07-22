import { useState } from 'react';
import { X } from 'lucide-react';
import { api } from '../api/client';
import { Button } from './ui';

export function ImportWalletModal({ onClose, onImported }: { onClose: () => void; onImported: () => void }) {
  const [name, setName] = useState('');
  const [descriptor, setDescriptor] = useState('');
  const [network, setNetwork] = useState('Mainnet');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    setError(null);
    setSubmitting(true);
    try {
      await api.importWallet({
        name,
        description: description || undefined,
        descriptor,
        network,
        initialAddressCount: 20,
      });
      onImported();
      onClose();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Import failed');
    } finally {
      setSubmitting(false);
    }
  }

  const inputClass =
    'w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm outline-none focus:border-[var(--color-coffer-orange)]';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
      <div className="w-full max-w-lg rounded-2xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] p-6">
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-bold">Import watch-only wallet</h2>
          <button onClick={onClose} className="text-[var(--color-coffer-muted)] hover:text-white">
            <X size={20} />
          </button>
        </div>

        <div className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Name</label>
            <input className={inputClass} value={name} onChange={(e) => setName(e.target.value)} placeholder="Cold Storage Vault" />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">
              Descriptor or extended public key (xpub / ypub / zpub)
            </label>
            <textarea
              className={`${inputClass} h-24 resize-none font-mono text-xs`}
              value={descriptor}
              onChange={(e) => setDescriptor(e.target.value)}
              placeholder="wpkh([fingerprint/84h/0h/0h]xpub.../0/*) or zpub6..."
            />
            <p className="mt-1 text-xs text-[var(--color-coffer-muted)]">
              Public keys only. CofferOS never accepts private keys or seed phrases.
            </p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Network</label>
              <select className={inputClass} value={network} onChange={(e) => setNetwork(e.target.value)}>
                <option>Mainnet</option>
                <option>Testnet</option>
                <option>Signet</option>
                <option>Regtest</option>
              </select>
            </div>
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Note (optional)</label>
              <input className={inputClass} value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Keys with lawyer" />
            </div>
          </div>

          {error && <div className="rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</div>}
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={submit} disabled={submitting || !name || !descriptor}>
            {submitting ? 'Importing…' : 'Import wallet'}
          </Button>
        </div>
      </div>
    </div>
  );
}
