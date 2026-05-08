---
area: Diagnostics — Events
scope: ["src/MongoDB.Driver/Core/Events/**/*.cs"]
reviewer-agent: diagnostics-reviewer
adjacent-areas: [Core/Logging, Core (transport), Core/Operations]
---

# Core/Events — AGENTS.md

Structured event publishing for command monitoring, connection-pool lifecycle (CMAP), per-connection lifecycle, server discovery & monitoring (SDAM), and cluster topology. Foundation for the structured-logging layer in `Core/Logging/` and for any custom observability subscribers.

## Architecture

- **`IEvent`** — visibility and shape:
  - `IEvent` itself is an `internal` marker interface — external subscribers can never name it directly.
  - Almost all per-event concrete types are `public` value types (`struct`s like `CommandStartedEvent`, `ConnectionPoolReadyEvent`, etc.). The one exception in this codebase is `ClusterEnteredSelectionQueueEvent`, declared `internal struct` — listed below under cluster/server-selection events but not subscribable by external code.
  - `IEventSubscriber` is `public`, so external subscribers register against the public concrete event structs by type. All events are **`struct`** value types (no heap allocation on hot paths).
- **`IEventSubscriber`** — `bool TryGetEventHandler<TEvent>(out Action<TEvent> handler)`. Subscribers declare which events they handle at registration time; `EventPublisher.Publish` resolves and caches the handler delegate (or `null`) on the **first publish** of each event type — not at startup — so subsequent publishes of an event with no handler are effectively free. Implementation detail: the cache is a fixed-size `Delegate[__eventTypesCount]` field named `_eventHandlers`, indexed by `(int)EventType` (`EventPublisher.cs`), so all `EventType` values must remain a dense `[0, length)` range. Do not assign explicit numeric values and do not leave gaps; appending at the end is safe, but inserting in the middle renumbers every later entry. Adding a new event means adding an `EventType` entry as well as the event struct itself.
- **`EventAggregator`** — internal multiplexer; lazily combines handlers across subscribers per `TEvent` via `Delegate.Combine` in `TryGetEventHandler` (no pre-built startup composite).
- **Synchronous, in-thread dispatch.** Events fire on the thread that triggered them (application thread for command events, monitor threads for heartbeats). A slow subscriber blocks the operation.
- **No exception isolation.** A subscriber that throws propagates into the calling layer. Handler code must do its own try/catch.

## Event categories

