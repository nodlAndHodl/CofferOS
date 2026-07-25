import type {
  BtcPriceInfo,
  CreateLoanRequest,
  CreateNoteRequest,
  CreateTimelineEventRequest,
  Dashboard,
  ElectrumStatus,
  ImportWalletRequest,
  LoanDetail,
  LoanSummary,
  NodeStatus,
  Note,
  ObjectMetadata,
  RecentActivityPage,
  SetBtcPriceRequest,
  TimelineEvent,
  TreasurySummary,
  UpdateLoanBalanceRequest,
  UpdateLoanCollateralRequest,
  UpdateLoanRequest,
  UpdateMetadataRequest,
  UpdateNoteRequest,
  UpdateTimelineEventRequest,
  WalletDetail,
  WalletSummary,
  WalletTimeline,
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

  // Treasury / Loans
  getTreasurySummary: () => request<TreasurySummary>('/treasury/summary'),
  listLoans: () => request<LoanSummary[]>('/loans'),
  getLoan: (id: string) => request<LoanDetail>(`/loans/${id}`),
  createLoan: (payload: CreateLoanRequest) =>
    request<LoanSummary>('/loans', { method: 'POST', body: JSON.stringify(payload) }),
  updateLoan: (id: string, payload: UpdateLoanRequest) =>
    request<LoanDetail>(`/loans/${id}`, { method: 'PUT', body: JSON.stringify(payload) }),
  deleteLoan: (id: string) => request<void>(`/loans/${id}`, { method: 'DELETE' }),
  updateLoanBalance: (id: string, payload: UpdateLoanBalanceRequest) =>
    request<LoanDetail>(`/loans/${id}/balance`, { method: 'PUT', body: JSON.stringify(payload) }),
  updateLoanCollateral: (id: string, payload: UpdateLoanCollateralRequest) =>
    request<LoanDetail>(`/loans/${id}/collateral`, { method: 'PUT', body: JSON.stringify(payload) }),
  setBtcPrice: (payload: SetBtcPriceRequest) =>
    request<{ price: number }>('/price', { method: 'POST', body: JSON.stringify(payload) }),
  getBtcPrice: () => request<BtcPriceInfo>('/price'),
};
