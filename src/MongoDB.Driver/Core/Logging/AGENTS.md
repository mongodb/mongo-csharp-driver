---
area: Diagnostics — Logging
scope: ["src/MongoDB.Driver/Core/Logging/**/*.cs"]
reviewer-agent: diagnostics-reviewer
adjacent-areas: [Core/Events, Driver root (LoggingSettings on MongoClientSettings)]
---

# Core/Logging — AGENTS.md

Bridges the event subsystem (`Core/Events/`) to `Microsoft.Extensions.Logging`. When the user supplies a `LoggerFactory` via `MongoClientSettings.LoggingSettings`, events are offered to the configured logger as structured log entries; whether each entry is actually emitted depends on the logger's level filter. Every dispatched `EventType` must have a registered template provider — `EventLogger` dereferences the provider unconditionally, so a missing provider for a dispatched event throws rather than skipping silently. In practice every event currently dispatched is covered; if you add a new `EventType` value, you must also register a template for it in the relevant `StructuredLogTemplateProviders*.cs`. (`LogCategories.Client` is the lone **non-event** sink — top-level driver logs that don't ride on an `EventType`; do not look for a `Client` template provider.)

## Wiring

User configures via `MongoClientSettings.LoggingSettings = new LoggingSettings(loggerFactory, maxDocumentSize)`. The `LoggingSettings` type itself lives at `src/MongoDB.Driver/Core/Configuration/LoggingSettings.cs` and is surfaced through `MongoClientSettings.LoggingSettings`. It has a single constructor that takes the `LoggerFactory` plus an optional `maxDocumentSize` typed `Optional<int>` (not raw `int`). `Optional<T>` defines an implicit conversion from `T`, so `new LoggingSettings(factory, 5000)` works directly; `Optional.Create(value)` is only needed when overload resolution needs disambiguation. The cluster builder propagates `LoggingSettings` through every component that constructs an `EventLogger<T>` (server monitors, connection pool, server selection, command path).

**Redaction.** The wire layer redacts the bodies of an allowlisted set of auth-carrying commands (`authenticate`, `saslStart`, `saslContinue`, `getnonce`, `createUser`, `updateUser`, `copydbsaslstart`, `copydb`, plus `hello` / legacy-hello when carrying `speculativeAuthenticate`) before the event is published — see `Core/Connections/CommandEventHelper.ShouldRedactCommand` and the redaction note in `Core/Events/AGENTS.md`. Anything outside that allowlist is **not** scrubbed by the logging layer; it only truncates document payloads via `MaxDocumentSize`. Do not embed application-level secrets (KMS material, X.509 cert bytes, ad-hoc tokens) in arbitrary command payloads or log scopes — they will pass through unredacted into whichever sink the application configured.

`EventLogger<T>` takes an `IEventSubscriber` (the event aggregator) and an `ILogger<T>`, and internally constructs an `EventPublisher` from the subscriber. Its main entry point is `LogAndPublish<TEvent>(TEvent @event, bool skipLogging = false) where TEvent : struct, IEvent` — the generic-plus-`struct` constraint is the whole point of the boxing-free dispatch, so don't refactor it to a non-generic `IEvent` parameter.

## Categories

`LogCategories` is `internal static` and exposes nested marker types used **inside the driver** for `ILogger<T>` resolution (e.g. `LogCategories.Command`, `LogCategories.Connection`, `LogCategories.SDAM`, `LogCategories.ServerSelection`; plus `LogCategories.Client` as a non-event category for top-level driver logs). User code cannot reference these marker types — what reaches the user's `ILoggerFactory` is the **decorated string category** (`MongoDB.Command`, `MongoDB.Connection`, `MongoDB.SDAM`, `MongoDB.ServerSelection`, `MongoDB.Client`), built by `LogCategoryHelper.DecorateCategoryName` (which also applies `MongoDB.Internal` / `MongoDB.Tests` prefixes for callers outside the `MongoDB.Bson` / `MongoDB.Driver` / `MongoDB.Driver.Core` assembly allowlist — see the `LogCategoryHelper` section below). Filter by those strings in your logging framework. (Note: the per-category template-provider files — `Command`, `Cmap`, `Connection`, `Cluster`, `Sdam` — are an internal grouping for log templates and do not 1:1 map to `LogCategories` nested types.)

## Templates

Per-event templates live in `StructuredLogTemplateProviders*.cs` (one file per category: `Command`, `Cmap`, `Connection`, `Cluster`, `Sdam`). Each provider exposes:

