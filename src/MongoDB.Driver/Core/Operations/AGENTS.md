---
area: Operations, Sessions & Transactions
scope: ["src/MongoDB.Driver/Core/Operations/**/*.cs", "src/MongoDB.Driver/Core/Bindings/**/*.cs", "src/MongoDB.Driver/ClientSession*.cs", "src/MongoDB.Driver/AbortTransactionOptions.cs", "src/MongoDB.Driver/CommitTransactionOptions.cs"]
reviewer-agent: operations-reviewer
adjacent-areas: [Core (transport/SDAM), Driver root (aggregation fluent, change streams, ClientSessionHandle), Authentication]
---

# Core/Operations — AGENTS.md (Operations, Sessions & Transactions)

The execution layer between the public driver surface and the wire. Every public CRUD/aggregate/admin call eventually constructs one of these operations and hands it to an executor. This file's scope is wider than its directory: the **operations-reviewer** also owns `Core/Bindings/`, the session/transaction state machines, and the surface-level `ClientSession*` types at the driver root. (`src/MongoDB.Driver/AbortTransactionOptions.cs` and `CommitTransactionOptions.cs` are also in scope but are currently `internal sealed` — the user-facing public abstraction is `ClientSessionOptions`.)

## Operation hierarchy

Two operation interfaces defined in `Core/Operations/IOperation.cs` (both `internal`; the file is named `IOperation.cs` but contains only the two interfaces below — there is no shared `IOperation` base type):

- `IReadOperation<TResult>` — `Execute(OperationContext, IReadBinding)` + `ExecuteAsync(...)`. Returns `TResult` (often `IAsyncCursor<T>`).
- `IWriteOperation<TResult>` — `Execute(OperationContext, IWriteBinding)` + `ExecuteAsync(...)`.

Both interfaces also expose a `string OperationName { get; }` property (see `IOperation.cs`); diagnostics layers use it to label the operation in events and logs.

Plus retry-aware variants in `Core/Operations/IRetryableOperation.cs`:

- `IExecutableInRetryableReadContext<TResult>` — accepts `RetryableReadContext` instead of a raw binding.
- `IExecutableInRetryableWriteContext<TResult>` — write counterpart.

