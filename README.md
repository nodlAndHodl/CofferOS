# CofferOS

**Privacy-first, self-hosted Bitcoin observability and treasury intelligence.**

CofferOS is a unified local interface that helps Bitcoin users understand and manage
their infrastructure. It doesn't replace your wallet, node, or Lightning
implementation — it **integrates** with them.

> Home Assistant for Bitcoin · Grafana for Bitcoin infrastructure · Plex for Bitcoin history.

## Core principles

- **Privacy first** — runs locally, self-hosted, no cloud account, no email, no
  telemetry, no analytics, no external services. You own all data.
- **Strictly watch-only** — never stores private keys or seeds, never signs, never
  broadcasts. Works with xpubs, output descriptors, and read-only node RPC.

See [`docs/security-model.md`](docs/security-model.md) and
[`docs/architecture.md`](docs/architecture.md).

## Quick start (Docker)

```bash
git clone <this-repo> CofferOS && cd CofferOS
cp .env.example .env          # optional: adjust port / enable a node
docker compose up -d --build
```

Then open **http://localhost:8080**.

You can immediately import a watch-only wallet from an xpub / descriptor and see its
derived addresses. Connecting a Bitcoin Core node (see below) adds live chain status,
balances, UTXOs and transaction history.

Reset all local data:

```bash
docker compose down -v        # removes the SQLite volume
```

### Connecting Bitcoin Core (optional)

Edit `.env`:

```env
BITCOIN_CORE_ENABLED=true
BITCOIN_CORE_RPC_URL=http://<your-node>:8332
BITCOIN_CORE_RPC_USER=...
BITCOIN_CORE_RPC_PASSWORD=...
```

Only read-only RPCs are used.

## Local development

Backend (requires the .NET 10 SDK):

```bash
dotnet run --project src/backend/CofferOS.Api    # http://localhost:5080 (Swagger in Development)
```

Frontend (requires Node 20+):

```bash
cd src/frontend/cofferos-ui
npm install
npm run dev                                       # http://localhost:5173, proxies /api → :5080
```

Tests:

```bash
dotnet test
```

## API surface (MVP)

| Method | Route | Description |
| ------ | ----- | ----------- |
| GET  | `/api/health` | Liveness check |
| GET  | `/api/dashboard` | Aggregate balance, wallets, recent activity, node status |
| GET  | `/api/wallets` | List wallet summaries |
| POST | `/api/wallets` | Import a watch-only wallet (xpub / descriptor) |
| GET  | `/api/wallets/{id}` | Wallet detail: descriptors, addresses, UTXOs, transactions, labels |

## Tech stack

- **Backend:** .NET 10, ASP.NET Core, C#, EF Core, SQLite, NBitcoin, Serilog
- **Frontend:** React, TypeScript, Vite, Tailwind CSS, lucide-react
- **Architecture:** modular monolith, clean architecture, domain events, plugin-style integrations

## Repository layout

```
src/backend/     .NET solution (Api, Application, Domain, Infrastructure, Integrations)
src/frontend/    React + Vite UI
tests/           Test projects
docker/          Dockerfiles + nginx config
docs/            architecture.md, security-model.md
```

## Status

MVP vertical slice: import descriptors → derive addresses → persist → view in a
dashboard and wallet-detail UI, all runnable via `docker compose`. See
[`docs/architecture.md`](docs/architecture.md) §10 for what is intentionally not
built yet.

## License

Open source. License TBD.
