import { useState } from 'react';
import { Building2, HardDrive, Landmark, Lock, Wallet, X, Zap } from 'lucide-react';
import { Button } from './ui';
import { ImportWalletModal } from './ImportWalletModal';
import { CreateLoanModal } from './CreateLoanModal';
import { CreateRetirementAccountModal } from './CreateRetirementAccountModal';

interface Props {
  onClose: () => void;
  onComplete: () => void;
}

type HoldingChoice = 'wallet' | 'loan' | 'retirement' | null;

interface HoldingOption {
  id: HoldingChoice;
  icon: React.ReactNode;
  label: string;
  description: string;
  enabled: boolean;
}

const holdingOptions: HoldingOption[] = [
  {
    id: 'wallet',
    icon: <Wallet size={20} />,
    label: 'Wallet',
    description: 'Import a watch-only wallet via xpub or descriptor',
    enabled: true,
  },
  {
    id: 'loan',
    icon: <Lock size={20} />,
    label: 'Bitcoin-backed Loan',
    description: 'Track collateral pledged against a loan',
    enabled: true,
  },
  {
    id: null,
    icon: <Zap size={20} />,
    label: 'Lightning',
    description: 'Lightning channel balances',
    enabled: false,
  },
  {
    id: 'retirement',
    icon: <Building2 size={20} />,
    label: 'Retirement Account',
    description: 'IRA or 401k Bitcoin positions',
    enabled: true,
  },
  {
    id: null,
    icon: <Landmark size={20} />,
    label: 'ETF Position',
    description: 'Bitcoin ETF holdings',
    enabled: false,
  },
  {
    id: null,
    icon: <HardDrive size={20} />,
    label: 'Manual Holding',
    description: 'Manually enter a Bitcoin balance',
    enabled: false,
  },
];

export function AddHoldingWizard({ onClose, onComplete }: Props) {
  const [selected, setSelected] = useState<HoldingChoice>(null);
  const [showWalletImport, setShowWalletImport] = useState(false);
  const [showLoanCreate, setShowLoanCreate] = useState(false);
  const [showRetirementCreate, setShowRetirementCreate] = useState(false);

  function handleContinue() {
    if (selected === 'wallet') {
      setShowWalletImport(true);
    } else if (selected === 'loan') {
      setShowLoanCreate(true);
    } else if (selected === 'retirement') {
      setShowRetirementCreate(true);
    }
  }

  if (showWalletImport) {
    return (
      <ImportWalletModal
        onClose={onClose}
        onImported={() => {
          onComplete();
          onClose();
        }}
      />
    );
  }

  if (showLoanCreate) {
    return (
      <CreateLoanModal
        onClose={onClose}
        onCreated={() => {
          onComplete();
          onClose();
        }}
      />
    );
  }

  if (showRetirementCreate) {
    return (
      <CreateRetirementAccountModal
        onClose={onClose}
        onCreated={() => {
          onComplete();
          onClose();
        }}
      />
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
      <div className="w-full max-w-lg rounded-2xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] p-6">
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-bold">How do you hold this Bitcoin?</h2>
          <button onClick={onClose} className="text-[var(--color-coffer-muted)] hover:text-white">
            <X size={20} />
          </button>
        </div>

        <div className="space-y-2">
          {holdingOptions.map((option, idx) => (
            <button
              key={idx}
              disabled={!option.enabled}
              onClick={() => option.enabled && option.id && setSelected(option.id)}
              className={`w-full flex items-center gap-3 rounded-lg border p-3 text-left transition ${
                !option.enabled
                  ? 'border-[var(--color-coffer-border)] opacity-40 cursor-not-allowed'
                  : selected === option.id
                    ? 'border-[var(--color-coffer-orange)] bg-[var(--color-coffer-orange)]/10'
                    : 'border-[var(--color-coffer-border)] hover:border-[var(--color-coffer-orange)]/50'
              }`}
            >
              <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[var(--color-coffer-border)] text-[var(--color-coffer-muted)]">
                {option.icon}
              </div>
              <div className="flex-1">
                <div className="text-sm font-medium">
                  {option.label}
                  {!option.enabled && (
                    <span className="ml-2 text-xs text-[var(--color-coffer-muted)]">(Coming Soon)</span>
                  )}
                </div>
                <div className="text-xs text-[var(--color-coffer-muted)]">{option.description}</div>
              </div>
              {option.enabled && (
                <div className={`h-4 w-4 shrink-0 rounded-full border-2 ${
                  selected === option.id
                    ? 'border-[var(--color-coffer-orange)] bg-[var(--color-coffer-orange)]'
                    : 'border-[var(--color-coffer-border)]'
                }`} />
              )}
            </button>
          ))}
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
          <Button onClick={handleContinue} disabled={!selected}>Continue</Button>
        </div>
      </div>
    </div>
  );
}