- **Command monitoring** — `CommandStartedEvent`, `CommandSucceededEvent`, `CommandFailedEvent`. Carry command name, BSON document (potentially truncated), database, operation/request IDs, connection ID, duration, timestamp.
- **Connection pool (CMAP)** — grouped by phase:
  - *Pool lifecycle* (`*ing` / `*ed` pairs): `ConnectionPoolOpeningEvent`/`OpenedEvent`, `ConnectionPoolClearingEvent`/`ClearedEvent`, `ConnectionPoolClosingEvent`/`ClosedEvent`; plus the standalone `ConnectionPoolReadyEvent`.
  - *Checkout* (intent / success / failure triplet): `ConnectionPoolCheckingOutConnectionEvent` → either `CheckedOutConnectionEvent` or `CheckingOutConnectionFailedEvent` (with reason enum).
  - *Check-in* (intent / success pair): `ConnectionPoolCheckingInConnectionEvent`, `CheckedInConnectionEvent`.
  - *Per-connection add/remove* inside the pool: `ConnectionPoolAddingConnectionEvent`/`AddedConnectionEvent`, `RemovingConnectionEvent`/`RemovedConnectionEvent`.

  `ConnectionPoolReadyEvent` has no `*ing`→`*ed` lifecycle partner — it fires once when the pool transitions to ready; the asymmetry is intentional (there's no observable "becoming ready" intermediate state to report). The checkout/`*Failed` triplets above are a separate asymmetry pattern, not a missing partner.
- **Connection** — per-connection lifecycle (`ConnectionOpening`, `Opened`, `OpeningFailed` (`ConnectionOpeningFailedEvent` — failure during open) and `Failed` (`ConnectionFailedEvent` — failure on an established connection), `Closing`, `Closed`, `Created`) and message-level events (`ConnectionSendingMessages`, `Sent`, `ReceivingMessage`, `Received`, plus failures). The `Ready` lifecycle event is at the **pool** level (`ConnectionPoolReadyEvent`), not the connection level — there is no `ConnectionReadyEvent`.
- **SDAM** — `ServerHeartbeatStarted`, `Succeeded`, `Failed` (carry the `awaited` flag for streaming heartbeats); `ServerDescriptionChanged`; `ServerOpening` / `Opened` / `Closing` / `Closed`; `SdamInformation` (diagnostic snapshot).
- **Cluster & server selection** — `ClusterOpening` / `Opened` / `Closing` / `Closed`, `ClusterDescriptionChanged`, server roster mutations (`ClusterAddingServerEvent`/`ClusterAddedServerEvent`, `ClusterRemovingServerEvent`/`ClusterRemovedServerEvent`), server selection (`ClusterSelectingServerEvent`, `ClusterSelectedServerEvent`, `ClusterSelectingServerFailedEvent`, `ClusterEnteredSelectionQueueEvent`). Note the identifier asymmetry on the last one: the struct is `ClusterEnteredSelectionQueueEvent` (`Queue`) while the corresponding `EventType` enum value is `ClusterEnteredSelectionWaitQueue` (`WaitQueue`) — the enum entry is what indexes the template-provider cache.

## Common pitfalls

- **Exceptions in handlers stall ops.** Always catch in subscriber code. The driver provides no recovery.
- **Subscribing/unsubscribing during dispatch is unsafe.** Configure subscribers before client construction, not from inside an event handler.
- **Heartbeat noise.** SDAM events fire every heartbeat interval per server. Without filtering, they dominate logs.
- **Large command BSON in subscribers.** Bulk inserts can ship multi-MB payloads. Filter or truncate in your subscriber, just like the logging layer does.
- **Slow subscribers cost real latency.** Because dispatch is synchronous, any blocking work in a handler — including sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`) on an async operation — directly stalls the driver thread that fired the event. Queue work to a background thread if needed.

## Redaction

The wire layer **does** redact a closed allowlist of authentication-bearing commands before they are published to subscribers or the structured-logging layer. `CommandEventHelper.ShouldRedactCommand` (in `Core/Connections/`) currently strips the body of `authenticate`, `saslStart`, `saslContinue`, `getnonce`, `createUser`, `updateUser`, `copydbsaslstart`, `copydb`, plus `hello` / legacy-hello when the command carries `speculativeAuthenticate`. Adding a new auth-carrying command means extending that allowlist — otherwise its payload will leak into every subscriber.

Outside that allowlist there is **no** generic secret scrubbing of `CommandStartedEvent.Command`, `CommandSucceededEvent.Reply`, etc. Application-level secrets you embed in arbitrary commands (e.g., a connection string in a `runCommand` body, KMS material, X.509 cert bytes) are *not* automatically redacted — keep them out of event payloads and log scopes.

## OpenTelemetry

The driver does **not** emit OTel signals directly. Bridge via `IEventSubscriber` (events → OTel spans/metrics) or via the structured logging layer (events → `ILogger<T>` → OTel logging exporter). See `Core/Logging/AGENTS.md`.

## Spec links

- `tests/MongoDB.Driver.Tests/Specifications/command-logging-and-monitoring/`
- `tests/MongoDB.Driver.Tests/Specifications/connection-monitoring-and-pooling/`
- `tests/MongoDB.Driver.Tests/Specifications/server-discovery-and-monitoring/`

## How to test

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Core.Events"
```

`EventCapturer` (in `tests/MongoDB.Driver.TestHelpers/`) is the standard way to capture events in tests.
