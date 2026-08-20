export const SUPPORTED_CURRENCIES = [
  { code: 'USD', label: 'US Dollar', symbol: '$' },
  { code: 'EUR', label: 'Euro', symbol: '€' },
  { code: 'GBP', label: 'British Pound', symbol: '£' },
  { code: 'CAD', label: 'Canadian Dollar', symbol: 'C$' },
  { code: 'AUD', label: 'Australian Dollar', symbol: 'A$' },
  { code: 'CHF', label: 'Swiss Franc', symbol: 'CHF' },
  { code: 'JPY', label: 'Japanese Yen', symbol: '¥' },
] as const;

/**
 * Converts a USD value to the target currency using exchange rates from CoinGecko.
 * rates keys are lowercase ISO codes (e.g. "usd", "eur").
 */
export function convertFromUsd(
  usdValue: number,
  currency: string,
  rates: Record<string, number>,
): number {
  return convertCurrency(usdValue, 'USD', currency, rates);
}

/**
 * Converts a value from one currency to another using CoinGecko BTC price ratios.
 * rates keys are lowercase ISO codes (e.g. "usd", "eur").
 */
export function convertCurrency(
  value: number,
  from: string,
  to: string,
  rates: Record<string, number>,
): number {
  const fromCode = (from ?? 'USD').toUpperCase();
  const toCode = (to ?? 'USD').toUpperCase();
  if (fromCode === toCode) return value;
  const fromRate = rates[fromCode.toLowerCase()];
  const toRate = rates[toCode.toLowerCase()];
  if (!fromRate || !toRate) return value;
  return value * (toRate / fromRate);
}

function toSafeCurrency(code: string | undefined | null): string {
  const upper = (code ?? 'USD').toUpperCase();
  return SUPPORTED_CURRENCIES.some((c) => c.code === upper) ? upper : 'USD';
}

/**
 * Formats a value in the user\'s preferred display currency, converting from the value\'s native currency.
 * Falls back gracefully if rates are not yet available.
 */
export function formatForDisplay(
  value: number,
  valueCurrency: string | undefined | null,
  displayCurrency: string | undefined | null,
  rates: Record<string, number>,
): string {
  const native = toSafeCurrency(valueCurrency);
  const target = toSafeCurrency(displayCurrency);
  const converted = native === target ? value : convertCurrency(value, native, target, rates);
  const isJpy = target === 'JPY';
  return converted.toLocaleString(undefined, {
    style: 'currency',
    currency: target,
    minimumFractionDigits: 0,
    maximumFractionDigits: isJpy ? 0 : 0,
  });
}

/**
 * Formats a value that is already in the specified currency.
 */
export function formatInCurrency(
  value: number,
  currency: string | undefined | null,
): string {
  const safe = toSafeCurrency(currency);
  const isJpy = safe === 'JPY';
  return value.toLocaleString(undefined, {
    style: 'currency',
    currency: safe,
    minimumFractionDigits: 0,
    maximumFractionDigits: isJpy ? 0 : 0,
  });
}

/**
 * Formats a BTC price in the user's preferred currency.
 * priceUsd is the current BTC/USD price.
 */
export function formatBtcPrice(
  priceUsd: number,
  currency: string,
  rates: Record<string, number>,
): string {
  return formatForDisplay(priceUsd, 'USD', currency, rates);
}

export function getCurrencySymbol(code: string): string {
  return SUPPORTED_CURRENCIES.find((c) => c.code === code)?.symbol ?? code;
}
