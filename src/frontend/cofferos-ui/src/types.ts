export interface Balance {
  confirmedSats: number;
  unconfirmedSats: number;
  totalSats: number;
  totalBtc: number;
}

export interface WalletSummary {
  id: string;
  name: string;
  description?: string | null;
  network: string;
  watchOnly: boolean;
  descriptorCount: number;
  transactionCount: number;
  balance: Balance;
  createdAt: string;
}

export interface Descriptor {
  id: string;
  source: string;
  scriptType: string;
  raw: string;
  masterFingerprint?: string | null;
  derivationPath?: string | null;
  checksum?: string | null;
  addressCount: number;
}

export interface Address {
  id: string;
  derivationIndex: number;
  isChange: boolean;
  value: string;
  isUsed: boolean;
  useCount: number;
  firstTxId?: string | null;
  lastTxId?: string | null;
  currentSats: number;
}

export interface Transaction {
  txId: string;
  netAmountSats: number;
  feeSats: number;
  direction: string;
  confirmations: number;
  blockHeight?: number | null;
  timestamp?: string | null;
}

export interface Utxo {
  txId: string;
  vout: number;
  valueSats: number;
  address?: string | null;
  confirmations: number;
  isSpent: boolean;
}

export interface Label {
  target: string;
  reference: string;
  text: string;
}

export interface Note {
  id: string;
  target: string;
  reference: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface WalletDetail {
  id: string;
  name: string;
  description?: string | null;
  network: string;
  watchOnly: boolean;
  balance: Balance;
  descriptors: Descriptor[];
  addresses: Address[];
  transactions: Transaction[];
  utxos: Utxo[];
  labels: Label[];
  notes: Note[];
  createdAt: string;
}

export interface NodeStatus {
  connected: boolean;
  providerId: string;
  chain?: string | null;
  blocks?: number | null;
  verificationProgress?: number | null;
  error?: string | null;
}

export interface ElectrumStatus {
  connected: boolean;
  providerId: string;
  host: string;
  port: number;
  socks5Proxy?: string | null;
  blockHeight?: number | null;
  error?: string | null;
}

export interface Dashboard {
  walletCount: number;
  totalBalance: Balance;
  wallets: WalletSummary[];
  recentActivity: Transaction[];
}

export interface ImportWalletRequest {
  name: string;
  description?: string;
  descriptor: string;
  network: string;
  initialAddressCount: number;
}

export interface CreateNoteRequest {
  target: string;
  reference: string;
  content: string;
}

export interface UpdateNoteRequest {
  content: string;
}
