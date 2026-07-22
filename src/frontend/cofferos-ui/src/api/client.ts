import type { CreateNoteRequest, Dashboard, ImportWalletRequest, Note, UpdateNoteRequest, WalletDetail, WalletSummary } from '../types';

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
};
