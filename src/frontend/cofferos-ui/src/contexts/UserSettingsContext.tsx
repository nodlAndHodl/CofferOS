import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { api } from '../api/client';
import type { UserSettings } from '../types';

const DEFAULT_SETTINGS: UserSettings = {
  currency: 'USD',
  enableLivePriceUpdates: true,
  enablePriceHistory: true,
  mempoolExplorerUrl: null,
};

interface UserSettingsContextValue {
  settings: UserSettings;
  updateSettings: (patch: Partial<UserSettings>) => Promise<void>;
  loading: boolean;
}

const UserSettingsContext = createContext<UserSettingsContextValue>({
  settings: DEFAULT_SETTINGS,
  updateSettings: async () => {},
  loading: true,
});

export function UserSettingsProvider({ children }: { children: React.ReactNode }) {
  const [settings, setSettings] = useState<UserSettings>(DEFAULT_SETTINGS);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.getUserSettings()
      .then(setSettings)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const updateSettings = useCallback(async (patch: Partial<UserSettings>) => {
    const merged = { ...settings, ...patch };
    setSettings(merged);
    try {
      const saved = await api.updateUserSettings(merged);
      setSettings(saved);
    } catch {
      setSettings(settings);
      throw new Error('Failed to save settings');
    }
  }, [settings]);

  return (
    <UserSettingsContext.Provider value={{ settings, updateSettings, loading }}>
      {children}
    </UserSettingsContext.Provider>
  );
}

export function useUserSettings() {
  return useContext(UserSettingsContext);
}
