import { useEffect, useRef, useState } from 'react';
import { Calendar, X } from 'lucide-react';
import { api } from '../api/client';
import type { BitcoinPriceInfo, WalletSummary } from '../types';
import { Button } from './ui';

const SUPPORTED_CURRENCIES: string[] = ['USD', 'EUR', 'GBP', 'CAD', 'AUD', 'CHF', 'JPY'];

interface Props {
  onClose: () => void;
  onCreated: () => void;
}

export function CreateLoanModal({ onClose, onCreated }: Props) {
  const dateInputRef = useRef<HTMLInputElement>(null);
  const [name, setName] = useState('');
  const [lender, setLender] = useState('');
  const [principalAmount, setPrincipalAmount] = useState(0);
  // Store as percent in the UI (e.g. 5 for 5%) so users think in normal terms.
  const [interestRatePercent, setInterestRatePercent] = useState(5);
  const [interestType, setInterestType] = useState('Fixed');
  const [loanStartDate, setLoanStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [loanTermMonths, setLoanTermMonths] = useState<number | undefined>(12);
  const [paymentFrequency, setPaymentFrequency] = useState('Monthly');
  const [interestPaymentSchedule, setInterestPaymentSchedule] = useState('Accruing');
  const [collateralAmountBtc, setCollateralAmountBtc] = useState(0);
  const [currentBtcPrice, setCurrentBtcPrice] = useState(100000);
  const [warningLtvPercent, setWarningLtvPercent] = useState(80);
  const [liquidationLtvPercent, setLiquidationLtvPercent] = useState(90);
  const [notes, setNotes] = useState('');
  const [collateralCostBasis, setCollateralCostBasis] = useState(0);
  const [currency, setCurrency] = useState('USD');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [priceInfo, setPriceInfo] = useState<BitcoinPriceInfo | null>(null);

  // Wallet relationship (optional)
  const [wallets, setWallets] = useState<WalletSummary[]>([]);
  const [collateralWalletLinked, setCollateralWalletLinked] = useState<'yes' | 'no' | null>(null);
  const [linkedWalletId, setLinkedWalletId] = useState<string>('');

  function applyPrice(info: BitcoinPriceInfo | null, cur: string) {
    if (!info) return;
    const key = cur.toLowerCase();
    const rate = info.exchangeRates[key];
    if (rate != null && rate > 0) {
      setCurrentBtcPrice(rate);
    } else if (cur === 'USD' && info.priceUsd != null) {
      setCurrentBtcPrice(info.priceUsd);
    }
  }

  useEffect(() => {
    api.getBitcoinPrice()
      .then((info) => {
        setPriceInfo(info);
        applyPrice(info, currency);
      })
      .catch(() => {
        // leave the default if the price service is unavailable
      });
    api.listWallets()
      .then(setWallets)
      .catch(() => {});
  }, []);


  async function submit() {
    setError(null);
    if (!name.trim()) {
      setError('Name is required');
      return;
    }
    if (principalAmount <= 0) {
      setError('Principal amount must be greater than 0');
      return;
    }
    if (currentBtcPrice < 0) {
      setError('BTC price cannot be negative');
      return;
    }
    if (collateralAmountBtc <= 0) {
      setError('Collateral must be greater than 0 BTC');
      return;
    }
    if (liquidationLtvPercent <= warningLtvPercent) {
      setError('Liquidation LTV must be greater than warning LTV');
      return;
    }
    setSubmitting(true);
    try {
      await api.createLoan({
        name: name.trim(),
        lender: lender.trim() || undefined,
        principalAmount,
        currentBalance: principalAmount,
        interestRate: interestRatePercent / 100,
        interestType,
        loanStartDate: new Date(loanStartDate).toISOString(),
        loanTermMonths: loanTermMonths || undefined,
        paymentFrequency,
        collateralAmountBtc,
        currentBtcPrice,
        warningLtv: warningLtvPercent / 100,
        liquidationLtv: liquidationLtvPercent / 100,
        collateralCostBasis: collateralCostBasis || undefined,
        notes: notes.trim() || undefined,
        interestPaymentSchedule,
        currency,
      });
      onCreated();
      onClose();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to create loan');
    } finally {
      setSubmitting(false);
    }
  }

  const inputClass =
    'w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm outline-none focus:border-[var(--color-coffer-orange)]';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4 overflow-y-auto">
      <div className="w-full max-w-2xl rounded-2xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-panel)] p-6 my-8">
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-bold">Create Bitcoin-backed loan</h2>
          <button onClick={onClose} className="text-[var(--color-coffer-muted)] hover:text-white">
            <X size={20} />
          </button>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-500/10 px-3 py-2 text-sm text-red-400">{error}</div>}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div className="md:col-span-2">
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Loan Name</label>
            <input className={inputClass} value={name} onChange={(e) => setName(e.target.value)} placeholder="Unchained Loan 2025" />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Lender (optional)</label>
            <input className={inputClass} value={lender} onChange={(e) => setLender(e.target.value)} placeholder="Unchained / Private" />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Loan Start Date</label>
            <div
              className="group relative cursor-pointer rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] pr-2 focus-within:border-[var(--color-coffer-orange)] hover:border-[var(--color-coffer-orange)]/60"
              title="Click to pick a date"
              onClick={() => {
                const el = dateInputRef.current;
                if (el) {
                  if (typeof (el as any).showPicker === 'function') {
                    (el as any).showPicker();
                  } else {
                    el.focus();
                    el.click();
                  }
                }
              }}
            >
              <input
                ref={dateInputRef}
                type="date"
                className="w-full cursor-pointer bg-transparent px-3 py-2 pr-8 text-sm outline-none text-[var(--color-coffer-text)] [color-scheme:dark] accent-[var(--color-coffer-orange)] caret-[var(--color-coffer-orange)] [&::-webkit-calendar-picker-indicator]:hidden"
                value={loanStartDate}
                onChange={(e) => setLoanStartDate(e.target.value)}
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
                  const el = dateInputRef.current;
                  if (el) {
                    if (typeof (el as any).showPicker === 'function') {
                      (el as any).showPicker();
                    } else {
                      el.focus();
                      el.click();
                    }
                  }
                }}
                className="absolute right-1 top-1/2 -translate-y-1/2 rounded p-1 text-[var(--color-coffer-muted)] hover:bg-[var(--color-coffer-border)] hover:text-[var(--color-coffer-orange)] focus:outline-none focus:ring-1 focus:ring-[var(--color-coffer-orange)]"
                aria-label="Open date picker"
              >
                <Calendar size={18} />
              </button>
            </div>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Loan Currency</label>
            <select className={inputClass} value={currency} onChange={(e) => {
              const cur = e.target.value;
              setCurrency(cur);
              applyPrice(priceInfo, cur);
            }}>
              {SUPPORTED_CURRENCIES.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Principal Amount ({currency})</label>
            <input type="number" className={inputClass} value={principalAmount} onChange={(e) => setPrincipalAmount(parseFloat(e.target.value) || 0)} />
          </div>


          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Interest Rate (%)</label>
            <input type="number" step="0.01" className={inputClass} value={interestRatePercent} onChange={(e) => setInterestRatePercent(parseFloat(e.target.value) || 0)} />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Interest Type</label>
            <select className={inputClass} value={interestType} onChange={(e) => setInterestType(e.target.value)}>
              <option>Fixed</option>
              <option>Variable</option>
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Payment Frequency</label>
            <select className={inputClass} value={paymentFrequency} onChange={(e) => setPaymentFrequency(e.target.value)}>
              <option>Monthly</option>
              <option>BiWeekly</option>
              <option>Weekly</option>
              <option>Quarterly</option>
              <option>Annually</option>
              <option>OneTime</option>
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Interest Payment Schedule</label>
            <select className={inputClass} value={interestPaymentSchedule} onChange={(e) => setInterestPaymentSchedule(e.target.value)}>
              <option value="Accruing">Accruing (compounds daily)</option>
              <option value="InterestOnly">Interest-Only (no accrual)</option>
            </select>
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Term (months, optional)</label>
            <input type="number" className={inputClass} value={loanTermMonths ?? ''} onChange={(e) => setLoanTermMonths(e.target.value ? parseInt(e.target.value) : undefined)} />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Collateral (BTC)</label>
            <input type="number" step="0.0001" className={inputClass} value={collateralAmountBtc} onChange={(e) => setCollateralAmountBtc(parseFloat(e.target.value) || 0)} />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Current BTC Price ({currency})</label>
            <input type="number" className={inputClass} value={currentBtcPrice} onChange={(e) => setCurrentBtcPrice(parseFloat(e.target.value) || 0)} />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Warning LTV % (e.g. 80)</label>
            <input type="number" step="0.01" className={inputClass} value={warningLtvPercent} onChange={(e) => setWarningLtvPercent(parseFloat(e.target.value) || 0)} />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Liquidation LTV % (e.g. 90)</label>
            <input type="number" step="0.01" className={inputClass} value={liquidationLtvPercent} onChange={(e) => setLiquidationLtvPercent(parseFloat(e.target.value) || 0)} />
          </div>

          <div>
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Collateral Cost Basis</label>
            <input type="number" className={inputClass} value={collateralCostBasis} onChange={(e) => setCollateralCostBasis(parseFloat(e.target.value) || 0)} />
          </div>

          <div className="md:col-span-2">
            <label className="mb-1 block text-xs font-medium text-[var(--color-coffer-muted)]">Notes (optional)</label>
            <textarea className={`${inputClass} h-20`} value={notes} onChange={(e) => setNotes(e.target.value)} />
          </div>

          {/* Wallet relationship */}
          {wallets.length > 0 && (
            <div className="md:col-span-2 rounded-lg border border-[var(--color-coffer-border)] p-4">
              <label className="mb-2 block text-xs font-medium text-[var(--color-coffer-muted)]">
                Is the collateral represented by one of your imported wallets?
              </label>
              <div className="flex gap-4 mb-3">
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <input
                    type="radio"
                    name="walletLinked"
                    checked={collateralWalletLinked === 'yes'}
                    onChange={() => setCollateralWalletLinked('yes')}
                    className="accent-[var(--color-coffer-orange)]"
                  />
                  Yes
                </label>
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <input
                    type="radio"
                    name="walletLinked"
                    checked={collateralWalletLinked === 'no'}
                    onChange={() => { setCollateralWalletLinked('no'); setLinkedWalletId(''); }}
                    className="accent-[var(--color-coffer-orange)]"
                  />
                  No
                </label>
              </div>
              {collateralWalletLinked === 'yes' && (
                <select
                  className={inputClass}
                  value={linkedWalletId}
                  onChange={(e) => setLinkedWalletId(e.target.value)}
                >
                  <option value="">Select a wallet...</option>
                  {wallets.map((w) => (
                    <option key={w.id} value={w.id}>{w.name}</option>
                  ))}
                </select>
              )}
              <p className="mt-2 text-xs text-[var(--color-coffer-muted)]">
                Linking a wallet helps track collateral without duplicating ownership in holdings.
              </p>
            </div>
          )}
        </div>

        <div className="mt-6 flex gap-3">
          <Button onClick={submit} disabled={submitting}>{submitting ? 'Creating...' : 'Create Loan'}</Button>
          <Button variant="ghost" onClick={onClose}>Cancel</Button>
        </div>

        <p className="mt-3 text-xs text-[var(--color-coffer-muted)]">
          All data stays local. No keys or external services required for Phase 1.
        </p>
      </div>
    </div>
  );
}
