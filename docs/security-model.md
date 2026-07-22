# CofferOS Security Model

> For an open-source Bitcoin project, the security assumptions **are** part of the
> product. This document is normative: code that violates it is a bug.

## 1. One-sentence summary

CofferOS is a **strictly watch-only**, **local-first** observability tool. It
understands your Bitcoin using **public** key material only, and keeps all data on
the machine you run it on.

## 2. The trust boundary

```
        Private Keys / Seeds
                 │
                 ✗   ← never cross this line into CofferOS
                 │
   Descriptors / xpubs / public keys
                 │
             CofferOS
                 │
   Analytics · Organization · Visualization
```

Everything CofferOS stores or processes is on the **public** side of the line.

## 3. Hard invariants (MUST NOT)

CofferOS must never:

- store private keys
- store seed phrases / mnemonics
- import, hold, or derive private keys from any input
- sign transactions
- broadcast transactions
- create spendable wallets on a node

These are enforced by design:

- The import surface accepts only xpubs / output descriptors. The parser
  (`NBitcoinDescriptorParser`) extracts an **extended public key** and rejects
  anything it cannot treat as public key material.
- The `Wallet` aggregate carries a `WatchOnly` invariant that is always `true`.
- Node integrations call **read-only** RPCs only (e.g. `getblockchaininfo`,
  `getrawtransaction`, `scantxoutset`). No wallet-creating or signing RPCs are used.

## 4. Privacy invariants

CofferOS must:

- run locally and be self-hostable
- require **no** cloud account, email, telemetry, analytics, or external SaaS
- keep all user data on the local machine (SQLite file on a local volume)
- make any outbound connection **explicit and user-configured** (e.g. pointing at
  *your* Bitcoin Core node)

The user owns all data. There is no phone-home path in the codebase.

## 5. Threat model (initial)

| Threat                                   | Mitigation                                                        |
| ---------------------------------------- | ----------------------------------------------------------------- |
| Malware exfiltrating keys                | No keys exist in CofferOS to steal.                               |
| Accidental spend / signing               | No signing or broadcast code paths exist.                         |
| Data leaving the device                  | No external services; only user-configured node connections.      |
| Supply-chain (vulnerable native SQLite)  | Transitive `e_sqlite3` pinned to a patched build.                 |
| Malicious descriptor input               | Parsing is fail-closed; invalid input is rejected before persistence. |

### Explicitly out of scope for the MVP

- Authentication / multi-user access control (assumes a trusted local host).
- Transport encryption between the browser and a locally-bound service.
- Hardening of the node RPC channel beyond basic auth.

These are documented so they are deliberate gaps, not accidental ones.

## 6. Where public data lives

- **SQLite database** — wallets, descriptors, derived addresses, labels, notes,
  and (once a node is connected) observed transactions/UTXOs. Stored at
  `/data/cofferos.db` inside the container, on a local Docker volume.

## 7. Reviewer checklist

When reviewing a PR, reject it if it:

- adds a dependency that phones home,
- introduces any code path that could hold a private key or sign/broadcast,
- adds telemetry/analytics,
- persists data anywhere other than the local database/volume,
- calls a non-read-only node RPC.
