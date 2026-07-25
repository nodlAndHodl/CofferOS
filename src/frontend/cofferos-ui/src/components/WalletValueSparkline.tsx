import { useMemo, useState } from 'react';
import type { Transaction } from '../types';
import { formatBtc } from '../lib/format';

interface Point {
  block: number;
  balance: number;
}

interface Props {
  transactions: Transaction[];
  currentSats: number;
}

function compactBtc(sats: number): string {
  const btc = sats / 100_000_000;
  const abs = Math.abs(btc);
  if (abs === 0) return '0';
  if (abs >= 1) return btc.toFixed(4);
  if (abs >= 0.01) return btc.toFixed(6);
  return btc.toFixed(8);
}

export function WalletValueSparkline({ transactions, currentSats }: Props) {
  const { points, minBlock, maxBlock, minBal, maxBal, high } = useMemo(() => {
    const confirmed = transactions
      .filter((t) => t.blockHeight != null)
      .map((t) => ({ block: t.blockHeight as number, net: t.netAmountSats }))
      .sort((a, b) => a.block - b.block || 0);

    // collapse per-block to final running balance at that block
    const byBlock = new Map<number, number>();
    let running = 0;
    for (const p of confirmed) {
      running += p.net;
      byBlock.set(p.block, running);
    }

    const pts: Point[] = Array.from(byBlock.entries())
      .map(([block, balance]) => ({ block, balance }))
      .sort((a, b) => a.block - b.block);

    if (pts.length === 0) {
      return { points: [] as Point[], minBlock: 0, maxBlock: 0, minBal: 0, maxBal: 0, high: null as Point | null };
    }

    let mnB = pts[0].block;
    let mxB = pts[0].block;
    let mnBal = pts[0].balance;
    let mxBal = pts[0].balance;
    let highPt: Point = pts[0];

    for (const p of pts) {
      if (p.block < mnB) mnB = p.block;
      if (p.block > mxB) mxB = p.block;
      if (p.balance < mnBal) mnBal = p.balance;
      if (p.balance > mxBal) {
        mxBal = p.balance;
        highPt = p;
      }
    }

    // If current is higher than any historical running (e.g. unconfirmed), treat current as the visual high for label purposes
    const currentIsHigh = currentSats > mxBal;
    const highPoint: Point = currentIsHigh
      ? { block: mxB, balance: currentSats }
      : highPt;

    return {
      points: pts,
      minBlock: mnB,
      maxBlock: mxB,
      minBal: mnBal,
      maxBal: mxBal,
      high: highPoint,
    };
  }, [transactions, currentSats]);

  const [hovered, setHovered] = useState<Point | null>(null);

  if (points.length === 0) return null;

  const width = 260;
  const height = 42;
  const pad = 4;

  const rangeX = Math.max(1, maxBlock - minBlock);
  const rangeY = Math.max(1, maxBal - minBal);

  function toX(block: number) {
    const t = (block - minBlock) / rangeX;
    return pad + t * (width - pad * 2);
  }

  function toY(balance: number) {
    // invert: higher balance -> higher on chart (smaller svg y)
    const t = (balance - minBal) / rangeY;
    return pad + (1 - t) * (height - pad * 2);
  }

  // Step-function path: horizontal at previous balance until the tx block, then vertical change at that block.
  // This ensures balance drops/rises appear as instant cliffs exactly at the transaction's block height.
  let pathD = '';
  if (points.length > 0) {
    const x0 = toX(points[0].block);
    const y0 = toY(points[0].balance);
    pathD = `M ${x0.toFixed(2)} ${y0.toFixed(2)}`;
    for (let i = 1; i < points.length; i++) {
      const curr = points[i];
      const xCurr = toX(curr.block);
      const yCurr = toY(curr.balance);
      // horizontal from previous point's x at old y, up to current block x (still at old balance)
      pathD += ` H ${xCurr.toFixed(2)}`;
      // vertical at the tx block: instant balance change
      pathD += ` V ${yCurr.toFixed(2)}`;
    }
  }

  const start = points[0];
  const endBlock = maxBlock;
  const endBalanceForLine = points[points.length - 1].balance;

  // high point for marker (use the computed high's block/balance for position; if currentIsHigh we place at end x)
  const highPoint = high!; // guarded by points.length > 0
  const highX = toX(highPoint.block);
  const highY = toY(highPoint.balance);

  const showHighMarker =
    highPoint.balance !== endBalanceForLine || highPoint.block !== endBlock || highPoint.balance !== currentSats;

  function handleMove(e: React.MouseEvent<SVGSVGElement>) {
    const rect = (e.currentTarget as SVGSVGElement).getBoundingClientRect();
    const frac = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
    const targetBlock = minBlock + frac * rangeX;

    // find nearest by block
    let nearest = points[0];
    let best = Math.abs(points[0].block - targetBlock);
    for (const p of points) {
      const d = Math.abs(p.block - targetBlock);
      if (d < best) {
        best = d;
        nearest = p;
      }
    }
    setHovered(nearest);
  }

  function handleLeave() {
    setHovered(null);
  }

  const hoveredX = hovered ? toX(hovered.block) : 0;
  const hoveredY = hovered ? toY(hovered.balance) : 0;

  const startLabel = `${start.block} · ${compactBtc(start.balance)}`;
  const endLabel = `${endBlock} · ${compactBtc(currentSats)}`;
  const highLabel = highPoint.block !== endBlock || highPoint.balance !== currentSats
    ? `${highPoint.block} · ${compactBtc(highPoint.balance)}`
    : null;

  return (
    <div className="select-none">
      <svg
        width={width}
        height={height}
        viewBox={`0 0 ${width} ${height}`}
        onMouseMove={handleMove}
        onMouseLeave={handleLeave}
        className="cursor-crosshair overflow-visible"
      >
        {/* subtle baseline */}
        <line
          x1={pad}
          y1={height - pad}
          x2={width - pad}
          y2={height - pad}
          stroke="currentColor"
          strokeOpacity={0.15}
          strokeWidth={1}
        />

        {/* main spark line */}
        <path
          d={pathD}
          fill="none"
          stroke="currentColor"
          strokeWidth={1.75}
          strokeLinejoin="round"
          strokeLinecap="round"
          className="text-[var(--color-coffer-orange)]"
        />

        {/* start dot */}
        <circle
          cx={toX(start.block)}
          cy={toY(start.balance)}
          r={2}
          className="fill-[var(--color-coffer-orange)]"
        />

        {/* high marker (if distinct) */}
        {showHighMarker && (
          <g>
            <circle
              cx={highX}
              cy={highY}
              r={2.5}
              className="fill-amber-400"
              stroke="white"
              strokeWidth={0.5}
              strokeOpacity={0.8}
            />
            <line
              x1={highX}
              y1={highY - 3}
              x2={highX}
              y2={highY + 3}
              stroke="#fbbf24"
              strokeWidth={1}
            />
          </g>
        )}

        {/* end dot (current historical point) — omit if high marker occupies same x for current */}
        {!(showHighMarker && highPoint.block === endBlock) && (
          <circle
            cx={toX(endBlock)}
            cy={toY(endBalanceForLine)}
            r={2}
            className="fill-[var(--color-coffer-orange)]"
          />
        )}

        {/* hover indicator */}
        {hovered && (
          <g>
            <line
              x1={hoveredX}
              y1={pad}
              x2={hoveredX}
              y2={height - pad}
              stroke="currentColor"
              strokeOpacity={0.5}
              strokeWidth={1}
              strokeDasharray="2 2"
            />
            <circle
              cx={hoveredX}
              cy={hoveredY}
              r={3}
              className="fill-white stroke-[var(--color-coffer-orange)]"
              strokeWidth={1.5}
            />
          </g>
        )}
      </svg>

      {/* labels */}
      <div className="mt-0.5 text-[10px] text-[var(--color-coffer-muted)]">
        {!hovered ? (
          <div className="flex items-center justify-between gap-2">
            <span title="Opening">{startLabel}</span>
            {highLabel && showHighMarker && (
              <span className="text-amber-400" title="High">
                {highLabel}
              </span>
            )}
            <span title="Current">{endLabel}</span>
          </div>
        ) : (
          <div className="text-[var(--color-coffer-orange)]">
            {hovered.block} · {formatBtc(hovered.balance)}
          </div>
        )}
      </div>
    </div>
  );
}