Most operations expose both sync and async; the retryable variants additionally expose `Execute` / `ExecuteAsync` overloads that take the relevant `RetryableReadContext` / `RetryableWriteContext` instead of a raw binding (there is no separate `*WithContext` method name — it's just an overload). When you fix a bug in one path, fix it in the matching other paths the operation actually defines.

## Categories

- **Find / Read**: `FindOperation<T>`, `CountOperation`, `DistinctOperation<T>`, `EstimatedDocumentCountOperation`, `ListDatabasesOperation`, `ListCollectionsOperation`, `ListIndexesOperation`, `ReadCommandOperation<TCommandResult>`.
- **Insert**: `BulkInsertOperation` (extends `BulkUnmixedWriteOperationBase<InsertRequest>`), `RetryableInsertCommandOperation`.
- **Update / Delete / Replace**: `BulkUpdateOperation`, `BulkDeleteOperation`, `RetryableUpdateCommandOperation`, `RetryableDeleteCommandOperation`, `FindOneAndUpdateOperation`, `FindOneAndDeleteOperation`, `FindOneAndReplaceOperation`, `BulkMixedWriteOperation` (the engine for `IMongoCollection.BulkWrite`).
- **Aggregate**: `AggregateOperation<T>`, `AggregateToCollectionOperation` (for `$out`/`$merge` — implements `IRetryableWriteOperation<BsonDocument>` for shape, but reports `IsOperationRetryable => false` because the trailing materializing stage forbids retry; sends a single `aggregate` `runCommand` with the materializing stage in the pipeline rather than building a separate write payload). `ChangeStreamOperation` is also a read operation that returns a cursor.
- **Cursors**: `AsyncCursor<T>` (declared `internal class` — not sealed, so adjacent areas may derive; implements `IAsyncCursor<T>` and `ICursorBatchInfo`; the public constructor parameter is typed as the base `IChannelSource`, but callers are expected to pass an `IChannelSourceHandle` so the channel source is reference-counted. For `getMore`, the cursor reuses its `_channelSource` directly; the session fork (`_channelSource.Session.Fork()`) happens inside the `ChannelReadWriteBinding` constructed around it for the request — not as a separate step in `GetNextBatch`). `ChangeStreamCursor<T>`.
- **Cluster / Admin**: `CreateIndexesOperation`, `DropIndexOperation`, `CreateCollectionOperation`, `DropCollectionOperation`, `RenameCollectionOperation`, `CreateViewOperation`, `CreateSearchIndexesOperation`, `DropSearchIndexOperation`, `UpdateSearchIndexOperation`.
- **Transaction**: abstract `EndTransactionOperation` with `CommitTransactionOperation` and `AbortTransactionOperation` derivatives. Both retryable.

## Bindings (cross-ref to `Core/Bindings/`)

Bindings are how operations get to a server. The hierarchy in `IBinding.cs` (the `IBinding` interface family is `internal`, and the concrete binding implementations — `ChannelReadBinding`, `ChannelReadWriteBinding`, `SingleServerReadBinding`, `SingleServerReadWriteBinding`, `ReadPreferenceBinding`, `WritableServerBinding`, `EndTransactionReadWriteBinding` — are also `internal sealed`. The public types under `Core/Bindings/` are the *session/transaction* `Core*` types — `CoreSession`, `CoreSessionHandle`, `CoreSessionOptions`, `CoreTransaction`, etc. See the boundary section below):

```
IBinding
├── IReadBinding         GetReadChannelSource(OperationContext, …)
├── IWriteBinding        GetWriteChannelSource(OperationContext, …)
└── IReadWriteBinding    (combines both)
```

A binding holds an `ICoreSessionHandle`. Concrete bindings include `ChannelReadBinding`, `ChannelReadWriteBinding`, `SingleServerReadBinding`, `SingleServerReadWriteBinding`, `ReadPreferenceBinding`, `WritableServerBinding`, `EndTransactionReadWriteBinding`. Some bindings (`ReadPreferenceBinding`, `WritableServerBinding`, `SingleServerReadBinding`) are decorators over a cluster-level binding that select a server; others (`ChannelReadBinding`, `ChannelReadWriteBinding`) are leaf bindings that hold an already-acquired pinned `IChannelHandle` (used post-pinning, e.g. inside transactions). The `Get*ChannelSource` methods also accept overloads for `IReadOnlyCollection<ServerDescription> deprioritizedServers` and (for writes) `IMayUseSecondaryCriteria` — both are part of the contract.

`IChannelSource` represents a route to a specific server. `IChannelSourceHandle` is reference-counted (`Fork()`); `IChannelHandle` (`IChannel.cs`) is the actual wire-protocol channel — its `Command<TResult>` and `CommandAsync<TResult>` methods bundle session, read preference, validators, and response handling.

**Pinning.** A session pins a channel and server at the start of a transaction. `CoreTransaction` itself is `public`, but the pinning members — `PinnedChannel`, `PinnedServer`, and `UnpinAll()` — are `internal` (see `Core/Bindings/CoreTransaction.cs`); they are not part of the SemVer surface. All subsequent ops in that transaction reuse the pinned route. Commit/abort use the pinned channel via `EndTransactionReadWriteBinding`. New transaction → `transaction.UnpinAll()`.

## Sessions, transactions, causal consistency

Three layers:

**Session layers:**

| Layer | Type | File |
|---|---|---|
| Public | `ClientSessionHandle` (impl), `IClientSessionHandle` (iface) | `src/MongoDB.Driver/ClientSessionHandle.cs` |
| Core | `CoreSession` | `src/MongoDB.Driver/Core/Bindings/CoreSession.cs` |
| Server-side state | `ICoreServerSession` (pooled in `ICoreServerSessionPool`) | `Core/Bindings/CoreServerSession*.cs`. The interface file itself is `ICoreServerSesssion.cs` with a triple-`s` typo — there is no non-typo'd file. Separately, the interface is declared in the `MongoDB.Driver` namespace, **not** `MongoDB.Driver.Core.Bindings` — relevant when grepping for the public type. |

**Transaction state** (orthogonal to the session layers): `CoreTransaction` (state machine) — `Core/Bindings/CoreTransaction.cs`.

`ClientSessionHandle` is a thin wrapper around `ICoreSessionHandle` — the surface holds nothing of substance. State lives in `CoreSession`.

### Cluster time vs operation time

```csharp
public BsonDocument ClusterTime => _clusterClock.ClusterTime;     // shared
public BsonTimestamp OperationTime => _operationClock.OperationTime; // per-op
```

Cluster time advances after every server response (`AdvanceClusterTime`) and is gossiped between clients via the session. Causally-consistent reads include `afterClusterTime: <latest>`; the server waits for its applied cluster time to catch up before executing.

### Transaction state machine

`CoreTransactionState` is `Starting → InProgress → Committed | Aborted`. `StartTransaction`:

1. Increments `transactionNumber` (`AdvanceTransactionNumber()`).
2. Creates a fresh `CoreTransaction(transactionNumber, options)` with state `Starting`.
3. Transitions to `InProgress` on the first command.

Pinned channel/server attach on the first command. Recovery token (returned by mongos in sharded clusters) is stored on the transaction; `commitTransaction` re-uses it on retry to find the original mongos when a different one is contacted.

**Unacknowledged writes are not allowed in transactions** — `CoreSession.StartTransaction` throws `InvalidOperationException` ("Transactions do not support unacknowledged write concerns.") if the effective write concern is `w: 0` at transaction-start time. The check fires once at start, not per-write.

## Retryable reads & writes

`RetryableReadOperationExecutor` and `RetryableWriteOperationExecutor` are static loops that:

1. Call `operationContext.ThrowIfTimedOutOrCanceled()` at the top of each iteration.
2. Execute one attempt against the binding.
3. On retryable exception, deprioritize the failed server. For ordinary retryable errors there is no back-off — the loop retries immediately against a different server. For `SystemOverloadedError`-labelled exceptions an adaptive back-off (`Thread.Sleep` on the sync path, `Task.Delay` on the async path, computed by `RetryabilityHelper.GetOperationRetryBackoffDelay(attempt, random)`) is applied; **both** paths bail early if the back-off would exceed the remaining CSOT deadline (explicit `if (remaining != Timeout.InfiniteTimeSpan && remaining < backoff) throw originalException;` check before each sleep/delay in `RetryableReadOperationExecutor.cs` / `RetryableWriteOperationExecutor.cs`). The async path needs its own check because `OperationContext.CancellationToken` is the raw caller token and does **not** fire when the CSOT deadline elapses — `Task.Delay(backoff, operationContext.CancellationToken)` would otherwise sleep the full back-off (up to `MaxBackoff`, or a server-supplied `baseBackoffMS`) regardless of the remaining timeout. The sync and async paths must stay separate here — do **not** collapse them by calling `Task.Delay(...).GetAwaiter().GetResult()` (sync-over-async on a hot retry path).
4. On non-retryable exception or attempt budget exhausted, throw.

Retry classification lives in `RetryabilityHelper`: network errors, the `RetryableWriteError` error label, and overload errors are retryable; auth/validation errors are not. Backpressure / `SystemOverloadedError` retries follow the separate adaptive-back-off path described above and are additionally gated by the `enableOverloadRetargeting` feature flag in both executors (`RetryableReadOperationExecutor.cs` / `RetryableWriteOperationExecutor.cs`) — with the flag off, overload errors do not get the adaptive back-off and behave like ordinary retryable errors. Both executors also take a `maxAdaptiveRetries` parameter (threaded through to `RetryableReadContext` / `RetryableWriteContext`) that bounds the **adaptive-back-off retry count** — it caps how many times the loop will sleep on the back-off path after observing a `SystemOverloadedError`, not the lifetime retry count from attempt 1. Once that adaptive cap is reached, the loop stops retrying even if the failure would otherwise be retryable. (See the `ShouldRetry` helper, defined in **both** `RetryableReadOperationExecutor.cs` and `RetryableWriteOperationExecutor.cs`.)

**`txnNumber` bookkeeping.** For retryable **writes**, the same `txnNumber` is reused across attempts — the server uses it to deduplicate. Don't increment it on retry. Retryable **reads** attach no `txnNumber` to their attempts. A session (`lsid`) is still required for retryable reads — `txnNumber` is the per-write deduplication tag; the `lsid` is what the server uses to identify the logical session, independent of write deduplication.

**EndTransactionOperation is always retryable** (commit/abort) at the executor layer — `IsOperationRetryable` returns `true` regardless of explicit retry-write configuration, so the executor will resubmit on a retryable error. This is a **deliberate spec deviation** from the general retryable-writes rule (which honours `retryWrites=false`): the transactions spec requires `commitTransaction` and `abortTransaction` to retry once even when retryable writes are otherwise disabled. The deeper server-side deduplication semantics for `commitTransaction` are governed by the same spec (recovery-token + `txnNumber` re-presented to a fresh mongos on retry), not by this flag. It reuses the pinned channel.

## OperationContext

`src/MongoDB.Driver/OperationContext.cs` — bundles cancellation + deadline. The type itself is `internal sealed`, even though its members are public:

```csharp
public CancellationToken CancellationToken { get; }
public TimeSpan? Timeout { get; }                    // null → infinite
public TimeSpan RemainingTimeout { get; }            // Timeout - Elapsed, clamped at 0
public void ThrowIfTimedOutOrCanceled();
```

It also carries optional OpenTelemetry metadata (operation name, db/collection names). It does **not** carry the binding or session — those are passed alongside.

CSOT propagation: every layer (executor → operation → binding → channel → wire protocol) must pass the context unchanged on the retry/operation path. Don't replace or wrap it on that path. There are a few **narrow** escape hatches in `AsyncCursor` for bounded cleanup work that must complete even when the parent deadline has expired — don't copy these patterns elsewhere:

- `KillCursors` / `KillCursorsAsync` (in `AsyncCursor.cs`) bound the cursor-release request with `operationContext.WithTimeout(TimeSpan.FromSeconds(10))` on their channel acquisition, so a server-side cursor still gets released. Called from `CloseIfNotAlreadyClosed` (e.g. when a cursor finishes during iteration), not from `Dispose` directly.
- `CloseIfNotAlreadyClosedFromDispose` (in `AsyncCursor.cs`) uses a raw `CancellationTokenSource(TimeSpan.FromSeconds(10))` because no `OperationContext` is in scope on the `Dispose` path. Don't add new raw `CancellationTokenSource` substitutions elsewhere to "match" this case.
- `GetNextBatch` / `GetNextBatchAsync` (in `AsyncCursor.cs`) and `KillCursors` / `KillCursorsAsync` themselves currently fabricate a fresh `OperationContext(null, cancellationToken)` with a `// TODO: CSOT` comment — a known gap, not a pattern to copy.

Skipping a `ThrowIfTimedOutOrCanceled` check at a re-entry point (next retry, next pool wait) is a stall risk.

## Common pitfalls

- **Sync/async path drift.** Each operation has both, often duplicated; bug fixes need to touch both.
- **Cursor disposal.** `AsyncCursor<T>` must be disposed (or fully iterated) to send `killCursors`. Otherwise the cursor lingers on the server.
- **Implicit vs explicit sessions.** No `IClientSessionHandle` parameter → driver creates an implicit session and disposes it. With an explicit session, **the user must dispose** — leaking is server-side memory growth.
- **Read concern on transactions.** Transaction-level read/write concern overrides collection defaults. Don't pass operation-level overrides expecting them to win.
- **Recovery-token loss.** If a sharded transaction's recovery token is lost (object thrown away mid-flow), commit/abort retries against a fresh mongos can fail. Don't drop the `CoreTransaction` reference.
- **Binding lifetime.** Bindings are reference-counted (`Fork`). Disposing the underlying channel-source while a forked handle is in use crashes the next operation.
- **Unacknowledged writes in transactions.** `CoreSession.StartTransaction` throws when the effective write concern is unacknowledged; the rule is `w >= 1` always.
- **Server pinning persistence.** Pinned channel doesn't unpin until a new transaction starts (`UnpinAll`). Idle pinned connections are a real cost in long-running apps.
- **`OperationContext` substitution.** Wrapping or replacing `OperationContext` mid-call drops the deadline. Always pass through.
- **Retry on the wrong layer.** This layer owns retry. The wire/transport layer (`Core/`) reports errors and lets the retry executor decide. Don't add retry inside a binding or channel.

## How to test

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Core.Operations|FullyQualifiedName~Core.Bindings"

# Sessions & transactions
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Sessions|FullyQualifiedName~Transactions"
```

JSON-driven spec tests under:

- `tests/MongoDB.Driver.Tests/Specifications/sessions/`
- `tests/MongoDB.Driver.Tests/Specifications/transactions/`
- `tests/MongoDB.Driver.Tests/Specifications/retryable-reads/`
- `tests/MongoDB.Driver.Tests/Specifications/retryable-writes/`
- `tests/MongoDB.Driver.Tests/Specifications/crud/`
- `tests/MongoDB.Driver.Tests/Specifications/server-selection/`

These exercise the state machines end-to-end. See the spec-conformance AGENTS.md.

## Boundaries

- **vs `Core/` (transport/SDAM).** This layer asks for a server via a binding and uses the channel handed back. Never opens or pools connections directly.
- **vs `Driver` root (aggregation fluent / change streams / sessions).** The root surface is the public API; this layer is the engine. Operation classes under `Core/Operations/` are mostly `internal sealed`; a few are non-sealed where derivation is intentional (`AsyncCursor<T>` is `internal class`; `EndTransactionOperation` is `internal abstract` with `CommitTransactionOperation` / `AbortTransactionOperation` deriving from it). The operation interfaces (`IReadOperation<TResult>`, `IWriteOperation<TResult>`, `IExecutableInRetryableReadContext<TResult>`, `IExecutableInRetryableWriteContext<TResult>`) are also all `internal`. **`Core/Bindings/`, however, contains several public types** — `CoreSession`, `CoreTransaction`, `CoreTransactionState`, `CoreSessionHandle`, `ICoreSession`, `ICoreSessionHandle`, `WrappingCoreSession`, `NoCoreSession`, `CoreSessionOptions`, `ICoreServerSession` — so changes there are SemVer-sensitive. Other parts of `Core/` also expose public types — see `Core/AGENTS.md`.
- **vs `Authentication/`.** Auth happens during connection establishment in the transport layer; operations don't authenticate per-op.
