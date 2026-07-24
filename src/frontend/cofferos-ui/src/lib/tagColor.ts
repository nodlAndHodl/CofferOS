const palette = [
  'bg-rose-500/15 text-rose-400 border border-rose-500/30',
  'bg-amber-500/15 text-amber-400 border border-amber-500/30',
  'bg-sky-500/15 text-sky-400 border border-sky-500/30',
  'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30',
  'bg-purple-500/15 text-purple-400 border border-purple-500/30',
  'bg-indigo-500/15 text-indigo-400 border border-indigo-500/30',
  'bg-fuchsia-500/15 text-fuchsia-400 border border-fuchsia-500/30',
];

function hashString(value: string) {
  let hash = 0;
  for (let i = 0; i < value.length; i += 1) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

export function getTagColorClass(tag: string): string {
  if (!tag) return palette[0];
  return palette[hashString(tag) % palette.length];
}
