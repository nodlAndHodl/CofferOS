import { useEffect, useState } from 'react';
import { api } from '../api/client';
import { useUserSettings } from '../contexts/UserSettingsContext';
import { useNotifications } from './useWalletNotifications';

export interface LiveBitcoinPrice {
  priceUsd: number | null;
  exchangeRates: Record<string, number>;
  isLive: boolean;
  lastUpdated: string | null;
}

export function useBitcoinPrice(): LiveBitcoinPrice {
  const { settings } = useUserSettings();
  const [priceUsd, setPriceUsd] = useState<number | null>(null);
  const [exchangeRates, setExchangeRates] = useState<Record<string, number>>({});
  const [isLive, setIsLive] = useState(false);
  const [lastUpdated, setLastUpdated] = useState<string | null>(null);

  useEffect(() => {
    api.getBitcoinPrice()
      .then((info) => {
        if (info.priceUsd != null) setPriceUsd(info.priceUsd);
        if (info.exchangeRates) setExchangeRates(info.exchangeRates);
      })
      .catch(() => {});
  }, []);

  useNotifications({
    onEvent: (notification) => {
      if (notification.eventType !== 'bitcoin_price_updated') return;
      if (!settings.enableLivePriceUpdates) return;

      const data = notification.data as {
        priceUsd?: number;
        exchangeRates?: Record<string, number>;
        timestamp?: string;
      };

      if (data.priceUsd != null) {
        setPriceUsd(data.priceUsd);
        setIsLive(true);
        setLastUpdated(data.timestamp ?? new Date().toISOString());
      }
      if (data.exchangeRates) {
        setExchangeRates(data.exchangeRates);
      }
    },
  });

  return { priceUsd, exchangeRates, isLive, lastUpdated };
}
