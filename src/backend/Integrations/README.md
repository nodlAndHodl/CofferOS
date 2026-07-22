# Integrations

Each integration is a **plugin** that implements one or more of the provider
contracts defined in `CofferOS.Application/Abstractions/Providers`:

- `IBitcoinNodeProvider` – chain state / connectivity
- `IWalletProvider` – node-side wallet info
- `ITransactionProvider` – transaction lookup
- `IUtxoProvider` – UTXO discovery

The rest of the application depends only on these interfaces, never on a concrete
implementation. Adding an integration is done by creating a new project here and
registering it in DI (mirroring `CofferOS.Integrations.BitcoinCore`).

## Implemented

- **BitcoinCore** – `CofferOS.Integrations.BitcoinCore` (JSON-RPC, watch-only).

## Planned (designed for, not yet implemented)

| Integration    | Intended provider(s)                         | Notes                                  |
| -------------- | -------------------------------------------- | -------------------------------------- |
| Electrum       | `ElectrumProvider`                           | Electrum / Fulcrum / ElectrumX server. |
| Mempool        | `MempoolProvider`                            | Self-hosted mempool.space instance.    |
| Lightning      | `LndProvider`, `CoreLightningProvider`       | LND (gRPC/REST), Core Lightning (RPC). |

## External tools CofferOS aims to interoperate with

Sparrow, Specter, Nunchuk, Casa, Unchained — via descriptor / label (BIP-329)
import and export. These are **not** reimplemented; CofferOS reads their exports.
