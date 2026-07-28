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
  timestamp?: string | null;
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

export interface Tag {
  target: string;
  reference: string;
  value: string;
}

export interface Category {
  target: string;
  reference: string;
  name: string;
}

export interface MetadataEntry {
  target: string;
  reference: string;
  key: string;
  value: string;
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
  tags: Tag[];
  categories: Category[];
  metadata: MetadataEntry[];
  createdAt: string;
}

export interface ObjectMetadata {
  target: string;
  reference: string;
  label?: string | null;
  category?: string | null;
  tags: string[];
  metadata: Record<string, string>;
  notes: Note[];
}

export interface UpdateMetadataRequest {
  target: string;
  reference: string;
  label?: string | null;
  category?: string | null;
  tags?: string[] | null;
  metadata?: Record<string, string> | null;
}

export interface TimelineEntry {
  id?: string | null;
  type: string;
  occurredAt: string;
  title: string;
  description?: string | null;
  reference?: string | null;
  amountSats?: number | null;
  runningBalanceSats?: number | null;
  isUserEvent: boolean;
}

export interface WalletTimeline {
  walletId: string;
  walletName: string;
  currentBalance: Balance;
  entries: TimelineEntry[];
}

export interface TimelineEvent {
  id: string;
  type: string;
  occurredAt: string;
  title: string;
  description?: string | null;
  reference?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTimelineEventRequest {
  occurredAt: string;
  title: string;
  description?: string | null;
  reference?: string | null;
  type?: string | null;
}

export interface UpdateTimelineEventRequest {
  occurredAt: string;
  title: string;
  description?: string | null;
  reference?: string | null;
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

export interface RecentActivityItem {
  txId: string;
  netAmountSats: number;
  blockHeight?: number | null;
  timestamp?: string | null;
  walletName: string;
  label?: string | null;
  tags: string[];
}

export interface RecentActivityPage {
  skip: number;
  take: number;
  total: number;
  items: RecentActivityItem[];
}

export interface Dashboard {
  walletCount: number;
  totalBalance: Balance;
  wallets: WalletSummary[];
  recentActivity: RecentActivityPage;
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

// Treasury / Loans (Phase 1)
export interface CreateLoanRequest {
  name: string;
  lender?: string;
  principalAmount: number;
  currentBalance: number;
  interestRate: number;
  interestType: string;
  loanStartDate: string;
  loanTermMonths?: number;
  paymentFrequency: string;
  collateralAmountBtc: number;
  currentBtcPrice: number;
  warningLtv: number;
  liquidationLtv: number;
  notes?: string;
  interestPaymentSchedule?: string;
}

export interface UpdateLoanRequest {
  name: string;
  lender?: string;
  principalAmount: number;
  currentBalance: number;
  interestRate: number;
  interestType: string;
  loanStartDate: string;
  loanTermMonths?: number;
  paymentFrequency: string;
  collateralAmountBtc: number;
  currentBtcPrice: number;
  warningLtv: number;
  liquidationLtv: number;
  notes?: string;
  interestPaymentSchedule?: string;
}

export interface LoanSummary {
  id: string;
  name: string;
  lender?: string | null;
  status: string;
  principalAmount: number;
  currentBalance: number;
  interestRate: number;
  interestType: string;
  collateralAmountBtc: number;
  currentBtcPrice: number;
  currentCollateralValue: number;
  currentLtv: number;
  warningLtv: number;
  liquidationLtv: number;
  distanceToWarning: number;
  distanceToLiquidation: number;
  createdAt: string;
  updatedAt: string;
}

export interface LoanDetail {
  id: string;
  name: string;
  lender?: string | null;
  status: string;
  notes?: string | null;
  principalAmount: number;
  currentBalance: number;
  interestRate: number;
  interestType: string;
  loanStartDate: string;
  loanTermMonths?: number | null;
  paymentFrequency: string;
  interestPaymentSchedule: string;
  collateralAmountBtc: number;
  currentBtcPrice: number;
  currentCollateralValue: number;
  currentLtv: number;
  warningLtv: number;
  liquidationLtv: number;
  warningPrice: number;
  liquidationPrice: number;
  distanceToWarning: number;
  distanceToLiquidation: number;
  remainingCollateralBuffer: number;
  createdAt: string;
  updatedAt: string;
}

export interface TreasurySummary {
  activeLoanCount: number;
  totalLoanBalance: number;
  totalCollateralBtc: number;
  totalCollateralValue: number;
  averageLtv: number;
  highestRiskLoan?: LoanSummary | null;
  currentBtcPrice?: number | null;
  priceProvider: string;
}

export interface BtcPriceInfo {
  price: number | null;
  providerId: string;
  displayName: string;
  lastUpdated?: string | null;
  note?: string | null;
}

export interface LoanPriceSnapshot {
  snapshotDate: string;
  priceUsd: number;
  currentBalance: number;
  collateralValue: number;
  ltv: number;
}

export interface LoanHistoricalData {
  loanId: string;
  startDate: string;
  endDate: string;
  snapshots: LoanPriceSnapshot[];
}

export interface DashboardOverview {
  // Holdings
  totalBitcoin: number;
  availableBitcoin: number;
  collateralBitcoin: number;
  bitcoinPriceUsd: number;
  totalValueUsd: number;

  // Treasury
  activeLoanCount: number;
  outstandingLoanBalanceUsd: number;
  weightedAverageLtv: number;
  highestRiskLoan?: LoanSummary | null;

  // Wallet summary + activity (merged from old dashboard)
  walletCount: number;
  totalBalance: Balance;
  wallets: WalletSummary[];
  recentActivity: RecentActivityPage;

  // Metadata
  lastUpdatedUtc: string;
}

// Keep old name as alias for now if referenced elsewhere, but prefer DashboardOverview
export type TreasuryOverview = DashboardOverview;

// Holdings (first-class domain concept)
export type HoldingType = 'Wallet' | 'LoanCollateral' | 'Lightning' | 'Retirement' | 'Etf' | 'Mining' | 'Manual';

export interface HoldingsSummary {
  totalBitcoin: number;
  availableBitcoin: number;
  collateralBitcoin: number;
  totalValueUsd: number;
  breakdown: HoldingBreakdown[];
}

export interface HoldingBreakdown {
  category: string;
  bitcoinAmount: number;
  percentage: number;
  valueUsd: number;
  count: number;
}

export interface Holding {
  id: string;
  type: HoldingType;
  name: string;
  bitcoinAmount: number;
  availableBitcoin: number;
  lockedBitcoin: number;
  valueUsd: number;
  isReadOnly: boolean;
  institution?: string | null;
}
