import { useMemo, useState } from 'react';
import type { LoanHistoricalData } from '../types';
import { Spinner } from './ui';
import { convertCurrency } from '../lib/currency';

interface Props {
  data: LoanHistoricalData;
  warningLtv: number;
  liquidationLtv: number;
  currency?: string;
  displayCurrency?: string;
  exchangeRates?: Record<string, number>;
}

export function LoanHistoricalChart({ data, warningLtv, liquidationLtv, currency, displayCurrency, exchangeRates }: Props) {
  const rates = exchangeRates ?? {};
  const supported = ['USD', 'EUR', 'GBP', 'CAD', 'AUD', 'CHF', 'JPY'];
  const raw = (currency ?? data.currency ?? 'USD').toUpperCase();
  const loanCurrency = supported.includes(raw) ? raw : 'USD';
  const targetRaw = (displayCurrency ?? loanCurrency).toUpperCase();
  const targetCurrency = supported.includes(targetRaw) ? targetRaw : loanCurrency;
  const currencySymbol = targetCurrency === 'JPY' ? '¥' :
    targetCurrency === 'EUR' ? '€' : targetCurrency === 'GBP' ? '£' :
    targetCurrency === 'CAD' ? 'C$' : targetCurrency === 'AUD' ? 'A$' :
    targetCurrency === 'CHF' ? 'Fr' : '$';
  const convert = (v: number) => convertCurrency(v, loanCurrency, targetCurrency, rates);
  const [hover, setHover] = useState<{ x: number; y: number; snapshotIndex: number } | null>(null);
  const [activeSeries, setActiveSeries] = useState<'ltv' | 'price'>('ltv');

  const chart = useMemo(() => {
    if (!data.snapshots || data.snapshots.length === 0) return null;

    const snapshots = data.snapshots;
    const width = 800;
    const height = 384;
    const padding = { top: 20, right: 60, bottom: 40, left: 50 };
    const plotWidth = width - padding.left - padding.right;
    const plotHeight = height - padding.top - padding.bottom;

    const dates = snapshots.map((s) => new Date(s.snapshotDate).getTime());
    const minDate = Math.min(...dates);
    const maxDate = Math.max(...dates);
    const dateRange = maxDate - minDate || 1;

    const prices = snapshots.map((s) => convert(s.priceUsd));
    const minPrice = Math.min(...prices);
    const maxPrice = Math.max(...prices);
    const priceRange = maxPrice - minPrice || 1;

    const ltvs = snapshots.map((s) => s.ltv * 100);
    const minLtv = Math.min(...ltvs, warningLtv * 100, liquidationLtv * 100);
    const maxLtv = Math.max(...ltvs, warningLtv * 100, liquidationLtv * 100);
    const ltvRange = maxLtv - minLtv || 1;

    const x = (index: number) =>
      padding.left + ((dates[index] - minDate) / dateRange) * plotWidth;
    const yLtv = (ltv: number) =>
      padding.top + plotHeight - ((ltv - minLtv) / ltvRange) * plotHeight;
    const yPrice = (price: number) =>
      padding.top + plotHeight - ((price - minPrice) / priceRange) * plotHeight;

    const ltvPath = snapshots
      .map((s, i) => `${i === 0 ? 'M' : 'L'} ${x(i)} ${yLtv(s.ltv * 100)}`)
      .join(' ');

    const pricePath = snapshots
      .map((s, i) => `${i === 0 ? 'M' : 'L'} ${x(i)} ${yPrice(convert(s.priceUsd))}`)
      .join(' ');

    const warningY = yLtv(warningLtv * 100);
    const liquidationY = yLtv(liquidationLtv * 100);

    const dateLabels = [0, Math.floor(snapshots.length / 2), snapshots.length - 1].map((i) => ({
      x: x(i),
      label: new Date(snapshots[i].snapshotDate).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
      }),
    }));

    const ltvTicks = 5;
    const ltvStep = ltvRange / (ltvTicks - 1);
    const ltvLabels = Array.from({ length: ltvTicks }, (_, i) => ({
      y: yLtv(minLtv + ltvStep * i),
      label: `${(minLtv + ltvStep * i).toFixed(1)}%`,
    }));

    const priceTicks = 5;
    const priceStep = priceRange / (priceTicks - 1);
    const priceLabels = Array.from({ length: priceTicks }, (_, i) => ({
      y: yPrice(minPrice + priceStep * i),
      label: `${currencySymbol}${Math.round(minPrice + priceStep * i).toLocaleString()}`,
    }));

    return {
      width,
      height,
      ltvPath,
      pricePath,
      warningY,
      liquidationY,
      padding,
      dateLabels,
      ltvLabels,
      priceLabels,
      snapshots,
      x,
      yLtv,
      yPrice,
      minLtv,
      maxLtv,
      minPrice,
      maxPrice,
    };
  }, [data, warningLtv, liquidationLtv, loanCurrency, targetCurrency, rates]);

  if (!chart) {
    return <Spinner />;
  }

  const handleMouseMove = (e: React.MouseEvent<SVGSVGElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const plotX = mouseX - chart.padding.left;
    const ratio = Math.max(0, Math.min(1, plotX / (chart.width - chart.padding.left - chart.padding.right)));
    const index = Math.round(ratio * (chart.snapshots.length - 1));
    const snapshot = chart.snapshots[index];
    if (!snapshot) return;

    const plotY =
      activeSeries === 'ltv'
        ? chart.yLtv(snapshot.ltv * 100)
        : chart.yPrice(snapshot.priceUsd);

    setHover({ x: chart.x(index), y: plotY, snapshotIndex: index });
  };

  const handleMouseLeave = () => setHover(null);

  const hoveredSnapshot = hover ? chart.snapshots[hover.snapshotIndex] : null;

  return (
    <div className="w-full">
      <div className="mb-3 flex items-center gap-4 text-sm">
        <button
          onClick={() => setActiveSeries('ltv')}
          className={`flex items-center gap-2 rounded-full px-3 py-1 transition ${
            activeSeries === 'ltv'
              ? 'bg-[var(--color-coffer-orange)] text-black'
              : 'text-[var(--color-coffer-muted)] hover:text-white'
          }`}
        >
          <span className="h-2 w-2 rounded-full bg-orange-500" />
          LTV %
        </button>
        <button
          onClick={() => setActiveSeries('price')}
          className={`flex items-center gap-2 rounded-full px-3 py-1 transition ${
            activeSeries === 'price'
              ? 'bg-blue-500 text-white'
              : 'text-[var(--color-coffer-muted)] hover:text-white'
          }`}
        >
          <span className="h-2 w-2 rounded-full bg-blue-500" />
          BTC Price
        </button>
      </div>

      <div className="relative w-full overflow-hidden">
        <svg
          viewBox={`0 0 ${chart.width} ${chart.height}`}
          className="h-96 w-full"
          onMouseMove={handleMouseMove}
          onMouseLeave={handleMouseLeave}
        >
          {/* Grid lines */}
          {Array.from({ length: 5 }, (_, i) => {
            const y = chart.padding.top + (chart.height - chart.padding.top - chart.padding.bottom) * (i / 4);
            return (
              <line
                key={i}
                x1={chart.padding.left}
                y1={y}
                x2={chart.width - chart.padding.right}
                y2={y}
                stroke="rgba(255, 255, 255, 0.1)"
                strokeWidth={1}
              />
            );
          })}

          {/* LTV line */}
          {activeSeries === 'ltv' && (
            <path d={chart.ltvPath} fill="none" stroke="rgb(249, 115, 22)" strokeWidth={2} />
          )}

          {/* Price line */}
          {activeSeries === 'price' && (
            <path d={chart.pricePath} fill="none" stroke="rgb(59, 130, 246)" strokeWidth={2} />
          )}

          {/* Warning threshold line */}
          {activeSeries === 'ltv' && (
            <line
              x1={chart.padding.left}
              y1={chart.warningY}
              x2={chart.width - chart.padding.right}
              y2={chart.warningY}
              stroke="rgb(250, 204, 21)"
              strokeWidth={1}
              strokeDasharray="4,4"
            />
          )}

          {/* Liquidation threshold line */}
          {activeSeries === 'ltv' && (
            <line
              x1={chart.padding.left}
              y1={chart.liquidationY}
              x2={chart.width - chart.padding.right}
              y2={chart.liquidationY}
              stroke="rgb(248, 113, 113)"
              strokeWidth={1}
              strokeDasharray="4,4"
            />
          )}

          {/* Axes */}
          <line
            x1={chart.padding.left}
            y1={chart.height - chart.padding.bottom}
            x2={chart.width - chart.padding.right}
            y2={chart.height - chart.padding.bottom}
            stroke="rgba(255, 255, 255, 0.3)"
            strokeWidth={1}
          />
          <line
            x1={chart.padding.left}
            y1={chart.padding.top}
            x2={chart.padding.left}
            y2={chart.height - chart.padding.bottom}
            stroke="rgba(255, 255, 255, 0.3)"
            strokeWidth={1}
          />
          <line
            x1={chart.width - chart.padding.right}
            y1={chart.padding.top}
            x2={chart.width - chart.padding.right}
            y2={chart.height - chart.padding.bottom}
            stroke="rgba(255, 255, 255, 0.3)"
            strokeWidth={1}
          />

          {/* Date labels */}
          {chart.dateLabels.map((label, i) => (
            <text
              key={i}
              x={label.x}
              y={chart.height - 10}
              textAnchor="middle"
              fill="var(--color-coffer-muted)"
              fontSize={10}
            >
              {label.label}
            </text>
          ))}

          {/* Left axis labels (LTV) */}
          {activeSeries === 'ltv' &&
            chart.ltvLabels.map((label, i) => (
              <text
                key={`ltv-${i}`}
                x={chart.padding.left - 8}
                y={label.y + 3}
                textAnchor="end"
                fill="var(--color-coffer-muted)"
                fontSize={10}
              >
                {label.label}
              </text>
            ))}

          {/* Right axis labels (Price) */}
          {activeSeries === 'price' &&
            chart.priceLabels.map((label, i) => (
              <text
                key={`price-${i}`}
                x={chart.width - chart.padding.right + 8}
                y={label.y + 3}
                textAnchor="start"
                fill="var(--color-coffer-muted)"
                fontSize={10}
              >
                {label.label}
              </text>
            ))}

          {/* Hover indicator */}
          {hover && hoveredSnapshot && (
            <>
              <line
                x1={hover.x}
                y1={chart.padding.top}
                x2={hover.x}
                y2={chart.height - chart.padding.bottom}
                stroke="rgba(255, 255, 255, 0.3)"
                strokeWidth={1}
                strokeDasharray="2,2"
              />
              <circle cx={hover.x} cy={hover.y} r={4} fill="var(--color-coffer-orange)" />
            </>
          )}
        </svg>

        {/* Tooltip */}
        {hover && hoveredSnapshot && (
          <div
            className="pointer-events-none absolute rounded border border-[var(--color-coffer-border)] bg-black/80 px-3 py-2 text-xs text-white"
            style={{
              left: Math.min(hover.x + 10, chart.width - 150),
              top: Math.max(hover.y - 60, 10),
            }}
          >
            <div className="font-medium">
              {new Date(hoveredSnapshot.snapshotDate).toLocaleDateString('en-US')}
            </div>
            <div className="text-orange-400">LTV: {(hoveredSnapshot.ltv * 100).toFixed(2)}%</div>
            <div className="text-blue-400">Price: {currencySymbol}{Math.round(convert(hoveredSnapshot.priceUsd)).toLocaleString()}</div>
            <div className="text-emerald-400">Value: {currencySymbol}{Math.round(convert(hoveredSnapshot.collateralValue)).toLocaleString()}</div>
          </div>
        )}
      </div>
    </div>
  );
}
