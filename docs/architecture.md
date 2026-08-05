# CofferOS Architecture

CofferOS is a privacy-first, self-hosted Bitcoin **observability and treasury
intelligence** platform. It does not replace wallets, nodes, or Lightning
implementations — it **integrates** with them and gives you a unified local
interface to understand and organize your Bitcoin infrastructure.

Mental model: *Home Assistant for Bitcoin · Grafana for Bitcoin infrastructure ·
Plex for Bitcoin history and metadata.*

See also: [`security-model.md`](./security-model.md) — the security assumptions are
part of the product.

## 1. Style: modular monolith

CofferOS is a **modular monolith**, not microservices. One deployable backend with
clear internal module boundaries and domain-driven design. This keeps
self-hosting simple (one process, one SQLite file) while preserving clean seams for
the larger vision.

Modules (current + planned):

`Holdings` · `Wallets` · `Nodes` · `Transactions` · `UTXO` · `Lightning` · `Treasury` · `Integrations`

## 2. Layers (Clean Architecture)

```
┌───────────────────────────────────────────────┐
│ CofferOS.Api            (ASP.NET Core, DI root) │
├───────────────────────────────────────────────┤
│ CofferOS.Application    (use-cases, contracts)  │  ← provider interfaces live here
├───────────────────────────────────────────────┤
│ CofferOS.Infrastructure (EF Core, NBitcoin)     │
├───────────────────────────────────────────────┤
│ CofferOS.Domain         (entities, events)      │  ← no external dependencies
└───────────────────────────────────────────────┘
        Integrations/CofferOS.Integrations.BitcoinCore  (plugin)
```

Dependency rule: dependencies point **inward**. `Domain` depends on nothing.
`Application` depends only on `Domain`. `Infrastructure` and integrations implement
`Application` interfaces. `Api` composes everything.

| Project | Responsibility |
| ------- | -------------- |
| `CofferOS.Domain` | Descriptor-centric entities (`Wallet`, `Descriptor`, `Address`, `WalletTransaction`, `Utxo`, `Label`, `Note`), value objects (`BitcoinAmount`, `Balance`), and domain events. Pure, no dependencies. |
| `CofferOS.Application` | Use-case services (`WalletImportService`, `WalletQueryService`, `DashboardService`), aggregation services (`HoldingsService`, `LoanAnalyticsService`, `DashboardQueryService`), DTO contracts, and **abstractions**: provider plugin contracts, `IDescriptorParser`, repositories, domain-event dispatcher. |
| `CofferOS.Infrastructure` | EF Core + SQLite `DbContext`, repositories, the NBitcoin-backed descriptor parser, and the domain-event dispatcher. |
| `CofferOS.Integrations.BitcoinCore` | A plugin implementing the provider contracts over Bitcoin Core JSON-RPC (read-only). |
| `CofferOS.Api` | Minimal-API HTTP surface, DI composition, Serilog logging, config, startup migrations. |

## 3. Descriptor-centric domain

The **descriptor is the source of truth**. Addresses, UTXOs, transactions and
balances are all *derived* or *observed* data hanging off a descriptor.

```
Wallet (watch-only)
 └── Descriptor (xpub / output descriptor)   ← primary data
      └── Address (derived, index N)          ← derived data
           └── UTXO / Transaction             ← observed data
```

`Wallet` is the aggregate root and always `WatchOnly = true`.

## 4. Integrations as plugins

The provider contracts in `CofferOS.Application/Abstractions/Providers` are the
plugin API:

- `IBitcoinNodeProvider` — chain state / connectivity
- `IWalletProvider` — node-side wallet info
- `ITransactionProvider` — transaction lookup
- `IUtxoProvider` — UTXO discovery

The rest of the app depends only on these interfaces. `BitcoinCoreProvider` is the
first implementation. Planned: `ElectrumProvider`, `MempoolProvider`,
`LndProvider`, `CoreLightningProvider`. Adding one means creating a project under
`src/backend/Integrations/` and registering it in DI — nothing else changes.

## 5. Event-driven core

Modules communicate through **domain events** rather than direct calls, so they
stay decoupled. The intended observability pipeline:

```
Bitcoin Core → New Block Event → Wallet Scanner → Transaction Updated → Dashboard Updated
```

Mechanics:

- Aggregates raise events (`Entity.Raise`).
- `CofferOSDbContext.SaveChangesAsync` collects buffered events and, after commit,
  hands them to the `IDomainEventDispatcher`.
