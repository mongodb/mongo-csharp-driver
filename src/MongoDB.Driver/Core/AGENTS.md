---
area: Connection & Transport (SDAM)
scope: ["src/MongoDB.Driver/Core/Clusters/**/*.cs", "src/MongoDB.Driver/Core/Servers/**/*.cs", "src/MongoDB.Driver/Core/Connections/**/*.cs", "src/MongoDB.Driver/Core/ConnectionPools/**/*.cs", "src/MongoDB.Driver/Core/WireProtocol/**/*.cs", "src/MongoDB.Driver/Core/Compression/**/*.cs", "src/MongoDB.Driver/Core/Configuration/**/*.cs", "src/MongoDB.Driver/Core/Misc/**/*.cs"]
reviewer-agent: transport-reviewer
adjacent-areas: [Core/Operations, Core/Events, Core/Logging, Driver/Authentication]
---

# MongoDB.Driver / Core — AGENTS.md (Connection & Transport / SDAM)

This file covers the **Server Discovery And Monitoring (SDAM)** topology layer, connection pooling, wire protocol, and supporting infrastructure under `src/MongoDB.Driver/Core/`. Scoped subdirectories: `Clusters/`, `Servers/`, `Connections/`, `ConnectionPools/`, `WireProtocol/`, `Compression/`, `Configuration/`, `Misc/`. (`Core/Authentication/` and `Core/Bindings/` also live under `Core/` but are out of scope for this reviewer — see the auth-area and operations-area AGENTS.md.)

**Ownership boundary:** the Operations layer (`Core/Operations/`, `Core/Bindings/`) sits above this and owns retry, session attachment, and cursor lifecycle. Events and Logging have their own `AGENTS.md` files. Authentication mechanisms live in `Authentication/` — but this layer drives the auth handshake during connection establishment.

---

## 1. Clusters / SDAM Topology

**Key files:** `Cluster.cs`, `ClusterDescription.cs`, `ClusterType.cs`, `ClusterFactory.cs`, `MultiServerCluster.cs`, `LoadBalancedCluster.cs`, `SingleServerCluster.cs`, `DnsMonitor.cs`, `ServerSelectors/`. Plus supporting ID/state types — `ClusterClock.cs`, `ClusterId.cs`, `ClusterState.cs`, `ElectionId.cs`, `ReplicaSetConfig.cs` — and the various `I*` interfaces.

### Topology types and state machine

