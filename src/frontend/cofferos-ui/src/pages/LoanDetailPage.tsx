import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Calendar, Edit2, MinusCircle, PlusCircle } from 'lucide-react';
import { api } from '../api/client';
import type { LoanCollateralTransaction, LoanDetail, LoanHistoricalData } from '../types';
import { Badge, Button, Card, Spinner } from '../components/ui';
import { LoanHistoricalChart } from '../components/LoanHistoricalChart';
import { formatDate, formatPercent } from '../lib/format';
import { formatForDisplay, SUPPORTED_CURRENCIES } from '../lib/currency';
import { useBitcoinPrice } from '../hooks/useBitcoinPrice';
import { useUserSettings } from '../contexts/UserSettingsContext';

const inputClass =
  'w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm outline-none focus:border-[var(--color-coffer-orange)]';

export function LoanDetailPage() {
  const { id } = useParams<{ id: string }>();
  const startDateRef = useRef<HTMLInputElement>(null);
  const [loan, setLoan] = useState<LoanDetail | null>(null);
  const [historicalData, setHistoricalData] = useState<LoanHistoricalData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [collateralTransactions, setCollateralTransactions] = useState<LoanCollateralTransaction[]>([]);
  const [collateralModalMode, setCollateralModalMode] = useState<'add' | 'remove' | null>(null);
  const [collateralForm, setCollateralForm] = useState({ amountBtc: 0, btcPriceAtTime: 0, transactionDate: '', notes: '' });
  const [collateralSaving, setCollateralSaving] = useState(false);
  const [collateralError, setCollateralError] = useState<string | null>(null);
  const { settings } = useUserSettings();
  const { exchangeRates } = useBitcoinPrice();
  const displayCurrency = settings.currency;
  const fmt = (value: number) => formatForDisplay(value, loan?.currency ?? 'USD', displayCurrency, exchangeRates);

  // Editable fields (using human-friendly values where appropriate)
  // These mirror the fields available during initial loan creation.
  const [form, setForm] = useState({
    name: '',
    lender: '',
    principalAmount: 0,
    currentBalance: 0,
    // Interest stored in UI as percent (e.g. 5 for 5%)
    interestRatePercent: 5,
    interestType: 'Fixed',
    // Start date as YYYY-MM-DD for <input type="date">
    loanStartDate: new Date().toISOString().slice(0, 10),
    loanTermMonths: undefined as number | undefined,
    paymentFrequency: 'Monthly',
    interestPaymentSchedule: 'Accruing',
    collateralAmountBtc: 0,
    currentBtcPrice: 0,
    // LTVs stored as percent in the UI (e.g. 80 for 80%)
    warningLtvPercent: 80,
    liquidationLtvPercent: 90,
    collateralCostBasis: 0,
    notes: '',
    currency: 'USD',
  });

  function openCollateralModal(mode: 'add' | 'remove') {
    setCollateralError(null);
    setCollateralForm({
      amountBtc: 0,
      btcPriceAtTime: loan?.currentBtcPrice ?? 0,
      transactionDate: new Date().toISOString().slice(0, 10),
      notes: '',
    });
    setCollateralModalMode(mode);
  }

  async function submitCollateral() {
    if (!id || !loan) return;
    if (collateralForm.amountBtc <= 0) { setCollateralError('Amount must be greater than 0'); return; }
    if (collateralForm.btcPriceAtTime < 0) { setCollateralError('BTC price cannot be negative'); return; }
    setCollateralSaving(true);
    setCollateralError(null);
    try {
      const payload = {
        amountBtc: collateralForm.amountBtc,
        btcPriceAtTime: collateralForm.btcPriceAtTime,
        transactionDate: collateralForm.transactionDate ? new Date(collateralForm.transactionDate).toISOString() : null,
        notes: collateralForm.notes || null,
      };
      const result = collateralModalMode === 'add'
        ? await api.addLoanCollateral(id, payload)
        : await api.removeLoanCollateral(id, payload);
      setLoan(result.loan);
      setCollateralTransactions((prev) => [...prev, result.transaction].sort(
        (a, b) => new Date(a.transactionDate).getTime() - new Date(b.transactionDate).getTime()
      ));
      setCollateralModalMode(null);
    } catch (e) {
      setCollateralError(e instanceof Error ? e.message : 'Failed to update collateral');
    } finally {
      setCollateralSaving(false);
    }
  }

  async function load() {
    if (!id) return;
    setLoading(true);
    setError(null);
    try {
      const d = await api.getLoan(id);
      setLoan(d);
      const startDateStr = d.loanStartDate ? new Date(d.loanStartDate).toISOString().slice(0, 10) : new Date().toISOString().slice(0, 10);
      setForm({
        name: d.name,
        lender: d.lender ?? '',
        principalAmount: d.principalAmount,
        currentBalance: d.currentBalance,
        interestRatePercent: Math.round((d.interestRate ?? 0) * 100 * 100) / 100,
        interestType: d.interestType,
        loanStartDate: startDateStr,
        loanTermMonths: d.loanTermMonths ?? undefined,
        paymentFrequency: d.paymentFrequency,
        interestPaymentSchedule: d.interestPaymentSchedule,
        collateralAmountBtc: d.collateralAmountBtc,
        currentBtcPrice: d.currentBtcPrice,
        warningLtvPercent: Math.round((d.warningLtv ?? 0.8) * 100 * 100) / 100,
        liquidationLtvPercent: Math.round((d.liquidationLtv ?? 0.9) * 100 * 100) / 100,
        collateralCostBasis: d.collateralCostBasis ?? 0,
        notes: d.notes ?? '',
        currency: d.currency ?? 'USD',
      });
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load loan');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, [id]);

  useEffect(() => {
    if (!id) return;
    api.getLoanHistoricalData(id)
      .then((data) => { setHistoricalData(data); })
      .catch((e) => { console.error('Failed to load historical data:', e); });
  }, [id]);

  useEffect(() => {
    if (!id) return;
    api.getLoanCollateralTransactions(id)
      .then((data) => { setCollateralTransactions(data); })
      .catch((e) => { console.error('Failed to load collateral transactions:', e); });
  }, [id]);

  async function save() {
    if (!id || !loan) return;
    setSaving(true);
    setError(null);
    if (form.principalAmount <= 0) {
      setError('Principal amount must be greater than 0');
      setSaving(false);
      return;
    }
    try {
      const payload = {
        name: form.name,
        lender: form.lender || undefined,
        principalAmount: form.principalAmount,
        currentBalance: form.principalAmount,
        interestRate: form.interestRatePercent / 100,
        interestType: form.interestType,
        loanStartDate: new Date(form.loanStartDate).toISOString(),
        loanTermMonths: form.loanTermMonths ?? undefined,
        paymentFrequency: form.paymentFrequency,
        collateralAmountBtc: form.collateralAmountBtc,
        currentBtcPrice: form.currentBtcPrice,
        warningLtv: form.warningLtvPercent / 100,
        liquidationLtv: form.liquidationLtvPercent / 100,
        collateralCostBasis: form.collateralCostBasis || undefined,
        notes: form.notes || undefined,
        interestPaymentSchedule: form.interestPaymentSchedule,
        currency: form.currency,
      };
      const updated = await api.updateLoan(id, payload);
      setLoan(updated);
      setEditing(false);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save');
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <Spinner />;
  if (error && !loan) return <div className="rounded-lg bg-red-500/10 px-4 py-3 text-sm text-red-400">{error}</div>;
  if (!loan) return <div className="text-[var(--color-coffer-muted)]">Loan not found.</div>;

  const ltvColor = loan.currentLtv >= loan.liquidationLtv ? 'text-red-400' : loan.currentLtv >= loan.warningLtv ? 'text-yellow-400' : 'text-emerald-400';

  return (
    <div>
      <Link to="/treasury" className="mb-6 inline-flex items-center gap-2 text-sm text-[var(--color-coffer-muted)] hover:text-white">
        <ArrowLeft size={16} /> Back to Treasury
      </Link>

      <div className="mb-6 flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">{loan.name}</h1>
            <Badge tone={loan.status === 'Active' ? 'orange' : 'default'}>{loan.status}</Badge>
          </div>
          {loan.lender && <p className="text-sm text-[var(--color-coffer-muted)]">Lender: {loan.lender}</p>}
          <p className="mt-1 text-xs text-[var(--color-coffer-muted)]">Created {formatDate(loan.createdAt)}</p>
        </div>
        <div className="text-right">
          <div className="text-2xl font-bold">{fmt(loan.currentBalance)}</div>
          <div className="text-xs text-[var(--color-coffer-muted)]">Current balance</div>
        </div>
      </div>

      {error && <div className="mb-4 rounded-lg bg-red-500/10 px-4 py-2 text-sm text-red-400">{error}</div>}

      <div className="grid gap-4 lg:grid-cols-2">
        {/* General / Financial */}
        <Card className="p-4">
          <div className="mb-3 flex items-center justify-between">
            <div className="text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Financial Details</div>
            {!editing && (
              <button onClick={() => setEditing(true)} className="text-[var(--color-coffer-muted)] hover:text-white"><Edit2 size={16} /></button>
            )}
          </div>

          {editing ? (
            <div className="space-y-3">
              <Field label="Name">
                <input className={inputClass} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
              </Field>
              <Field label="Lender">
                <input className={inputClass} value={form.lender} onChange={(e) => setForm({ ...form, lender: e.target.value })} />
              </Field>
              <Field label="Currency">
                <select className={inputClass} value={form.currency} onChange={(e) => setForm({ ...form, currency: e.target.value })}>
                  {SUPPORTED_CURRENCIES.map((c) => (
                    <option key={c.code} value={c.code}>
                      {c.code} — {c.label}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label={`Principal Amount (${form.currency})`}>
                <input type="number" className={inputClass} value={form.principalAmount} onChange={(e) => setForm({ ...form, principalAmount: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Interest Rate (%)">
                <input type="number" step="0.01" className={inputClass} value={form.interestRatePercent} onChange={(e) => setForm({ ...form, interestRatePercent: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Interest Type">
                <select className={inputClass} value={form.interestType} onChange={(e) => setForm({ ...form, interestType: e.target.value })}>
                  <option>Fixed</option>
                  <option>Variable</option>
                </select>
              </Field>
              <Field label="Payment Frequency">
                <select className={inputClass} value={form.paymentFrequency} onChange={(e) => setForm({ ...form, paymentFrequency: e.target.value })}>
                  <option>Monthly</option>
                  <option>BiWeekly</option>
                  <option>Weekly</option>
                  <option>Quarterly</option>
                  <option>Annually</option>
                  <option>OneTime</option>
                </select>
              </Field>
              <Field label="Interest Payment Schedule">
                <select className={inputClass} value={form.interestPaymentSchedule} onChange={(e) => setForm({ ...form, interestPaymentSchedule: e.target.value })}>
                  <option value="Accruing">Accruing (compounds daily)</option>
                  <option value="InterestOnly">Interest-Only (no accrual)</option>
                </select>
              </Field>
              <Field label="Term (months, optional)">
                <input type="number" className={inputClass} value={form.loanTermMonths ?? ''} onChange={(e) => setForm({ ...form, loanTermMonths: e.target.value ? parseInt(e.target.value) : undefined })} />
              </Field>
              <Field label="Loan Start Date">
                <div
                  className="group relative cursor-pointer rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] pr-2 focus-within:border-[var(--color-coffer-orange)] hover:border-[var(--color-coffer-orange)]/60"
                  title="Click to pick a date"
                  onClick={() => {
                    const el = startDateRef.current;
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
                    ref={startDateRef}
                    type="date"
                    className="w-full cursor-pointer bg-transparent px-3 py-2 pr-8 text-sm outline-none text-[var(--color-coffer-text)] [color-scheme:dark] accent-[var(--color-coffer-orange)] caret-[var(--color-coffer-orange)] [&::-webkit-calendar-picker-indicator]:hidden"
                    value={form.loanStartDate}
                    onChange={(e) => setForm({ ...form, loanStartDate: e.target.value })}
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
                      const el = startDateRef.current;
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
              </Field>
              <Field label="Collateral (BTC)">
                <input type="number" step="0.0001" className={inputClass} value={form.collateralAmountBtc} onChange={(e) => setForm({ ...form, collateralAmountBtc: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label={`BTC Price (${form.currency})`}>
                <input type="number" className={inputClass} value={form.currentBtcPrice} onChange={(e) => setForm({ ...form, currentBtcPrice: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Warning LTV (%)">
                <input type="number" step="0.01" className={inputClass} value={form.warningLtvPercent} onChange={(e) => setForm({ ...form, warningLtvPercent: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Liquidation LTV (%)">
                <input type="number" step="0.01" className={inputClass} value={form.liquidationLtvPercent} onChange={(e) => setForm({ ...form, liquidationLtvPercent: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Collateral Cost Basis">
                <input type="number" className={inputClass} value={form.collateralCostBasis} onChange={(e) => setForm({ ...form, collateralCostBasis: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Notes">
                <textarea className={`${inputClass} h-20`} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
              </Field>
              <div className="flex gap-2 pt-2">
                <Button onClick={save} disabled={saving}>{saving ? 'Saving...' : 'Save'}</Button>
                <Button variant="ghost" onClick={() => {
                  setEditing(false);
                  if (loan) {
                    const startDateStr = loan.loanStartDate ? new Date(loan.loanStartDate).toISOString().slice(0, 10) : new Date().toISOString().slice(0, 10);
                    setForm({
                      name: loan.name,
                      lender: loan.lender ?? '',
                      principalAmount: loan.principalAmount,
                      currentBalance: loan.currentBalance,
                      interestRatePercent: Math.round((loan.interestRate ?? 0) * 100 * 100) / 100,
                      interestType: loan.interestType,
                      loanStartDate: startDateStr,
                      loanTermMonths: loan.loanTermMonths ?? undefined,
                      paymentFrequency: loan.paymentFrequency,
                      interestPaymentSchedule: loan.interestPaymentSchedule,
                      collateralAmountBtc: loan.collateralAmountBtc,
                      currentBtcPrice: loan.currentBtcPrice,
                      warningLtvPercent: Math.round((loan.warningLtv ?? 0.8) * 100 * 100) / 100,
                      liquidationLtvPercent: Math.round((loan.liquidationLtv ?? 0.9) * 100 * 100) / 100,
                      collateralCostBasis: loan.collateralCostBasis ?? 0,
                      notes: loan.notes ?? '',
                      currency: loan.currency ?? 'USD',
                    });
                  }
                }}>Cancel</Button>
              </div>
            </div>
          ) : (
            <div className="space-y-2 text-sm">
              <Row label="Principal" value={fmt(loan.principalAmount)} />
              <Row label="Current Balance" value={fmt(loan.currentBalance)} />
              <Row label="Interest Rate" value={`${(loan.interestRate * 100).toFixed(2)}% (${loan.interestType})`} />
              <Row label="Started" value={formatDate(loan.loanStartDate)} />
              {loan.loanTermMonths && <Row label="Term" value={`${loan.loanTermMonths} months`} />}
              <Row label="Payment Frequency" value={loan.paymentFrequency} />
              <Row label="Payment Type" value={loan.interestPaymentSchedule === 'InterestOnly' ? 'Interest-Only' : 'Accruing'} />
              {loan.interestPaymentSchedule === 'InterestOnly' && (
                <Row 
                  label="Monthly Interest Payment" 
                  value={fmt((loan.principalAmount * loan.interestRate) / 12)} 
                />
              )}
              <Row label="Warning LTV" value={formatPercent(loan.warningLtv)} />
              <Row label="Liquidation LTV" value={formatPercent(loan.liquidationLtv)} />
            </div>
          )}
        </Card>

        {/* Collateral & LTV */}
        <Card className="p-4">
          <div className="mb-3 flex items-center justify-between">
            <div className="text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Collateral &amp; Risk</div>
            {loan.status === 'Active' && (
              <div className="flex gap-2">
                <button
                  onClick={() => openCollateralModal('add')}
                  className="flex items-center gap-1 rounded px-2 py-1 text-xs text-emerald-400 hover:bg-emerald-400/10"
                  title="Add Collateral"
                >
                  <PlusCircle size={14} /> Add
                </button>
                <button
                  onClick={() => openCollateralModal('remove')}
                  className="flex items-center gap-1 rounded px-2 py-1 text-xs text-red-400 hover:bg-red-400/10"
                  title="Remove Collateral"
                >
                  <MinusCircle size={14} /> Remove
                </button>
              </div>
            )}
          </div>
          <div className="space-y-2 text-sm">
            <Row label="Collateral" value={`${loan.collateralAmountBtc.toFixed(4)} BTC`} />
            <Row label="BTC Price" value={fmt(loan.currentBtcPrice)} />
            <Row label="Collateral Cost Basis" value={fmt(loan.collateralCostBasis)} />
            <Row label="Unrealized P&L" value={fmt(loan.currentCollateralValue - loan.collateralCostBasis)} />
            <Row label="Collateral Value" value={fmt(loan.currentCollateralValue)} />
            <Row label="Current LTV" value={<span className={ltvColor}>{formatPercent(loan.currentLtv)}</span>} />
            <Row label="Warning Threshold" value={formatPercent(loan.warningLtv)} />
            <Row label="Liquidation Threshold" value={formatPercent(loan.liquidationLtv)} />
            <Row label="Warning Price" value={fmt(loan.warningPrice)} />
            <Row label="Liquidation Price" value={fmt(loan.liquidationPrice)} />
            <Row label="Distance to Warning" value={formatPercent(loan.distanceToWarning)} />
            <Row label="Distance to Liquidation" value={formatPercent(loan.distanceToLiquidation)} />
            <Row label="Collateral Buffer" value={`${loan.remainingCollateralBuffer.toFixed(4)} BTC`} />
          </div>
        </Card>
      </div>

      {/* Historical LTV Chart */}
      {historicalData && historicalData.snapshots.length > 0 && (
        <Card className="mt-6 p-4">
          <div className="mb-4 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Historical LTV & Price</div>
          <LoanHistoricalChart 
            data={historicalData} 
            warningLtv={loan.warningLtv}
            liquidationLtv={loan.liquidationLtv}
            currency={loan.currency}
            displayCurrency={displayCurrency}
            exchangeRates={exchangeRates}
          />
        </Card>
      )}
      {historicalData && historicalData.snapshots.length === 0 && (
        <Card className="mt-6 p-4">
          <div className="mb-2 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Historical LTV & Price</div>
          <p className="text-sm text-[var(--color-coffer-muted)]">
            No historical price data is available yet. The next successful price fetch will populate this chart.
          </p>
        </Card>
      )}

      {/* Collateral History */}
      <Card className="mt-6 p-4">
        <div className="mb-3 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Collateral History</div>
        {collateralTransactions.length === 0 ? (
          <p className="text-sm text-[var(--color-coffer-muted)]">No collateral adjustments recorded yet.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead>
                <tr className="border-b border-[var(--color-coffer-border)] text-left text-[var(--color-coffer-muted)]">
                  <th className="pb-2 pr-4 font-medium">Date</th>
                  <th className="pb-2 pr-4 font-medium">Type</th>
                  <th className="pb-2 pr-4 font-medium text-right">Amount (BTC)</th>
                  <th className="pb-2 pr-4 font-medium text-right">BTC Price</th>
                  <th className="pb-2 pr-4 font-medium text-right">Collateral After</th>
                  <th className="pb-2 pr-4 font-medium text-right">LTV After</th>
                  <th className="pb-2 font-medium">Notes</th>
                </tr>
              </thead>
              <tbody>
                {[...collateralTransactions].reverse().map((txn) => (
                  <tr key={txn.id} className="border-b border-[var(--color-coffer-border)]/30 py-1">
                    <td className="py-2 pr-4 text-[var(--color-coffer-muted)]">{formatDate(txn.transactionDate)}</td>
                    <td className="py-2 pr-4">
                      <span className={txn.transactionType === 'Added' ? 'text-emerald-400' : 'text-red-400'}>
                        {txn.transactionType === 'Added' ? '▲ Added' : '▼ Removed'}
                      </span>
                    </td>
                    <td className="py-2 pr-4 text-right font-medium">{txn.amountBtc.toFixed(8)}</td>
                    <td className="py-2 pr-4 text-right">{fmt(txn.btcPriceAtTime)}</td>
                    <td className="py-2 pr-4 text-right">{txn.collateralAmountBtcAfter.toFixed(8)} BTC</td>
                    <td className="py-2 pr-4 text-right">
                      <span className={
                        txn.ltvAtTime >= loan.liquidationLtv ? 'text-red-400'
                        : txn.ltvAtTime >= loan.warningLtv ? 'text-yellow-400'
                        : 'text-emerald-400'
                      }>
                        {formatPercent(txn.ltvAtTime)}
                      </span>
                    </td>
                    <td className="py-2 text-[var(--color-coffer-muted)]">{txn.notes ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Notes */}
      <Card className="mt-4 p-4">
        <div className="mb-2 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Notes</div>
        {editing ? (
          <textarea className={`${inputClass} h-24 w-full`} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        ) : (
          <p className="whitespace-pre-wrap text-sm">{loan.notes || '—'}</p>
        )}
      </Card>

      {/* Collateral Adjustment Modal */}
      {collateralModalMode && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4">
          <div className="w-full max-w-md rounded-xl border border-[var(--color-coffer-border)] bg-[var(--color-coffer-surface)] p-6 shadow-xl">
            <h2 className="mb-4 text-lg font-bold">
              {collateralModalMode === 'add' ? '▲ Add Collateral' : '▼ Remove Collateral'}
            </h2>

            {collateralError && (
              <div className="mb-4 rounded bg-red-500/10 px-3 py-2 text-sm text-red-400">{collateralError}</div>
            )}

            <div className="space-y-3">
              <Field label="Amount (BTC)">
                <input
                  type="number"
                  step="0.00000001"
                  min="0"
                  className={inputClass}
                  value={collateralForm.amountBtc || ''}
                  onChange={(e) => setCollateralForm({ ...collateralForm, amountBtc: parseFloat(e.target.value) || 0 })}
                  autoFocus
                />
              </Field>
              <Field label={`BTC Price (${loan.currency})`}>
                <input
                  type="number"
                  className={inputClass}
                  value={collateralForm.btcPriceAtTime || ''}
                  onChange={(e) => setCollateralForm({ ...collateralForm, btcPriceAtTime: parseFloat(e.target.value) || 0 })}
                />
              </Field>
              <Field label="Date">
                <input
                  type="date"
                  className={`${inputClass} [color-scheme:dark]`}
                  value={collateralForm.transactionDate}
                  onChange={(e) => setCollateralForm({ ...collateralForm, transactionDate: e.target.value })}
                />
              </Field>
              <Field label="Notes (optional)">
                <textarea
                  className={`${inputClass} h-16`}
                  value={collateralForm.notes}
                  onChange={(e) => setCollateralForm({ ...collateralForm, notes: e.target.value })}
                />
              </Field>

              {collateralForm.amountBtc > 0 && collateralForm.btcPriceAtTime > 0 && (
                <div className="rounded bg-[var(--color-coffer-bg)]/60 px-3 py-2 text-xs text-[var(--color-coffer-muted)]">
                  <span className="font-medium text-white">Preview: </span>
                  {collateralModalMode === 'add' ? (
                    <>New collateral: {(loan.collateralAmountBtc + collateralForm.amountBtc).toFixed(8)} BTC</>
                  ) : (
                    <>New collateral: {Math.max(0, loan.collateralAmountBtc - collateralForm.amountBtc).toFixed(8)} BTC</>
                  )}
                </div>
              )}

              <div className="flex gap-2 pt-2">
                <Button
                  onClick={submitCollateral}
                  disabled={collateralSaving}
                  className={collateralModalMode === 'remove' ? 'bg-red-500 hover:bg-red-600' : undefined}
                >
                  {collateralSaving ? 'Saving...' : collateralModalMode === 'add' ? 'Add Collateral' : 'Remove Collateral'}
                </Button>
                <Button variant="ghost" onClick={() => setCollateralModalMode(null)} disabled={collateralSaving}>
                  Cancel
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between border-b border-[var(--color-coffer-border)]/50 py-1">
      <span className="text-[var(--color-coffer-muted)]">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  );
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <div className="mb-1 text-xs font-medium text-[var(--color-coffer-muted)]">{label}</div>
      {children}
    </div>
  );
}
