---
area: Bindings (cross-ref)
scope: ["src/MongoDB.Driver/Core/Bindings/**/*.cs"]
reviewer-agent: operations-reviewer
adjacent-areas: [Core/Operations, Core (transport)]
---

# Core/Bindings — AGENTS.md

Bindings, sessions, and transactions are owned by the **Operations, Sessions & Transactions** area. See `src/MongoDB.Driver/Core/Operations/AGENTS.md` for full coverage:

- `IBinding` / `IReadBinding` / `IWriteBinding` / `IReadWriteBinding` hierarchy
- `IChannelSource`, `IChannelSourceHandle`, `IChannel`, `IChannelHandle`
- `CoreSession` (cluster time, operation time, transaction state, pinning)
- `CoreTransaction` (state machine: `Starting → InProgress → Committed | Aborted`, recovery token)
- Server pinning behavior during transactions

**Public SemVer surface here.** The binding interfaces and the concrete binding implementations are `internal`, but the session/transaction types `CoreSession`, `CoreSessionHandle`, `CoreSessionOptions`, `CoreTransaction`, `CoreTransactionState`, `ICoreSession`, `ICoreSessionHandle`, `ICoreServerSession`, `WrappingCoreSession`, and `NoCoreSession` are **public** — changes are SemVer-sensitive. (`CoreTransaction`'s pinning members — `PinnedChannel`, `PinnedServer`, `UnpinAll()` — are `internal`.)

**Grepping pitfalls.** The `ICoreServerSession` interface lives in a file with a triple-`s` typo — `ICoreServerSesssion.cs` — there is no non-typo'd file. The interface itself is declared in the `MongoDB.Driver` namespace, **not** `MongoDB.Driver.Core.Bindings`, despite living in this directory.

Reviewer: `operations-reviewer`.
