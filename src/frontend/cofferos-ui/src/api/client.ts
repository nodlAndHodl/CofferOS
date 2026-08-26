import type {
  AddCollateralRequest,
  BtcPriceInfo,
  CollateralAdjustmentResponse,
  CostBasisEntryInput,
  CreateLoanRequest,
  CreateNoteRequest,
  CreateRetirementAccountRequest,
  CreateTimelineEventRequest,
  Dashboard,
  DashboardOverview,
  ElectrumStatus,
  Holding,
  HoldingsSummary,
  ImportWalletRequest,
  LoanCollateralTransaction,
  LoanDetail,
  LoanHistoricalData,
  LoanSummary,
  NodeStatus,
  Note,
  ObjectMetadata,
  RecentActivityPage,
  RemoveCollateralRequest,
  RetirementAccount,
  TimelineEvent,
  TreasurySummary,
  UpdateLoanRequest,
  UpdateMetadataRequest,
  UpdateNoteRequest,
  UpdateRetirementAccountRequest,
  UpdateTimelineEventRequest,
  WalletDetail,
  WalletSummary,
  WalletTimeline,
  UserSettings,
  BitcoinPriceInfo,
} from '../types';

const BASE = import.meta.env.VITE_API_BASE ?? '/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });

  if (!response.ok) {
    let message = `Request failed (${response.status})`;
    try {
      const body = await response.json();
      if (body?.error) message = body.error;
    } catch {
      /* ignore non-JSON error bodies */
    }
    throw new Error(message);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  getDashboard: () => request<Dashboard>('/dashboard'),
  getRecentActivity: (page = 1, pageSize = 10) =>
    request<RecentActivityPage>(`/dashboard/activity?page=${page}&pageSize=${pageSize}`),
  listWallets: () => request<WalletSummary[]>('/wallets'),
  getWallet: (id: string) => request<WalletDetail>(`/wallets/${id}`),
  deleteWallet: (id: string) => request<void>(`/wallets/${id}`, { method: 'DELETE' }),
  importWallet: (payload: ImportWalletRequest) =>
    request<WalletSummary>('/wallets', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  createNote: (walletId: string, payload: CreateNoteRequest) =>
    request<Note>(`/wallets/${walletId}/notes`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  updateNote: (noteId: string, payload: UpdateNoteRequest) =>
    request<Note>(`/notes/${noteId}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
  deleteNote: (noteId: string) => request<void>(`/notes/${noteId}`, { method: 'DELETE' }),

  getObjectMetadata: (walletId: string, target: string, reference: string) =>
    request<ObjectMetadata>(`/wallets/${walletId}/objects/${encodeURIComponent(target)}/${encodeURIComponent(reference)}/metadata`),
  updateObjectMetadata: (walletId: string, target: string, reference: string, payload: UpdateMetadataRequest) =>
    request<void>(`/wallets/${walletId}/objects/${encodeURIComponent(target)}/${encodeURIComponent(reference)}/metadata`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),

  getWalletTimeline: (walletId: string) => request<WalletTimeline>(`/wallets/${walletId}/timeline`),
  createTimelineEvent: (walletId: string, payload: CreateTimelineEventRequest) =>
    request<TimelineEvent>(`/wallets/${walletId}/timeline`, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  updateTimelineEvent: (id: string, payload: UpdateTimelineEventRequest) =>
    request<TimelineEvent>(`/timeline-events/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
  deleteTimelineEvent: (id: string) => request<void>(`/timeline-events/${id}`, { method: 'DELETE' }),

  getNodeStatus: () => request<NodeStatus>('/node/status'),
  getElectrumStatus: () => request<ElectrumStatus>('/electrum/status'),

  // Merged dashboard overview (holdings + treasury + wallet summary + recent activity)
  // Node and Electrum status are fetched separately (they can be slow/blocking).
  getDashboardOverview: () => request<DashboardOverview>('/dashboard/overview'),
  getTreasurySummary: () => request<TreasurySummary>('/treasury/summary'),
  listLoans: () => request<LoanSummary[]>('/loans'),
  getLoan: (id: string) => request<LoanDetail>(`/loans/${id}`),
  createLoan: (payload: CreateLoanRequest) =>
    request<LoanSummary>('/loans', { method: 'POST', body: JSON.stringify(payload) }),
  updateLoan: (id: string, payload: UpdateLoanRequest) =>
    request<LoanDetail>(`/loans/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  deleteLoan: (id: string) => request<void>(`/loans/${id}`, { method: 'DELETE' }),
  getBtcPrice: () => request<BtcPriceInfo>('/price'),
  getLoanHistoricalData: (id: string) => request<LoanHistoricalData>(`/loans/${id}/historical`),
  getLoanCollateralTransactions: (id: string) => request<LoanCollateralTransaction[]>(`/loans/${id}/collateral/transactions`),
  addLoanCollateral: (id: string, payload: AddCollateralRequest) =>
    request<CollateralAdjustmentResponse>(`/loans/${id}/collateral/add`, { method: 'POST', body: JSON.stringify(payload) }),
  removeLoanCollateral: (id: string, payload: RemoveCollateralRequest) =>
    request<CollateralAdjustmentResponse>(`/loans/${id}/collateral/remove`, { method: 'POST', body: JSON.stringify(payload) }),

  // Cost basis
  setCostBasis: (target: string, reference: string, amount: number) =>
    request<void>(`/cost-basis/${encodeURIComponent(target)}/${encodeURIComponent(reference)}`, {
      method: 'PUT',
      body: JSON.stringify({ amount }),
    }),
  clearCostBasis: (target: string, reference: string) =>
    request<void>(`/cost-basis/${encodeURIComponent(target)}/${encodeURIComponent(reference)}`, { method: 'DELETE' }),

  // Holdings
  getHoldingsSummary: () => request<HoldingsSummary>('/holdings/summary'),
  listHoldings: () => request<Holding[]>('/holdings/'),

  // Retirement Accounts
  listRetirementAccounts: () => request<RetirementAccount[]>('/retirement-accounts/'),
  getRetirementAccount: (id: string) => request<RetirementAccount>(`/retirement-accounts/${id}`),
  createRetirementAccount: (payload: CreateRetirementAccountRequest) =>
    request<RetirementAccount>('/retirement-accounts/', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  updateRetirementAccount: (id: string, payload: UpdateRetirementAccountRequest) =>
    request<RetirementAccount>(`/retirement-accounts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
  deleteRetirementAccount: (id: string) => request<void>(`/retirement-accounts/${id}`, { method: 'DELETE' }),
  addCostBasisEntry: (id: string, entry: CostBasisEntryInput) =>
    request<RetirementAccount>(`/retirement-accounts/${id}/cost-basis`, {
      method: 'POST',
      body: JSON.stringify(entry),
    }),
  removeCostBasisEntry: (id: string, entryId: string) =>
    request<RetirementAccount>(`/retirement-accounts/${id}/cost-basis/${entryId}`, { method: 'DELETE' }),

  getUserSettings: () => request<UserSettings>('/settings/'),
  updateUserSettings: (payload: UserSettings) =>
    request<UserSettings>('/settings/', { method: 'PUT', body: JSON.stringify(payload) }),
  getBitcoinPrice: () => request<BitcoinPriceInfo>('/settings/bitcoin-price'),
};
