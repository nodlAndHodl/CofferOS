import { useEffect, useState } from 'react';
import { Check, Globe, RefreshCw, Server } from 'lucide-react';
import { useUserSettings } from '../contexts/UserSettingsContext';
import type { UserSettings } from '../types';
import { SUPPORTED_CURRENCIES } from '../lib/currency';
import { Button, Card } from '../components/ui';

export function SettingsPage() {
  const { settings, updateSettings, loading } = useUserSettings();
  const [form, setForm] = useState<UserSettings>(settings);
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setForm(settings);
  }, [settings]);

  async function handleSave() {
    setSaving(true);
    setError(null);
    setSaved(false);
    try {
      await updateSettings(form);
      setSaved(true);
      setTimeout(() => setSaved(false), 2500);
    } catch {
      setError('Failed to save settings. Please try again.');
    } finally {
      setSaving(false);
    }
  }

  const isDirty = JSON.stringify(form) !== JSON.stringify(settings);

  if (loading) {
    return (
      <div>
        <div className="mb-8">
          <h1 className="text-2xl font-bold">Settings</h1>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-2xl">
      <div className="mb-8">
        <h1 className="text-2xl font-bold">Settings</h1>
        <p className="text-sm text-[var(--color-coffer-muted)]">Application configuration</p>
      </div>

      {error && (
        <div className="mb-6 rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>
      )}

      {/* Display Preferences */}
      <Card className="mb-4 p-6">
        <div className="mb-4 flex items-center gap-2">
          <Globe size={16} className="text-[var(--color-coffer-muted)]" />
          <h2 className="font-semibold">Display Preferences</h2>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Default Currency</label>
          <p className="mb-3 text-xs text-[var(--color-coffer-muted)]">
            Bitcoin values on the dashboard and holdings pages will be shown in this currency.
          </p>
          <select
            value={form.currency}
            onChange={(e) => setForm({ ...form, currency: e.target.value })}
            className="w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm focus:border-[var(--color-coffer-orange)] focus:outline-none"
          >
            {SUPPORTED_CURRENCIES.map((c) => (
              <option key={c.code} value={c.code}>
                {c.symbol} {c.label} ({c.code})
              </option>
            ))}
          </select>
        </div>
      </Card>

      {/* Live Price Updates */}
      <Card className="mb-4 p-6">
        <div className="mb-4 flex items-center gap-2">
          <RefreshCw size={16} className="text-[var(--color-coffer-muted)]" />
          <h2 className="font-semibold">Live Price Updates</h2>
        </div>

        <ToggleSetting
          label="Enable live price updates"
          description="Periodically fetch the current Bitcoin price from CoinGecko and push updates via WebSocket. Disable for maximum privacy."
          checked={form.enableLivePriceUpdates}
          onChange={(v) => setForm({ ...form, enableLivePriceUpdates: v })}
        />

        <div className="mt-4">
          <ToggleSetting
            label="Enable price history"
            description="Save Bitcoin price snapshots to the local database for portfolio tracking. Requires live price updates to be enabled."
            checked={form.enablePriceHistory}
            onChange={(v) => setForm({ ...form, enablePriceHistory: v })}
            disabled={!form.enableLivePriceUpdates}
          />
        </div>
      </Card>

      {/* Infrastructure */}
      <Card className="mb-6 p-6">
        <div className="mb-4 flex items-center gap-2">
          <Server size={16} className="text-[var(--color-coffer-muted)]" />
          <h2 className="font-semibold">Infrastructure</h2>
        </div>

        <div>
          <label className="mb-1 block text-sm font-medium">Mempool Explorer URL</label>
          <p className="mb-3 text-xs text-[var(--color-coffer-muted)]">
            Custom mempool.space instance for transaction links. Leave blank to use the default.
          </p>
          <input
            type="url"
            value={form.mempoolExplorerUrl ?? ''}
            onChange={(e) => setForm({ ...form, mempoolExplorerUrl: e.target.value || null })}
            placeholder="https://mempool.space"
            className="w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm placeholder:text-[var(--color-coffer-muted)] focus:border-[var(--color-coffer-orange)] focus:outline-none"
          />
        </div>
      </Card>

      <div className="flex items-center gap-3">
        <Button onClick={handleSave} disabled={saving || !isDirty}>
          {saving ? 'Saving…' : 'Save Settings'}
        </Button>
        {saved && (
          <span className="flex items-center gap-1 text-sm text-green-400">
            <Check size={14} /> Saved
          </span>
        )}
        {isDirty && !saving && (
          <span className="text-xs text-[var(--color-coffer-muted)]">Unsaved changes</span>
        )}
      </div>
    </div>
  );
}

function ToggleSetting({
  label,
  description,
  checked,
  onChange,
  disabled,
}: {
  label: string;
  description: string;
  checked: boolean;
  onChange: (v: boolean) => void;
  disabled?: boolean;
}) {
  return (
    <div className={`flex items-start justify-between gap-4 ${disabled ? 'opacity-50' : ''}`}>
      <div>
        <div className="text-sm font-medium">{label}</div>
        <div className="mt-0.5 text-xs text-[var(--color-coffer-muted)]">{description}</div>
      </div>
      <button
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => !disabled && onChange(!checked)}
        className={`relative mt-0.5 h-5 w-9 flex-shrink-0 cursor-pointer rounded-full transition-colors duration-200 ${
          checked ? 'bg-[var(--color-coffer-orange)]' : 'bg-[var(--color-coffer-border)]'
        } ${disabled ? 'cursor-not-allowed' : ''}`}
      >
        <span
          className={`absolute top-0.5 left-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform duration-200 ${
            checked ? 'translate-x-4' : 'translate-x-0'
          }`}
        />
      </button>
    </div>
  );
}