- `GetTemplate(IEvent)` — message template with `{Param}` placeholders.
- `GetParams(IEvent, EventLogFormattingOptions)` — substitution values.
- `LogLevel` — per event type. **Headline:** command-monitoring / CMAP / connection-pool & connection-lifecycle / most SDAM templates emit at `Debug`; the connection **message-IO** channel and `ServerDescriptionChangedEvent` emit at `Trace`; `ClusterEnteredSelectionQueueEvent` is the lone `Information` template; no template currently emits at `Warning` or higher. Detail by file: in the SDAM template provider the only `Trace` template is `ServerDescriptionChangedEvent` (`SdamInformationEvent` is `Debug`); in the Connection template provider the message-IO templates (`ConnectionReceivedMessage`, `Receiving`, `ReceivingFailed`, `SendingMessages`, `SendingFailed`, `SentMessages` in `StructuredLogTemplateProvidersConnection.cs`) are all `Trace` — that whole sub-channel is `Trace` by design, while the rest of the Connection templates (lifecycle: opening/opened/failed/closing/closed/created) stay at `Debug`. Command-monitoring (`CommandStarted`/`Succeeded`/`Failed`) and server-heartbeat templates are `Debug`. The mechanism supports the full `LogLevel` range; nothing currently uses `Warning`/`Error`/`Critical`.

`EventLogFormattingOptions.MaxDocumentSize` truncates BSON command/reply documents in logs. Without it, a bulk insert with thousands of documents can flood storage.

## LogCategoryHelper

`DecorateCategoryName` builds the public log-category name by keeping only the **last** path component of a dot-separated category string and applying a fixed `MongoDB` / `MongoDB.Internal` / `MongoDB.Tests` prefix — e.g. `MongoDB.Driver.Core.Logging.LogCategories.Command` becomes `MongoDB.Command`. Nested-type `+` separators in CLR type names are normalised to `.` upstream in `GetCategoryName<T>` (`type.FullName.Replace('+', '.')`) before `DecorateCategoryName` is called, so by the time it runs the input is already dot-only — it just splits on `.`, picks the last component, and re-prefixes.

## Common pitfalls

- **`LoggerFactory` not set** → no log output (silent). Easy to miss.
- **`MaxDocumentSize = 0`** is **not** "no limit" — `TruncateIfNeeded` is `str.Length > length ? str.Substring(0, length) + "..." : str`, so any non-empty document renders as just `"..."` (an empty `""` body is unchanged because `0 > 0` is false). The default that the user-facing `LoggingSettings.MaxDocumentSize` resolves to (via `Optional<int>`) is `MongoInternalDefaults.Logging.MaxDocumentSize` (1000); pass a deliberately large `int` if you want effectively unlimited payloads. **Warning:** the `EventLogger<T>` constructor (see `Core/Logging/EventLogger.cs`) falls back to `new EventLogFormattingOptions(0)` when its `eventLogFormattingOptions` parameter is `null` — this is the aggressive-truncate path. Any internal `EventLogger<T>` call site that constructs the logger without explicit formatting options (including the `Empty` logger) gets this path. End-user code reaches `MaxDocumentSize` via the `LoggingSettings` constructor's `Optional<int>` default, which correctly resolves to `1000`; the `0` fallback is only hit by call sites that bypass `LoggingSettings`.
- **SDAM heartbeat spam** at Debug/Trace level. The SDAM category contains a mix of levels — `ServerDescriptionChangedEvent` is `Trace`, every other heartbeat/server-lifecycle template is `Debug`, and `ClusterEnteredSelectionQueueEvent` (server-selection wait queue) is `Information`. Filtering `MongoDB.SDAM` to `Information+` silences nearly all SDAM signal except the server-selection waits, which is the right tradeoff for steady-state production; use `Debug+` when you're actively debugging topology and want the heartbeat detail back.
- **Synchronous logging on monitor threads.** Templates render on the same monitor threads that fire heartbeat / CMAP events. A slow logger backend stalls those threads; the same rule that applies to event subscribers (no `.Result` / `.Wait()` / `GetAwaiter().GetResult()` — see `Core/Events/AGENTS.md`) applies inside any custom logger or sink wired up here. Use async sinks where supported by your logging framework.
- **`IsEnabled` short-circuits**. Cost when log level is disabled is negligible — don't pre-filter in user code, let the framework do it.

## How to test

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Logging|FullyQualifiedName~LogCategories"
```

Spec runner: `tests/MongoDB.Driver.Tests/Specifications/command-logging-and-monitoring/` covers the cross-driver structured-logging suite.