- **`ClusterType`** enum: `Unknown`, `Standalone`, `ReplicaSet`, `Sharded`, `LoadBalanced`. (Direct-connection mode is a connection-string option, not a `ClusterType` value — it's expressed via `ClusterSettings.DirectConnection` and produces a `SingleServerCluster`.) Determines selector behavior and read/write affinity.
- **`ClusterDescription`** immutable snapshot of cluster state: holds a list of `ServerDescription`s, the cluster type, and computed properties like `LogicalSessionTimeout`. Updated atomically when servers report topology changes.
- **`Cluster`** abstract base; concrete implementations `MultiServerCluster` (standard SDAM for Standalone/ReplicaSet/Sharded), `SingleServerCluster` (direct-connection mode), `LoadBalancedCluster` (LB mode with single virtual endpoint).

### Cluster initialization and lifecycle

- **Initialization:** `Cluster.Initialize()` triggers server creation and monitoring threads. `MultiServerCluster` spawns a background DNS monitor thread (for SRV resolution) and per-server monitor threads. `LoadBalancedCluster` creates one virtual server and a DNS monitor.
- **Disposal:** Cancels all monitoring threads and closes pooled connections. Cluster remains queryable briefly during disposal for graceful shutdown.
- **Topology updates:** `ClusterDescriptionChangedEventArgs` published whenever a server's description changes. Listeners update read preference routing and failover logic.

### Server selection (`IServerSelector` chain)

- **`IServerSelector`:** Single responsibility — filter servers by one criterion (read preference, latency, writable, etc.).
- **`CompositeServerSelector`:** Chains selectors; later selectors filter the result of earlier ones.
- **Built-in selectors:**
  - `ReadPreferenceServerSelector` — applies the read preference mode (Primary, Secondary, Nearest, etc.) and filters by tags.
  - `WritableServerSelector` — returns only writable servers (Primaries in RS, all in Standalone/Sharded).
  - `LatencyLimitingServerSelector` — filters servers within `LocalThreshold` of the fastest server, reducing tail latency.
  - `EndPointServerSelector` — filters a specific server by endpoint (used internally for pinned connections).
  - `DeprioritizedServersServerSelector` — deprioritizes known bad servers.
  - `PriorityServerSelector` — `[Obsolete]`. The public, legacy form of the deprioritization filter: a standalone `public sealed` selector that takes a `deprioritizedServers` collection and removes those endpoints from the candidate set on sharded clusters. The internal replacement is `DeprioritizedServersServerSelector` (an `internal sealed` *composing* selector that wraps another `IServerSelector` and filters its output). Neither is related to replica-set priorities; `PriorityServerSelector` is slated for removal in a future release.
  - `OperationsCountServerSelector` — load-balances by operation count (for load-balanced deployments).
  - `RandomServerSelector` — random tie-breaker when multiple servers are equally good.
  - `DelegateServerSelector` — wraps an arbitrary `Func<…>` for ad-hoc selection logic (rarely used in production code; useful for tests and bespoke deployments).

**Selection flow:** `SelectServer(selector, timeout)` blocks until a suitable server is found or timeout expires. Internally it waits on the cluster's `DescriptionChanged` event (via an internal `TaskCompletionSource`) and re-runs the selector each time topology changes. (The `MaxServerSelectionWaitQueueSize` setting on `ClusterSettings` bounds the number of waiters, but there is no public `ServerSelectionWaitQueue` type.)

### SDAM state machine and monitoring loop

- **Heartbeat interval:** Configurable, default 10 seconds. In **polling** mode the server monitor sends an `isMaster` / `hello` command on each interval; in **streaming** mode (used against servers that advertise `helloOk`) the monitor instead holds an awaitable `hello` open and processes responses as the server pushes them — the interval bounds polling fallback and idle-keepalive only. The choice between polling and streaming is governed by `ServerMonitoringMode` (`Auto` / `Stream` / `Poll`; `Auto` is the default and resolves to streaming on supported servers).
- **FaaS polling override:** FaaS environments force polling mode under `Auto` regardless of server support — streaming heartbeats and short-lived FaaS invocations don't mix. Detection lives in `ServerMonitor.IsRunningInFaaS` (`Core/Servers/ServerMonitor.cs`), and the env vars actually checked are:
  - **AWS Lambda:** `AWS_EXECUTION_ENV` (must start with `AWS_Lambda_`) and `AWS_LAMBDA_RUNTIME_API`.
  - **Azure Functions:** `FUNCTIONS_WORKER_RUNTIME`.
  - **GCP:** `K_SERVICE` (set by Cloud Run and the Cloud Functions Gen2 runtime, which is Cloud Run under the hood — this is doc-author interpretation; the source comment says only "gcp.func") and `FUNCTION_NAME` (Cloud Functions Gen1).
  - **Vercel:** `VERCEL`.

  See `Core/Servers/ServerMonitor.cs` for the authoritative list.
- **Topology version:** Servers embed a `TopologyVersion` (process ID + counter) in heartbeat responses. Stale responses are ignored; newer topology versions trigger faster invalidation.
- **Streaming heartbeat (awaitable hello):** Servers that advertise `helloOk` support the `"awaitable": true` option, allowing the server to hold the heartbeat response until the topology changes. Reduces latency on failover.
- **Polling heartbeat:** Fallback for older servers. The monitor thread sleeps between intervals.
- **State invalidation:** On error or topology change, the cluster updates generation counters in the connection pool (forcing reconnection) and marks the offending server as unknown.

---

## 2. Servers / Per-server abstractions

**Key files:** `Server.cs`, `DefaultServer.cs`, `ServerDescription.cs`, `ServerType.cs`, `ServerMonitor.cs`, `RoundTripTimeMonitor.cs`, `TopologyVersion.cs`, `LoadBalancedServer.cs`. Plus per-server state and ID types — `ServerMonitorSettings.cs`, `ServerMonitoringMode.cs`, `ServerState.cs`, `ServerId.cs`, `SelectedServer.cs`, `ServerChannel.cs`.

### Server types

- **`ServerType`:** `Unknown`, `Standalone`, `ShardRouter`, `ReplicaSetPrimary`, `ReplicaSetSecondary`, `ReplicaSetPassive` (still public, but `[Obsolete]`), `ReplicaSetArbiter`, `ReplicaSetOther`, `ReplicaSetGhost`, `LoadBalanced`.
- **`ServerDescription`** immutable: endpoint, type, round-trip time (RTT), tags, setName, electionId, and health. Computed during heartbeat, never mutated.

### Server monitoring

- **`IServerMonitor`** — spawns a background thread that periodically contacts the server.
- **`ServerMonitor`** — concrete implementation. Manages a single connection for heartbeat probes; creates and destroys it as needed.
  - In polling mode, sends `hello` (or legacy `isMaster`) on each heartbeat interval; in streaming mode, holds a single awaitable `hello` open and processes pushed responses (the interval bounds polling fallback and idle-keepalive).
  - Interprets response via `HelloResult`, updating `ServerDescription`.
  - On error, classifies the exception (network, auth, incompatible version, etc.) and updates the server state accordingly.
  - Publishes `ServerDescriptionChangedEventArgs` when state changes.
- **`RoundTripTimeMonitor`** — the instance is always constructed alongside the `ServerMonitor`, but is only `Start()`-ed as a separate background thread in streaming mode; in that mode it measures latency via a dedicated heartbeat connection so the streaming connection isn't disturbed. Computes exponentially-weighted moving average (EWMA) of RTT. Polling-mode monitors don't start the dedicated RTT thread; they add RTT samples directly from each heartbeat round-trip.
- **Streaming heartbeat:** If the server supports `awaitable: true`, the monitor reuses the same connection across heartbeats and waits for the server to push updates.

### Error classification and SDAM recovery

- On a failed command, the driver inspects the error to decide whether the server is still writable, readable, or unknown.
- Examples:
  - `NotPrimaryError` → server marked `Unknown`; the next `hello` reclassifies it (commonly to `ReplicaSetSecondary`).
  - `ShutdownInProgressError` → server becomes Unknown.
  - Network timeouts → server becomes Unknown.
  - Auth failures → server becomes Unknown; client stops retrying.
- Error recovery is **not immediate**. The next heartbeat after an error confirms the server's new state.

---

## 3. Connections / Single-connection abstractions

**Files:** `BinaryConnection.cs`, `ConnectionDescription.cs`, `ConnectionId.cs`, `ConnectionInitializer.cs`, `IConnection.cs`, `HelloResult.cs`, `ClientDocumentHelper.cs`, `HelloHelper.cs`, `TcpStreamFactory.cs`, `SslStreamFactory.cs`, `Socks5ProxyStreamFactory.cs`.

### Connection lifecycle

- **`BinaryConnection`** — wraps a TCP/TLS stream. On `Open()`:
  1. Opens the underlying stream (TCP or TLS).
  2. Sends a `hello` command — `ConnectionInitializer` builds it via `HelloHelper.AddCompressorsToCommand` (compressor list embedded) and the authenticator's `CustomizeInitialHelloCommand` (often embedding `speculativeAuthenticate`). Both compression negotiation and the first auth step ride on this single hello.
  3. Authenticates the rest of the way if credentials are supplied and speculative auth didn't complete.
  4. Updates `ConnectionDescription` with negotiated parameters (compressor, max BSON size, max wire version, …) from the hello reply.
- **`ConnectionId`** — unique identifier combining server ID and a local counter. Used in SDAM events and logging.
- **`ConnectionDescription`** — immutable snapshot of negotiated connection state: hello result, compression type, auth mechanism, etc.

### isMaster / hello handshake

- **`HelloResult`** wraps the `hello` (or legacy `isMaster`) command response.
- Contains: topology version, server type, replica set info, compressor list, max BSON size, max wire version, etc.
- **Hello vs isMaster selection:** Drivers send `hello` against servers that advertise `helloOk` in their initial reply and fall back to `isMaster` for older servers. The two responses are payload-compatible; only the command name differs. The gate is the runtime `HelloOk` flag (see `ServerMonitor.cs` and `HelloResult.HelloOk`), **not** a hardcoded server-version floor — past versions of this file cited "5.0+" and then "4.4.2+"; both are misleading and should not be re-introduced.
- **Speculative authentication:** The hello may carry an embedded `"speculativeAuthenticate"` payload, allowing the first SASL step to ride on the handshake — see the auth-area `AGENTS.md` for which mechanisms speculate.

### Compression negotiation

- During connection initialization, the driver sends a list of supported compressors.
- The server responds with its preference.
- Both sides agree on a single compressor type; messages are compressed after that point.
- See also `Compression/` subdirectory.

### Stream factories and TLS/SOCKS5

- **`IStreamFactory`** — creates the underlying I/O stream.
- **`TcpStreamFactory`** — opens a `TcpClient`, applies keep-alive, then wraps it.
- **`SslStreamFactory`** — wraps the TCP stream in an `SslStream` for TLS. Validates certificates and handles SNI. Validation is configured via `SslStreamSettings.ServerCertificateValidationCallback`; the user-facing "trust anything" toggle is `MongoClientSettings.AllowInsecureTls` (there is no `SslStreamSettings.AllowInvalidCertificates` property).
- **`Socks5ProxyStreamFactory`** — wraps another `IStreamFactory` and performs the SOCKS5 handshake first, so any TLS layer sits on top of the proxied stream.

### Command monitoring at the connection level

- **`CommandEventHelper`** — fires `CommandStartedEvent` before sending a command and `CommandSucceededEvent` / `CommandFailedEvent` after.
- Events include command text, database, operation ID, and timing.
- **Encryption:** `CommandMessageFieldEncryptor` / `CommandMessageFieldDecryptor` modify command BSON before/after the wire.

---

## 4. ConnectionPools / Pool implementation

**Files:** `ExclusiveConnectionPool.cs`, `ExclusiveConnectionPool.Helpers.cs`, `ConnectionPoolSettings.cs`, `MaintenanceHelper.cs`, `ServiceStates.cs`, `CheckOutReasonCounter.cs`.

### CMAP (Connection Monitoring And Pooling)

- **`ExclusiveConnectionPool`** — single pool per (server, endpoint) pair. Follows the CMAP spec.
- **Pool state:** `PoolState` tracks open/closed status and event logging.
- **Maintenance thread:** `MaintenanceHelper` periodically removes idle connections and checks for stale generation.

### Pool generation and fork detection

- **Generation counter:** Incremented when the pool is cleared (e.g., after topology change or connection error).
- **`ServiceStates`:** Tracks per-service (load-balanced service) generation and connection count. The pool generation is incremented on cluster-driven invalidation events (e.g., `Clear()` after topology change, error classification, or LB service-id rollover); stale connections are then discarded and new ones are created on next checkout.

### Connection management

- **`CheckOutReasonCounter`** — categorizes checkout reasons (explicit, implicit, pool exhaustion, etc.) for observability.
- **Wait queue:** Async-friendly semaphore that blocks callers when the pool is at max connections or max connecting.
- **Semaphores:**
  - `_maxConnectionsQueue` — enforces `MaxConnections` limit.
  - `_maxConnectingQueue` — enforces `MaxConnecting` limit (concurrent TCP handshakes).
- **Idle eviction:** Connections unused for longer than `MaxIdleTime` are removed during maintenance. `MaxIdleTime` lives on `ConnectionSettings` (not `ConnectionPoolSettings`) — the pool reads it off each connection during the maintenance sweep.

### Settings

- **`ConnectionPoolSettings`:** `MaxConnections`, `MaxConnecting`, `MinConnections`, `MaintenanceInterval`, `WaitQueueTimeout`, `WaitQueueSize` (only `WaitQueueSize` is `[Obsolete]`). Note: `MaxIdleTime` is on `ConnectionSettings`, not `ConnectionPoolSettings` — the pool reads it from each connection during maintenance.
- The maintenance loop in `MaintenanceHelper` proactively brings the pool up to `MinConnections` on each maintenance tick (the actual min-size top-up runs via private helpers — there is no public `EnsureMinSize` API). Warm-up runs on the background maintenance thread, not at client construction: if the first checkout happens before maintenance has caught up, that checkout still synchronously creates a connection (subject to the `MaxConnecting` semaphore).

---

## 5. WireProtocol / OP_MSG and command messaging

**Files:** `CommandWireProtocol.cs`, `CommandUsingCommandMessageWireProtocol.cs`, `CommandUsingQueryMessageWireProtocol.cs`, `CursorBatch.cs`, `Messages/`, `WireProtocolMessageEncoders/`.

### Command wire protocol layers

- **`IWireProtocol<TResult>`** — abstract interface for a single request–response exchange.
- **`CommandWireProtocol<T>`** — generic high-level protocol. Routes to either `CommandUsingCommandMessageWireProtocol` (OP_MSG, modern) or `CommandUsingQueryMessageWireProtocol` (OP_QUERY, legacy fallback).

### OP_MSG (MongoDB 3.6+)

- **`CommandUsingCommandMessageWireProtocol`** — sends commands as OP_MSG (opcode 2013).
- **Message format:**
  - Header: flag bits, opcode, message length, request ID.
  - Payload sections:
    - Body section (type 0): the command document itself.
    - Document sections (type 1): bulk write payloads (insert/update/delete).
    - Compressed payloads: if compression is negotiated, the entire message is compressed.
- **`moreToCome`:** A bidirectional OP_MSG flag bit. **Inbound** (server-set on a response): more responses will follow on the same request — used by exhaust cursors (see also `Core/Operations/AGENTS.md` for cursor lifecycle) and the streaming `hello` heartbeat. **Outbound** (client-set on a request): tells the server not to reply at all — used by unacknowledged writes (`w: 0`) and other fire-and-forget command shapes. It is **not** a way for clients to batch outbound `getMore` requests — outbound batching uses cursor `batchSize` instead.
- **Response:** `CommandResponseMessage` contains a server reply document and optional cursor data.

### Legacy OP_QUERY / OP_GET_MORE / OP_KILL_CURSORS

- **`CommandUsingQueryMessageWireProtocol`** — fallback for servers < 3.6. Sends OP_QUERY (opcode 2004).
- Still present but deprecated; new feature work does not go here.
- getMore via OP_GET_MORE, cursor cleanup via OP_KILL_CURSORS.

### Cursor batching

- **`CursorBatch<T>`** — immutable result: cursor ID, document list, and optional post-batch resume token (for change streams).
- Cursor ID 0 means the cursor is exhausted; `CursorBatch` can be iterated to get all documents.
- getMore command repeated until cursor ID is 0.

### Messages subdirectory

- `CommandMessage`, `CommandRequestMessage`, `CommandResponseMessage` — wire message types.
- `CommandMessageSection`, `BatchableCommandMessageSection` — payload sections within OP_MSG.
- `QueryMessage`, `ReplyMessage` — legacy OP_QUERY / OP_REPLY.
- `CompressedMessage` — OP_COMPRESSED wrapper (opcode 2012). The wire format is a standard message header with opcode 2012, followed by the `originalOpcode` of the wrapped message, a `compressorId`, and the compressed payload. It is a distinct message type, not a flag on OP_MSG.

### Encoders

- `WireProtocolMessageEncoders/BinaryEncoders/` — encode/decode wire messages from/to BSON.
- Each message type has an encoder. Encoders handle endianness, section encoding, compression/decompression.

---

## 6. Compression / Wire compression

**Files:** `ICompressor.cs`, `SnappyCompressor.cs`, `ZlibCompressor.cs`, `ZstandardCompressor.cs`, `NoopCompressor.cs`, `CompressorSource.cs`, `CompressorTypeMapper.cs`.

### Compressor types

- **`CompressorType`** enum: `Noop` (0), `Snappy` (1), `Zlib` (2), `ZStandard` (3).
- Each type has a numeric wire protocol ID (as defined in the spec).

### ICompressor interface

- `Type` property: the compressor type.
- `Compress(Stream input, Stream output)` — compress input to output.
- `Decompress(Stream input, Stream output)` — decompress input to output.

### Compressor implementations

- **`SnappyCompressor`** — wraps the Snappier library (`using Snappier;`).
- **`ZlibCompressor`** — wraps `SharpCompress.Compressors.Deflate.ZlibStream`.
- **`ZstandardCompressor`** — wraps the ZstdSharp library.
- **`NoopCompressor`** — pass-through (for testing or no compression).

### Negotiation

- **`CompressorSource`** — holds the pool of supported compressors based on `ConnectionSettings.Compressors`.
- During `hello` handshake, the server advertises its supported compressors. The connection picks the first one in the client's list that the server also supports.
- Compression is per-connection and negotiated during initialization.

---

## 7. Configuration / Connection string and settings hierarchy

**Files:** `ConnectionString.cs`, `ClusterBuilder.cs`, `ClusterSettings.cs`, `ServerSettings.cs`, `ConnectionPoolSettings.cs`, `ConnectionSettings.cs`, `TcpStreamSettings.cs`, `SslStreamSettings.cs`, `Socks5ProxyStreamSettings.cs`, `CompressorConfiguration.cs`.

### ConnectionString parsing

- **`ConnectionString`** — parses `mongodb://` and `mongodb+srv://` URIs.
- Supports query parameters: `heartbeatIntervalMS`, `serverSelectionTimeoutMS`, `maxPoolSize`, `tls`, `tlsInsecure`, `tlsDisableCertificateRevocationCheck`, `compressors`, `loadBalanced`, `directConnection`, `srvMaxHosts`, `srvServiceName`, among others. (Note: `tlsCertificateKeyFile` is **not** parsed by the C# driver — supply client certificates programmatically via `SslSettings.ClientCertificates`.)
- Performs DNS-SRV resolution for `mongodb+srv://` (see `DnsClientWrapper` in `Misc/`).
- Returns a normalized list of endpoints and a dictionary of options.

### DNS-SRV resolution

- **Trigger:** When the scheme is `mongodb+srv://`, the cluster performs SRV resolution.
- **Format:** `_mongodb._tcp.example.com` → resolves to a list of (host, port) pairs.
- **TXT records:** Optional TXT records may override options like compressors and auth source.
- **TTL caching:** Not persistently cached by the driver; `DnsMonitor` holds the latest resolved endpoints in memory between rescans, but each rescan re-queries DNS. Caching across processes is the OS resolver's job.
- **`DnsMonitor`** — background thread that periodically re-resolves SRV and detects topology changes in the DNS records (e.g., nodes added/removed).

### Load-balanced mode

- Enabled via the `loadBalanced=true` query parameter on a standard `mongodb://` or `mongodb+srv://` URI — there is no distinct `mongodb+srv+lb://` scheme; only `mongodb://` and `mongodb+srv://` are recognised by the driver.
- Only one endpoint is expected; the LB proxy handles server routing internally.
- `LoadBalancedCluster` creates a single virtual server and does not perform server selection.
- In LB mode the driver runs **no background server monitor** — `LoadBalancedServer` has no `IServerMonitor` (only the regular `DefaultServer` does), so there are no periodic background heartbeats. A handshake-time `hello` is still sent per connection during initialization (negotiating `helloOk`, `maxBsonObjectSize`, etc.); what's absent is the *background* server monitor and its periodic heartbeats. The cluster type is `ClusterType.LoadBalanced` and the server type is `ServerType.LoadBalanced`.

### Settings hierarchy

1. **`ClusterSettings`** — cluster-wide: `LoadBalanced`, `ReplicaSetName`, `DirectConnection`, `EndPoints`, heartbeat intervals, etc.
2. **`ServerSettings`** — per-server defaults for heartbeat timeout.
3. **`ConnectionPoolSettings`** — per-pool: `MaxConnections`, `MaxConnecting`, `MinConnections`, `MaintenanceInterval`, `WaitQueueTimeout`, and the `[Obsolete]` `WaitQueueSize`.
4. **`ConnectionSettings`** — per-connection: `MaxIdleTime`, compressors, application name, server API version, max BSON size negotiation.
5. **`TcpStreamSettings`** — socket-level: `AddressFamily`, `ConnectTimeout`, `ReadTimeout`, `WriteTimeout`, `ReceiveBufferSize`, `SendBufferSize`, `SocketConfigurator` (callback for arbitrary `Socket` tuning, e.g. setting keep-alive or `NoDelay`).
6. **`SslStreamSettings`** — TLS: certificate validation, SNI, client certificate.

### ClusterBuilder

- Fluent builder to compose cluster settings. Chains methods like `ConfigureCluster()`, `ConfigureConnectionPool()`, `ConfigureConnection()`, etc.
- Final call: `BuildCluster()` creates the cluster instance.

---

## 8. Misc / Cross-cutting utilities

**Files:** `Ensure.cs`, `EndPointHelper.cs`, `DnsClientWrapper.cs`, `Feature.cs`, `ExceptionMapper.cs`, `EnvironmentVariableProvider.cs`, and more.

- **`Ensure`** — precondition checks (NotNull, IsGreaterThanZero, etc.).
- **`DnsClientWrapper`** — async DNS resolver. The implementation uses the `DnsClient` NuGet package (not `System.Net.Dns`) so SRV/TXT queries work consistently across platforms. Both `IDnsResolver` and `DnsClientWrapper` are `internal`; there is no public seam for plugging in a custom resolver. The interface exists so in-assembly tests (via `InternalsVisibleTo`) can substitute a fake — production code always goes through `DnsClientWrapper` from `DnsMonitor`. Past versions of this file claimed users could substitute a custom `IDnsResolver`; do not re-introduce that claim.
- **`ExceptionMapper`** — classifies wire exceptions (network error, timeout, server error) into semantic types.
- **`Feature`** — checks server version for feature availability (e.g., "does this server support transactions?").
- **`EnvironmentVariableProvider`** — abstraction over `Environment.GetEnvironmentVariable` for testing.

---

## Common pitfalls & design notes

### 1. **Connection-pool starvation under load**

- If `MaxConnections` is too low and many threads call `CheckOut()` concurrently, the wait queue will block.
- Diagnostics: subscribe to the CMAP events in `Core/Events/` — e.g. `ConnectionPoolReadyEvent`, `ConnectionPoolClearedEvent`, and `ConnectionPoolCheckedOutConnectionEvent` (the last one carries the wait `Duration`) — to see wait times and pool state transitions.
- `MaxConnecting` limits concurrent TCP handshakes; tuning is workload-dependent.

### 2. **TLS / SNI / certificate validation**

- **SNI:** The driver sends the hostname in the TLS ClientHello (via `System.Net.Security.SslStream`). Servers must support SNI for multi-tenant deployments.
- **Certificate validation:** `MongoClientSettings.AllowInsecureTls` is a "trust anything" toggle — it bypasses **both** certificate-chain validation **and** hostname/SAN matching (testing only). Invalid certs otherwise cause `AuthenticationException`. The connection-string option `tlsInsecure=true` is equivalent to `AllowInsecureTls`; `tlsDisableCertificateRevocationCheck` is narrower (only disables CRL/OCSP).
- **Custom client certificates / CA validation:** configured via the public `MongoClientSettings.SslSettings.ClientCertificates` and `MongoClientSettings.SslSettings.ServerCertificateValidationCallback` (the public `MongoDB.Driver.SslSettings` type at the driver root, distinct from the internal `MongoDB.Driver.Core.Configuration.SslStreamSettings`; the latter is the form the Core layer passes down to `SslStreamFactory`). The C# driver does **not** parse the connection-string `tlsCertificateKeyFile` option — client certificates must be supplied programmatically via `MongoClientSettings.SslSettings.ClientCertificates`. The `SslStream` uses the system CA store unless overridden by a custom validation callback.

### 3. **Heartbeat misconfiguration causing flapping**

- Setting `HeartbeatIntervalMS` too low floods the server with heartbeats.
- Setting the heartbeat timeout too low causes spurious timeouts and server flapping. There is **no** `heartbeatTimeoutMS` connection-string option — the timeout is set programmatically via `ClusterBuilder.ConfigureServer` (`ServerSettings.HeartbeatTimeout`), not via the URL.
- Default is 10s interval; the heartbeat timeout (`ServerSettings.HeartbeatTimeout`) defaults to `Timeout.InfiniteTimeSpan`, with the underlying socket I/O bounded by `connectTimeoutMS`. Tuning should be conservative.

### 4. **Forking a process after MongoClient init**

- The driver caches server descriptions and connection pools. A fork inherits these caches.
- On the child process, all cached connections and file descriptors are stale.
- The C# driver does **not** implement explicit fork detection; topology errors on the next operation will eventually invalidate the affected server's pool, but the user can observe spurious failures and shared-FD weirdness in the meantime.
- **Best practice:** Never fork after creating a `MongoClient`. If you must, call `Dispose()` on the parent's client before forking, and create a new client in the child.

### 5. **DNS-SRV TTL and txn-record overrides**

- DNS-SRV results are not cached by the driver; the OS resolver caches them.
- `DnsMonitor` re-queries periodically; the rescan interval starts at 60 seconds and `DnsMonitor.ComputeRescanDelay` only *lengthens* the delay when the min TTL returned in the SRV response exceeds 60s — a sub-60s TTL is floored at 60s, never shorter (`DnsMonitor.cs`).
- TXT records can override compressors and auth source. The driver does not cache TXT records either.
- If you change DNS records, the next monitor run picks up the change.

### 6. **Compression incompatibilities**

- Both client and server must support the negotiated compressor.
- If the server advertises a compressor the client doesn't have (e.g., zstd), the connection falls back to no compression.
- Network errors during compression/decompression are fatal to the connection.

### 7. **isMaster vs hello (during driver/server transition)**

- The driver sends `hello` against servers that advertise `helloOk` in their initial reply, and falls back to `isMaster` (legacy `OP_QUERY`) otherwise.
- The `hello` response payload is identical to `isMaster` except for the command name.
- **Deprecated:** `isMaster` is the legacy command name (`hello` replaced it); the driver's `CommandUsingQueryMessageWireProtocol` (OP_QUERY) path exists only to talk to pre-3.6 servers. OP_QUERY can be removed once the supported-server floor passes 3.6 — gated by minimum-server-version policy, not by any server-side `isMaster` removal date.

### 8. **Streaming hello / awaitable heartbeat edge cases**

- **Negotiated via `helloOk`** in the connection-establishing hello reply.
- **Issue:** If the server goes down while waiting for a streamed response, the TCP connection hangs. The monitor has a heartbeat timeout to detect this.
- **Fallback:** If `awaitable: true` is not supported, the monitor falls back to polling.
- **Race condition:** If a server topology changes (primary to secondary) while a response is streaming, the monitor may receive a stale response. `TopologyVersion` comparison detects this.

### 9. **CSOT (Client-Side Operation Timeout) propagation**

- The driver threads an `OperationContext` (defined at `src/MongoDB.Driver/OperationContext.cs`, not under `Core/Misc/` despite being a Core-level concern) carrying both `CancellationToken` and a remaining-timeout deadline, from the public API down through bindings, server selection, connection checkout, and the wire protocol.
- Each layer must call `ThrowIfTimedOutOrCanceled` at re-entry points (e.g., before each retry attempt, before each pool wait). Skipping a check is a stall risk under deadline.
- When changing this layer, verify the `OperationContext` is propagated unchanged — wrapping or replacing it loses the deadline.

### 10. **Retry logic lives in Operations, not Core**

- This layer is responsible for discovering servers and routing commands.
- Retries (on transient errors, failover) are implemented in `Core/Operations/`. The wire protocol layer is oblivious to retries.
- **Consequence:** If a connection is dropped mid-operation, the operations layer decides whether to retry; the connection layer just reports the error.

---

## How to test (Core-level changes)

```bash
# SDAM
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Core.Clusters|FullyQualifiedName~Core.Servers"

# Wire protocol
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Core.WireProtocol"

# Connection pool (CMAP)
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~ConnectionPools"

# Compression
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Compression"
```

JSON-driven SDAM and CMAP spec runners under `tests/MongoDB.Driver.Tests/Specifications/server-discovery-and-monitoring/` and `…/connection-monitoring-and-pooling/` exercise the full state machines — see the spec-conformance AGENTS.md.

---

## Key files quick reference

| Responsibility | Files |
|---|---|
| Topology discovery & SDAM | `Clusters/Cluster.cs`, `Clusters/MultiServerCluster.cs`, `Clusters/ClusterDescription.cs`, `Servers/ServerMonitor.cs` |
| Server selection | `Clusters/ServerSelectors/*.cs` |
| DNS-SRV & load balancing | `Clusters/DnsMonitor.cs`, `Clusters/LoadBalancedCluster.cs` |
| Connection lifecycle | `Connections/BinaryConnection.cs`, `Connections/ConnectionInitializer.cs` |
| Wire messages | `WireProtocol/CommandUsingCommandMessageWireProtocol.cs`, `WireProtocol/Messages/*.cs` |
| Connection pooling | `ConnectionPools/ExclusiveConnectionPool.cs` |
| Compression | `Compression/ICompressor.cs`, `Compression/*Compressor.cs` |
| Settings & parsing | `Configuration/ConnectionString.cs`, `Configuration/ClusterBuilder.cs` |

