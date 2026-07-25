import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Calendar, Edit2 } from 'lucide-react';
import { api } from '../api/client';
import type { LoanDetail } from '../types';
import { Badge, Button, Card, Spinner } from '../components/ui';
import { formatDate, formatPercent, formatUsd } from '../lib/format';

const inputClass =
  'w-full rounded-lg border border-[var(--color-coffer-border)] bg-[var(--color-coffer-bg)] px-3 py-2 text-sm outline-none focus:border-[var(--color-coffer-orange)]';

export function LoanDetailPage() {
  const { id } = useParams<{ id: string }>();
  const startDateRef = useRef<HTMLInputElement>(null);
  const [loan, setLoan] = useState<LoanDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editing, setEditing] = useState(false);
  const [saving, setSaving] = useState(false);

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
    collateralAmountBtc: 0,
    currentBtcPrice: 0,
    // LTVs stored as percent in the UI (e.g. 80 for 80%)
    warningLtvPercent: 80,
    liquidationLtvPercent: 90,
    notes: '',
  });

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
        collateralAmountBtc: d.collateralAmountBtc,
        currentBtcPrice: d.currentBtcPrice,
        warningLtvPercent: Math.round((d.warningLtv ?? 0.8) * 100 * 100) / 100,
        liquidationLtvPercent: Math.round((d.liquidationLtv ?? 0.9) * 100 * 100) / 100,
        notes: d.notes ?? '',
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

  async function save() {
    if (!id || !loan) return;
    setSaving(true);
    setError(null);
    try {
      const payload = {
        name: form.name,
        lender: form.lender || undefined,
        principalAmount: form.principalAmount,
        currentBalance: form.currentBalance,
        interestRate: form.interestRatePercent / 100,
        interestType: form.interestType,
        loanStartDate: new Date(form.loanStartDate).toISOString(),
        loanTermMonths: form.loanTermMonths ?? undefined,
        paymentFrequency: form.paymentFrequency,
        collateralAmountBtc: form.collateralAmountBtc,
        currentBtcPrice: form.currentBtcPrice,
        warningLtv: form.warningLtvPercent / 100,
        liquidationLtv: form.liquidationLtvPercent / 100,
        notes: form.notes || undefined,
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

  async function updateBalance(newBalance: number) {
    if (!id) return;
    try {
      const updated = await api.updateLoanBalance(id, { currentBalance: newBalance });
      setLoan(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update balance');
    }
  }

  async function updateCollateral(collateral: number, price: number) {
    if (!id) return;
    try {
      const updated = await api.updateLoanCollateral(id, { collateralAmountBtc: collateral, currentBtcPrice: price });
      setLoan(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update collateral');
    }
  }

  async function setGlobalPrice(price: number) {
    try {
      await api.setBtcPrice({ price });
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to set price');
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
          <div className="text-2xl font-bold">{formatUsd(loan.currentBalance)}</div>
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
              <Field label="Principal Amount (USD)">
                <input type="number" className={inputClass} value={form.principalAmount} onChange={(e) => setForm({ ...form, principalAmount: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Current Balance (USD)">
                <input type="number" className={inputClass} value={form.currentBalance} onChange={(e) => setForm({ ...form, currentBalance: parseFloat(e.target.value) || 0 })} />
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
                    readOnly
                    className="w-full cursor-pointer bg-transparent px-3 py-2 pr-8 text-sm outline-none [&::-webkit-calendar-picker-indicator]:hidden"
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
              <Field label="BTC Price (USD)">
                <input type="number" className={inputClass} value={form.currentBtcPrice} onChange={(e) => setForm({ ...form, currentBtcPrice: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Warning LTV (%)">
                <input type="number" step="0.01" className={inputClass} value={form.warningLtvPercent} onChange={(e) => setForm({ ...form, warningLtvPercent: parseFloat(e.target.value) || 0 })} />
              </Field>
              <Field label="Liquidation LTV (%)">
                <input type="number" step="0.01" className={inputClass} value={form.liquidationLtvPercent} onChange={(e) => setForm({ ...form, liquidationLtvPercent: parseFloat(e.target.value) || 0 })} />
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
                      collateralAmountBtc: loan.collateralAmountBtc,
                      currentBtcPrice: loan.currentBtcPrice,
                      warningLtvPercent: Math.round((loan.warningLtv ?? 0.8) * 100 * 100) / 100,
                      liquidationLtvPercent: Math.round((loan.liquidationLtv ?? 0.9) * 100 * 100) / 100,
                      notes: loan.notes ?? '',
                    });
                  }
                }}>Cancel</Button>
              </div>
            </div>
          ) : (
            <div className="space-y-2 text-sm">
              <Row label="Principal" value={formatUsd(loan.principalAmount)} />
              <Row label="Current Balance" value={formatUsd(loan.currentBalance)} />
              <Row label="Interest Rate" value={`${(loan.interestRate * 100).toFixed(2)}% (${loan.interestType})`} />
              <Row label="Started" value={formatDate(loan.loanStartDate)} />
              {loan.loanTermMonths && <Row label="Term" value={`${loan.loanTermMonths} months`} />}
              <Row label="Payment Frequency" value={loan.paymentFrequency} />
              <Row label="Warning LTV" value={formatPercent(loan.warningLtv)} />
              <Row label="Liquidation LTV" value={formatPercent(loan.liquidationLtv)} />
            </div>
          )}
        </Card>

        {/* Collateral & LTV */}
        <Card className="p-4">
          <div className="mb-3 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Collateral &amp; Risk</div>
          <div className="space-y-2 text-sm">
            <Row label="Collateral" value={`${loan.collateralAmountBtc.toFixed(4)} BTC`} />
            <Row label="BTC Price" value={formatUsd(loan.currentBtcPrice)} />
            <Row label="Collateral Value" value={formatUsd(loan.currentCollateralValue)} />
            <Row label="Current LTV" value={<span className={ltvColor}>{formatPercent(loan.currentLtv)}</span>} />
            <Row label="Warning Threshold" value={formatPercent(loan.warningLtv)} />
            <Row label="Liquidation Threshold" value={formatPercent(loan.liquidationLtv)} />
            <Row label="Warning Price" value={formatUsd(loan.warningPrice)} />
            <Row label="Liquidation Price" value={formatUsd(loan.liquidationPrice)} />
            <Row label="Distance to Warning" value={formatPercent(loan.distanceToWarning)} />
            <Row label="Distance to Liquidation" value={formatPercent(loan.distanceToLiquidation)} />
            <Row label="Collateral Buffer" value={`${loan.remainingCollateralBuffer.toFixed(4)} BTC`} />
          </div>
        </Card>
      </div>

      {/* Quick actions */}
      <div className="mt-6 grid gap-4 lg:grid-cols-2">
        <Card className="p-4">
          <div className="mb-2 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Update Balance</div>
          <div className="flex gap-2">
            <input type="number" className={inputClass} defaultValue={loan.currentBalance} id="bal" />
            <Button onClick={() => {
              const el = document.getElementById('bal') as HTMLInputElement;
              const v = parseFloat(el.value);
              if (!isNaN(v)) updateBalance(v);
            }}>Update</Button>
          </div>
          <p className="mt-1 text-xs text-[var(--color-coffer-muted)]">Enter new outstanding principal balance.</p>
        </Card>

        <Card className="p-4">
          <div className="mb-2 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Update Collateral / Price</div>
          <div className="flex gap-2">
            <input type="number" step="0.0001" className={inputClass + ' w-28'} defaultValue={loan.collateralAmountBtc} id="col" />
            <input type="number" className={inputClass + ' flex-1'} defaultValue={loan.currentBtcPrice} id="pr" />
            <Button onClick={() => {
              const c = parseFloat((document.getElementById('col') as HTMLInputElement).value);
              const p = parseFloat((document.getElementById('pr') as HTMLInputElement).value);
              if (!isNaN(c) && !isNaN(p)) updateCollateral(c, p);
            }}>Update</Button>
          </div>
          <div className="mt-2">
            <Button variant="ghost" onClick={() => {
              const p = parseFloat((document.getElementById('pr') as HTMLInputElement).value);
              if (!isNaN(p)) setGlobalPrice(p);
            }}>Apply price to all active loans</Button>
          </div>
        </Card>
      </div>

      {/* Notes */}
      <Card className="mt-4 p-4">
        <div className="mb-2 text-sm font-semibold uppercase tracking-wide text-[var(--color-coffer-muted)]">Notes</div>
        {editing ? (
          <textarea className={`${inputClass} h-24 w-full`} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        ) : (
          <p className="whitespace-pre-wrap text-sm">{loan.notes || '—'}</p>
        )}
      </Card>
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
