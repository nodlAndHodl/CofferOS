# Treasury & Holdings Architecture

CofferOS distinguishes between **holdings** (assets owned), **collateral** (assets pledged), and **liabilities** (loans). This document defines the domain model and aggregation services that power the treasury dashboard.

**Key principle:** Total holdings are the sum of wallet balances and collateral amounts (additive). Collateral is separate Bitcoin held at lenders and is not subtracted from wallet balances. Available Bitcoin equals the full wallet balances.

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
Bitcoin that is pledged against a loan. Collateral is still owned Bitcoin, held in custody at the lender. It is tracked as a distinct holding category and is additive to total holdings. Collateral is calculated as the sum of all active loan collateral amounts. It is not subtracted from wallet balances; wallets report their full balances as "available" in the holdings view.

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
1. How much Bitcoin do I own in total? (wallets + collateral, additive)
2. How much is held in wallets vs. pledged as collateral?
3. How healthy is my loan portfolio? (balances, LTVs, risk)
4. Is my infrastructure healthy? (node, Electrum)

## Service Architecture

The treasury aggregation layer is organized as a set of focused services, each responsible for a single domain concern:

```
DashboardController
        │
        ▼
IDashboardQueryService (orchestrator)
        │
        ├── IHoldingsService (GetBreakdownAsync)
        ├── DashboardService (wallets, balance, activity)
        ├── IBitcoinPriceProvider
        ├── ILoanRepository + ILoanPaymentRepository + ILoanAccrualService (for loan metrics)
        └── (infrastructure and full analytics fetched separately as needed)
```

### IHoldingsService

Aggregates Bitcoin holdings from all sources.

```csharp
public interface IHoldingsService
{
    Task<HoldingsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalBitcoinAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetAvailableBitcoinAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetCollateralBitcoinAsync(CancellationToken cancellationToken = default);
    Task<HoldingsBreakdown> GetBreakdownAsync(CancellationToken cancellationToken = default);
}
```

**Holdings formulas (additive model):**

- `TotalBitcoin` = Σ(wallet balances) + Σ(loan collateral amounts)
- `AvailableBitcoin` = Σ(wallet balances) — full wallet holdings; collateral is not subtracted
- `CollateralBitcoin` = Σ(loan collateral amounts)

Collateral represents separate Bitcoin held in custody at the lender. It contributes to total holdings but does not reduce the "available" amount reported for wallets.

**Responsibilities:**
- Sum all wallet balances (confirmed + unconfirmed)
- Sum collateral from active loans
- Compute totals using the additive model above
- Return a category breakdown (Wallet Holdings, Collateral) and a flat list of holdings

**Current implementation:**
- Queries all wallets and sums their balances
- Queries all active loans and sums their collateral amounts
- `TotalBitcoin` = wallets + collateral
- `AvailableBitcoin` = wallets (full balance)
- `CollateralBitcoin` = collateral

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
    Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);
}
```

**Responsibilities:**
- Call `IHoldingsService` for Bitcoin holdings (via `GetBreakdownAsync`)
- Call `DashboardService` for wallet summaries, total balance, wallet count, and recent activity
- Call loan repository for active loans; compute count, balances, LTVs, and highest-risk inline using payments + accrual service
- Call `IBitcoinPriceProvider` for current BTC price
- Assemble `DashboardOverviewDto` with all data
- Return single DTO to frontend

Note: Loan analytics (risk, LTV calculations) and infrastructure status are computed or fetched separately where needed; they are not part of the single overview orchestration today.

## Data Flow

### Frontend Request
```
GET /api/dashboard/overview
```

### Backend Processing
```
DashboardQueryService.GetOverviewAsync()
  ├─ DashboardService.GetAsync()
  │   ├─ WalletRepository.GetAllAsync()
  │   └─ (activity, counts, balances)
  │
  ├─ HoldingsService.GetBreakdownAsync()
  │   ├─ WalletRepository.GetAllAsync()      (wallet balances)
  │   └─ LoanRepository.GetActiveAsync()     (collateral amounts)
  │
  ├─ LoanRepository.GetActiveAsync()
  │   └─ (for each active loan)
  │       ├─ LoanPaymentRepository.GetByLoanAsync()
  │       └─ LoanAccrualService.CalculateAsync()
  │          (compute balances, LTVs, identify highest-risk)
  │
  └─ BitcoinPriceProvider.GetCurrentPriceAsync()
```

### Response
```json
{
  "totalBitcoin": 9.885,
  "availableBitcoin": 5.182,
  "collateralBitcoin": 4.703,
  "bitcoinPriceUsd": 65000,
  "totalValueUsd": 642525,
  "activeLoanCount": 2,
  "outstandingLoanBalanceUsd": 150000,
  "weightedAverageLtv": 0.45,
  "highestRiskLoan": { ... },
  "walletCount": 3,
  "totalBalance": { ... },
  "wallets": [ ... ],
  "recentActivity": { ... },
  "lastUpdatedUtc": "2026-07-27T12:34:56Z"
}
```

Under the additive model:
- `totalBitcoin` = wallet balances + collateral (e.g., 5.182 + 4.703 = 9.885)
- `availableBitcoin` = wallet balances (full amount; not reduced by collateral)
- `collateralBitcoin` = sum of active loan collateral amounts

## Dashboard Layout

The frontend displays the overview in three primary sections:

### 1. Bitcoin Holdings
- **Total Bitcoin Holdings** — `totalBitcoin` = wallets + collateral (additive)
- **Available Bitcoin** — `availableBitcoin` = full wallet balances (collateral is not subtracted)
- **Bitcoin Locked as Collateral** — `collateralBitcoin`
- **Total USD Value** — `totalValueUsd` (based on `totalBitcoin`)

### 2. Treasury
- **Active Loans** — `activeLoanCount`
- **Outstanding Loan Balance** — `outstandingLoanBalanceUsd`
- **Total collateral** — `collateralBitcoin` (BTC)
- **Collateral value** — `collateralBitcoin * bitcoinPriceUsd`
- **Weighted Average LTV** — `weightedAverageLtv`
- **Highest Risk Loan** — `highestRiskLoan` (name, LTV, warning/liquidation prices)

### 3. Infrastructure
Infrastructure status is shown on the **Infrastructure** page (node and Electrum connectivity, block heights). The dashboard overview includes `walletCount` at the top level for quick reference.

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
