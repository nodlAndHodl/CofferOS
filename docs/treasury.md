# Treasury & Holdings Architecture

CofferOS distinguishes between **holdings** (assets owned), **collateral** (assets pledged), and **liabilities** (loans). This document defines the domain model and aggregation services that power the treasury dashboard.

## Domain Concepts

### Holding
A Bitcoin position owned by the user. Holdings are aggregated from multiple sources:
- **Self-custody wallets** (current)
- **Multisig wallets** (future)
- **Lightning balances** (future)
- **Roth IRA** (future)
- **Traditional IRA** (future)
- **ETF positions** (future)
- **Brokerage accounts** (future)
- **Mining balances** (future)

A holding is not tied to a specific wallet. It represents ownership regardless of custody mechanism.

### Collateral
Bitcoin that is pledged against a loan. Collateral is still owned Bitcoin; it is simply unavailable for other uses. Collateral is calculated as the sum of all active loan collateral amounts.

### Loan
A liability secured by Bitcoin collateral. Loans have:
- **Principal amount** — the original borrowed amount
- **Current balance** — principal + accrued interest
- **Collateral amount** — BTC pledged
- **Interest rate** — annual percentage
- **LTV (Loan-to-Value)** — balance / collateral value
- **Warning LTV** — threshold at which to alert
- **Liquidation LTV** — threshold at which collateral may be seized
- **Status** — Active, Repaid, Defaulted

### Treasury
The aggregate view of all holdings, collateral, and loans. The treasury answers:
1. How much Bitcoin do I own?
2. How much of it is pledged as collateral?
3. How healthy is my treasury?
4. Is my infrastructure healthy?

## Service Architecture

The treasury aggregation layer is organized as a set of focused services, each responsible for a single domain concern:

```
DashboardController
        │
        ▼
IDashboardQueryService (orchestrator)
        │
        ├── IHoldingsService
        ├── ITreasuryService (existing, loan CRUD)
        ├── ILoanAnalyticsService
        ├── IMarketDataService (IBitcoinPriceProvider)
        └── IInfrastructureService
```

### IHoldingsService

Aggregates Bitcoin holdings from all sources.

```csharp
public interface IHoldingsService
{
    Task<decimal> GetTotalBitcoinAsync();
    Task<decimal> GetAvailableBitcoinAsync();
    Task<decimal> GetCollateralBitcoinAsync();
    Task<HoldingsBreakdown> GetBreakdownAsync();
}
```

**Responsibilities:**
- Sum wallet balances (confirmed + unconfirmed)
- Calculate collateral from active loans
- Calculate available Bitcoin (total - collateral)
- Return breakdown by source type

**Current implementation:**
- Queries all wallets and sums their balances
- Queries all active loans and sums their collateral
- Calculates available as total - collateral

**Future extensibility:**
- Add new holding sources by implementing a `IHoldingSource` interface
- Register sources in DI
- `HoldingsService` aggregates all sources without modification

### ITreasuryService

Existing service for loan CRUD and treasury summaries. Remains unchanged; used by `ILoanAnalyticsService` for calculations.

### ILoanAnalyticsService

Analyzes loan portfolio for risk metrics.

```csharp
public interface ILoanAnalyticsService
{
    Task<LoanRiskAnalysis?> GetHighestRiskLoanAsync();
    Task<LoanRiskAnalysis?> GetNearestWarningThresholdAsync();
    Task<IReadOnlyList<LoanLiquidationEstimate>> GetLiquidationEstimatesAsync();
    Task<CollateralUtilization> GetCollateralUtilizationAsync();
    Task<int> CalculatePortfolioRiskScoreAsync();
}
```

**Responsibilities:**
- Identify highest-risk loan (highest LTV)
- Identify loan nearest to warning threshold
- Calculate liquidation prices for all loans
- Compute collateral utilization metrics
- Calculate portfolio risk score (0-100)

### IInfrastructureService

Provides infrastructure health status.

```csharp
public interface IInfrastructureService
{
    Task<InfrastructureStatus> GetStatusAsync();
}
```

**Responsibilities:**
- Count wallets
- Check Bitcoin node connectivity and block height
- Check Electrum connectivity and block height
- Return unified status DTO

**Future extensibility:**
- Add Lightning node status
- Add Mempool API status
- Add additional node provider status

### IDashboardQueryService

Orchestrates all services to assemble the complete dashboard overview.

```csharp
public interface IDashboardQueryService
{
    Task<TreasuryOverviewDto> GetOverviewAsync();
}
```

**Responsibilities:**
- Call `IHoldingsService` for Bitcoin holdings
- Call loan repository for active loan count
- Call `ILoanAnalyticsService` for highest-risk loan
- Call `IBitcoinPriceProvider` for current BTC price
- Call `IInfrastructureService` for infrastructure status
- Assemble `TreasuryOverviewDto` with all data
- Return single DTO to frontend

## Data Flow

### Frontend Request
```
GET /api/dashboard/overview
```

