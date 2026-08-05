import { useState } from 'react';
import { X } from 'lucide-react';
import { api } from '../api/client';
import { Button } from './ui';

// Returns true if the input is a full output descriptor with an explicit script wrapper
// (e.g. wpkh(...), wsh(...), tr(...)). In that case the script type is already encoded.
function isFullDescriptor(s: string): boolean {
  const t = s.trim().toLowerCase();
  return (
    t.startsWith('wpkh(') ||
    t.startsWith('wsh(') ||
    t.startsWith('sh(wpkh(') ||
    t.startsWith('sh(wsh(') ||
    t.startsWith('pkh(') ||
    t.startsWith('tr(')
  );
}

// Returns true if the input contains a bare multisig expression without a script type wrapper
function hasBareMultisig(s: string): boolean {
  return /(?:sorted)?multi\s*\(/i.test(s) && !isFullDescriptor(s);
}

// Infers the default ScriptType value from SLIP-132 prefix or multisig detection
function inferScriptType(s: string): string {
  const t = s.trim();
  if (/^zpub|^vpub/i.test(t)) return 'P2wpkh';
  if (/^ypub|^upub/i.test(t)) return 'P2shP2wpkh';
  if (hasBareMultisig(t)) return 'P2wsh';
  return 'P2pkh';
}

const SINGLE_KEY_SCRIPT_TYPES = [
  { value: 'P2pkh', label: 'Legacy (P2PKH)', hint: '1...' },
  { value: 'P2shP2wpkh', label: 'Nested SegWit (P2SH-P2WPKH)', hint: '3...' },
  { value: 'P2wpkh', label: 'Native SegWit (P2WPKH)', hint: 'bc1q...' },
  { value: 'P2tr', label: 'Taproot (P2TR)', hint: 'bc1p...' },
];

const MULTISIG_SCRIPT_TYPES = [
  { value: 'P2sh', label: 'Legacy Multisig (P2SH)', hint: '3...' },
  { value: 'P2shP2wsh', label: 'Nested SegWit Multisig (P2SH-P2WSH)', hint: '3...' },
  { value: 'P2wsh', label: 'Native SegWit Multisig (P2WSH)', hint: 'bc1q...' },
];

export function ImportWalletModal({ onClose, onImported }: { onClose: () => void; onImported: () => void }) {
  const [name, setName] = useState('');
  const [descriptor, setDescriptor] = useState('');
  const [network, setNetwork] = useState('Mainnet');
  const [description, setDescription] = useState('');
  const [scriptType, setScriptType] = useState('P2pkh');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const showScriptTypeSelector = descriptor.trim().length > 0 && !isFullDescriptor(descriptor);
  const isMulti = hasBareMultisig(descriptor);
  const scriptTypeOptions = isMulti ? MULTISIG_SCRIPT_TYPES : SINGLE_KEY_SCRIPT_TYPES;

  function handleDescriptorChange(value: string) {
    setDescriptor(value);
    setScriptType(inferScriptType(value));
  }

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
        scriptTypeOverride: showScriptTypeSelector ? scriptType : undefined,
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
              onChange={(e) => handleDescriptorChange(e.target.value)}
              placeholder="wpkh([fingerprint/84h/0h/0h]xpub.../0/*) or zpub6..."
            />
            <p className="mt-1 text-xs text-[var(--color-coffer-muted)]">
              Public keys only. CofferOS never accepts private keys or seed phrases.
            </p>
          </div>

          {showScriptTypeSelector && (
            <div>
              <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">
                Address type
              </label>
              <select
                className={inputClass}
                value={scriptType}
                onChange={(e) => setScriptType(e.target.value)}
              >
                {scriptTypeOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label} — {opt.hint}
                  </option>
                ))}
              </select>
              <p className="mt-1 text-xs text-[var(--color-coffer-muted)]">
                Auto-detected from key prefix. Override if your wallet uses a different address format.
              </p>
            </div>
          )}

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
