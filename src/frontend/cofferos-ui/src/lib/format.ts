export function formatBtc(sats: number): string {
  const btc = sats / 100_000_000;
  return `${btc.toLocaleString(undefined, { minimumFractionDigits: 8, maximumFractionDigits: 8 })} BTC`;
}

export function formatSats(sats: number): string {
  return `${sats.toLocaleString()} sats`;
}

export function shorten(value: string, head = 10, tail = 8): string {
  if (value.length <= head + tail + 1) return value;
  return `${value.slice(0, head)}…${value.slice(-tail)}`;
}

export function formatDate(iso?: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString();
}