### Backend Processing
```
DashboardQueryService.GetOverviewAsync()
  ├─ HoldingsService.GetBreakdownAsync()
  │   ├─ WalletRepository.GetAllAsync()
  │   └─ LoanRepository.GetActiveAsync()
  │
  ├─ LoanRepository.GetActiveAsync()
  │   └─ (for each loan)
  │       ├─ LoanPaymentRepository.GetByLoanAsync()
  │       └─ LoanAccrualService.CalculateAsync()
  │
  ├─ LoanAnalyticsService.GetHighestRiskLoanAsync()
  │   └─ (same as above)
  │
  ├─ BitcoinPriceProvider.GetCurrentPriceAsync()
  │
  └─ InfrastructureService.GetStatusAsync()
      ├─ WalletRepository.GetAllAsync()
      ├─ BitcoinNodeProvider.TestConnectionAsync()
      ├─ BitcoinNodeProvider.GetBlockchainInfoAsync()
      └─ ElectrumServerProvider.GetStatusAsync()
```

### Response
```json
{
  "totalBitcoin": 5.182,
  "availableBitcoin": 0.479,
  "collateralBitcoin": 4.703,
  "bitcoinPriceUsd": 65000,
  "totalValueUsd": 336670,
  "activeLoanCount": 2,
  "outstandingLoanBalanceUsd": 150000,
  "weightedAverageLtv": 0.45,
  "highestRiskLoan": { ... },
  "infrastructure": {
    "walletCount": 3,
    "bitcoinNodeConnected": true,
    "bitcoinNodeBlockHeight": 850000,
    "electrumConnected": true,
    "electrumBlockHeight": 850000
  },
  "lastUpdatedUtc": "2026-07-27T12:34:56Z"
}
```

## Dashboard Layout

The frontend displays the overview in three primary sections:

### 1. Bitcoin Holdings
- **Total Bitcoin Holdings** — `totalBitcoin`
- **Available Bitcoin** — `availableBitcoin`
- **Bitcoin Locked as Collateral** — `collateralBitcoin`
- **Total USD Value** — `totalValueUsd`

### 2. Treasury
- **Active Loans** — `activeLoanCount`
- **Outstanding Loan Balance** — `outstandingLoanBalanceUsd`
- **Weighted Average LTV** — `weightedAverageLtv`
- **Highest Risk Loan** — `highestRiskLoan` (name, LTV, warning/liquidation prices)

### 3. Infrastructure
- **Wallet Count** — `infrastructure.walletCount`
- **Bitcoin Node Status** — `infrastructure.bitcoinNodeConnected`, block height
- **Electrum Status** — `infrastructure.electrumConnected`, block height

## Future Extensibility

### Adding a New Holding Source

1. Create `IMyHoldingSource` interface:
```csharp
public interface IMyHoldingSource
{
    Task<HoldingSource> GetHoldingAsync();
}
```

2. Implement the interface:
```csharp
public class MyHoldingSource : IMyHoldingSource
{
    public async Task<HoldingSource> GetHoldingAsync()
    {
        return new HoldingSource(
            SourceType: "MySource",
            DisplayName: "My Bitcoin Source",
            TotalBitcoin: 1.5m,
            AvailableBitcoin: 1.5m,
            CollateralBitcoin: 0m);
    }
}
```

3. Update `HoldingsService` to aggregate from all sources:
```csharp
private readonly IEnumerable<IMyHoldingSource> _sources;

public async Task<HoldingsBreakdown> GetBreakdownAsync()
{
    var holdings = new List<HoldingSource>();
    foreach (var source in _sources)
    {
        holdings.Add(await source.GetHoldingAsync());
    }
    // ... aggregate
}
```

4. Register in DI:
```csharp
services.AddScoped<IMyHoldingSource, MyHoldingSource>();
```

### Adding a New Infrastructure Service

1. Create status interface:
```csharp
public interface IMyInfrastructureProvider
{
    Task<MyServiceStatus> GetStatusAsync();
}
```

2. Update `InfrastructureService` to query it:
```csharp
public async Task<InfrastructureStatus> GetStatusAsync()
{
    var myStatus = await _myProvider.GetStatusAsync();
    // ... include in response
}
```

3. Update `InfrastructureStatus` DTO to include new field.

## Testing

### Unit Tests
- `HoldingsService` — mock wallet and loan repositories
- `LoanAnalyticsService` — mock loan repository and accrual service
- `InfrastructureService` — mock node and Electrum providers
- `DashboardQueryService` — mock all dependencies

### Integration Tests
- End-to-end `/api/dashboard/overview` with real database
- Verify calculations with known loan and wallet data

## Performance Considerations

The `/api/dashboard/overview` endpoint may query:
- All wallets (for balance sum)
- All active loans (for collateral, LTV, risk analysis)
- All loan payments (for accrual calculation)
- Bitcoin node and Electrum providers (network calls)

**Optimization opportunities (future):**
- Cache holdings breakdown (invalidate on wallet/loan changes)
- Cache infrastructure status (5-10 second TTL)
- Batch loan accrual calculations
- Async parallel queries where possible

## Related Documentation

- [`architecture.md`](./architecture.md) — overall system design
- [`security-model.md`](./security-model.md) — security assumptions