- The dispatcher resolves every `IDomainEventHandler<TEvent>` from DI and invokes it.

Today the handlers are lightweight (logging) to prove the wiring; the block →
scanner → dashboard pipeline plugs into these same seams.

## 6. Persistence

- **SQLite** via EF Core, single local file (`/data/cofferos.db`).
- Migrations are applied automatically at startup, so a fresh `docker compose up`
  produces a ready database with no manual steps.
- `DateTimeOffset` is stored via an order-preserving converter (SQLite cannot sort
  `DateTimeOffset` natively).

## 7. Frontend

- React + TypeScript + Vite + Tailwind CSS (v4).
- Lives in the same monorepo at `src/frontend/cofferos-ui`.
- Served by nginx in production; nginx reverse-proxies `/api` to the backend, so the
  UI and API share an origin (no CORS in prod). In dev, Vite proxies `/api`.
- Navigation: **Dashboard**, **Holdings**, **Treasury**, **Infrastructure**, **Settings**.
- Screens: **Dashboard** (holdings summary, loan overview, recent activity),
  **Holdings** (all Bitcoin ownership — wallets, collateral, future sources),
  **Treasury** (loans and liabilities), **Infrastructure** (node/electrum status),
  **Wallet detail** (descriptors, addresses, UTXOs, transactions, labels).
- "Add Holding" wizard replaces direct "Import Wallet" — users choose a holding
  type first, then the appropriate workflow (wallet import, loan creation, etc.).

## 8. Deployment

Docker-first. `docker compose up` builds and runs two services (`backend`,
`frontend`) plus a local volume for the database.

The architecture anticipates a future CLI installer:

```
cofferos init   # generates docker-compose config, env files, service discovery
```

Nothing in the current design blocks that: configuration is environment-variable
driven and integrations are opt-in.

## 9. Repository layout

```
CofferOS/
  src/
    backend/
      CofferOS.Api/
      CofferOS.Application/
      CofferOS.Domain/
      CofferOS.Infrastructure/
      Integrations/
        CofferOS.Integrations.BitcoinCore/
    frontend/
      cofferos-ui/
  tests/
    CofferOS.Infrastructure.Tests/
  docker/            # Dockerfiles + nginx config
  docs/              # architecture.md, security-model.md
  docker-compose.yml
```

## 10. Holdings — first-class domain concept

**Holdings** represent Bitcoin *ownership*, regardless of how it is technically
stored. A wallet is one type of holding; collateral pledged against a loan is
another. The architecture is extensible for Lightning, IRAs, ETFs, mining, and
manual entries.

```
Holding (ownership abstraction)
 ├── Wallet (self-custody, watch-only)
 ├── LoanCollateral (pledged against loans)
 ├── Lightning (channel balances) — planned
 ├── Retirement (IRA/401k)          — planned
 ├── ETF (Bitcoin ETF positions)     — planned
 ├── Mining                          — planned
 └── Manual (user-entered balance)   — planned
```

**Holdings formulas (additive model):**

- `TotalBitcoin` = Σ(wallet balances) + Σ(loan collateral amounts)
- `AvailableBitcoin` = Σ(wallet balances) — full wallet holdings, not reduced by collateral
- `CollateralBitcoin` = Σ(loan collateral amounts)

Collateral is **separate** Bitcoin held in custody at the lender. It is counted
in total holdings and is not subtracted from wallet balances. A wallet's full
balance remains "available" in the holdings view; collateral appears as its own
category representing Bitcoin owned but pledged elsewhere.

Key services:
- `IHoldingsService` — aggregates holdings from all sources into
  `HoldingsSummaryDto` and `HoldingDto`. Also provides `GetBreakdownAsync()`
  for the dashboard overview.
- `IDashboardQueryService` — orchestrates holdings, loans, and infrastructure
  into a single `DashboardOverviewDto`.
- API: `GET /api/holdings/summary`, `GET /api/holdings/`.

## 11. What is intentionally NOT built yet

Designed-for but not implemented (to keep the MVP a working vertical slice):
Electrum / Mempool / Lightning providers, live wallet scanning, treasury analytics,
and interop importers for Sparrow / Specter / Nunchuk / Casa / Unchained (via
descriptor and BIP-329 label import/export). Future holding types (Lightning,
Retirement, ETF, Mining, Manual) are stubbed in the UI as "Coming Soon".
