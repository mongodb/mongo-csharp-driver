---
area: Test infrastructure (driver helpers)
scope: ["tests/MongoDB.Driver.TestHelpers/**/*.cs"]
reviewer-agent: spec-conformance-reviewer
adjacent-areas: [tests/MongoDB.Driver.Tests/Specifications, tests/MongoDB.Bson.TestHelpers, tests/MongoDB.TestHelpers]
---

# MongoDB.Driver.TestHelpers — AGENTS.md

Shared test infrastructure for the driver test projects. Hosts integration-test base classes, fixtures, mock implementations, fail-point helpers, event capture, and xUnit extensions. Sibling helper projects: `tests/MongoDB.Bson.TestHelpers/` (BSON-specific) and `tests/MongoDB.TestHelpers/` (cross-cutting xUnit).

## Core fixtures and bases

- **`IntegrationTest<TFixture>`** — base for tests that require a live MongoDB. The constructor's signature is `IntegrationTest(TFixture fixture, Action<RequireServer> requireServerCheck = null)`: if the test passes a callback, the constructor calls `RequireServer.Check()` and invokes the callback **with** the resulting `RequireServer` instance, so the callback can chain fluent skip assertions on it (e.g. `requireServer.VersionGreaterThanOrEqualTo("7.0").Authentication(true)`). If no callback is passed, no skip check is performed by the base. **The `BeforeTestCase()` call is unconditional** — gated only by whether the test passes a fixture instance, not by whether the `requireServerCheck` callback is supplied; the callback only gates the optional skip check.
- **`MongoDatabaseFixture`** — lazy-creates a per-class temporary database (`CSTests<timestamp>`); tracks created collections and drops them on `Dispose`. `BeforeTestCase()` is the orchestrator entry point that `IntegrationTest` calls per method; it in turn invokes the overridable hooks `InitializeFixture()` (once per class, before the first test) and `InitializeTestCase()` (per method). When extending the fixture, override the hooks, not `BeforeTestCase()`.
- **`MongoCollectionFixture<TDocument, TInitial>`** — extends `MongoDatabaseFixture` with collection management; optionally re-seeds data before each test (`InitializeDataBeforeEachTestCase`).

## Configuration

- **`DriverTestConfiguration`** — static hub for `IMongoClient`, connection string, default namespace. `CreateMongoClient(settings => …)` lets tests customize. Multi-shard / multi-mongos clients are first-class.
- **`CoreTestConfiguration`** — companion exposing cluster metadata: server version, wire version, topology type, authentication mode. Used by `RequireServer` to gate tests by capability.

## Driver-event capture and fail-points

- **`EventCapturer`** — implements `IEventSubscriber` and records `CommandStartedEvent`, `CommandSucceededEvent`, etc. The public constructor takes an `IEventFormatter` (not another `IEventSubscriber` — the underlying `ReflectionEventSubscriber` is built internally) and the capturer routes events through it. Filters by event type and predicate; thread-safe queue. The standard pattern for asserting "what commands did the driver actually send".
- **`FailPoint`** — wraps `configureFailPoint` admin commands. Always use `using (FailPoint.Configure(...)) { … }` so `Dispose()` clears the fail-point. A leaked fail-point poisons subsequent tests.
- **`MockConnection`, `MockClusterableServerFactory`** (under `Core/`) — for unit tests that don't need a real server (wire-protocol-level expectations, retry logic, server-selection algorithm).
- **`EnvironmentVariableProviderMock`** — overrides env-var lookup in code under test; used with code that reads `OIDC_ENV`, `MONGODB_URI`, etc.

## JSON-driven helpers

Under `Core/JsonDrivenTests/`: a small set of event asserters and field-setter helpers (`CommandStartedEventAsserter`, `EventAsserterFactory`, `RecursiveFieldSetter`) used by the spec runners in `tests/MongoDB.Driver.Tests/Specifications/`. The broader JSON-driven test base classes (test factories, document/value comparers, JSON resource loading) live in the sibling project at `tests/MongoDB.Bson.TestHelpers/JsonDrivenTests/`, not here.

## xUnit extensions

This project's `Core/XunitExtensions/` carries `RequireServer.cs` and `RequirePlatform.cs`. Generic xUnit machinery (environment gating, the timeout-enforcing test framework) lives in the sibling `tests/MongoDB.TestHelpers/` project.

- **`RequireServer`** (here) — fluent skip predicates: `.ClusterType(...)`, `.VersionGreaterThanOrEqualTo("X.Y.Z")`, `.Authentication(true|false)`, `.RunOn(BsonArray requirements)`. Throws `SkipException` if not met.
- **`[RequirePlatform]`** (here) — skip on unsupported OSes.
- **`RequireEnvironment`** (sibling project, `tests/MongoDB.TestHelpers/XunitExtensions/`) — skip if env vars are missing (`.EnvironmentVariable("OIDC_ENV")`, `.OperatingSystem(...)`).
- **`TimeoutEnforcingXunitTestFramework`** (sibling project, `tests/MongoDB.TestHelpers/XunitExtensions/TimeoutEnforcing/`) — registered as the test framework via `XunitExtensionsConstants.TimeoutEnforcingXunitFramework` to enforce a wall-clock timeout (avoids hangs in CI). The runner classes (`TimeoutEnforcingTestRunner`, etc.) live alongside it.
- **`[Category("Integration")]`** — xUnit trait for organizing tests.
- **`X509CertificateLoader`** (top-level of this project) — a `#if !NET8_0_OR_GREATER` compat shim that polyfills the BCL's `System.Security.Cryptography.X509Certificates.X509CertificateLoader`. Test code can write `X509CertificateLoader.LoadCertificateFromFile(...)` / `LoadPkcs12FromFile(...)` uniformly across TFMs and let the compiler pick the BCL type on modern frameworks and this shim below. Don't reach for the deprecated `new X509Certificate2(string)` constructors in new code.

## Sibling helper projects

- `tests/MongoDB.Bson.TestHelpers/` — BSON-specific assertions, `BsonDocument`/`BsonArray` comparers, JSON-driven test base classes used by BSON spec runners.
- `tests/MongoDB.TestHelpers/` — cross-cutting xUnit extensions, value-attributes (parameterized tests), category traits.

## Common pitfalls

- **Tests cannot run in parallel.** Per root `AGENTS.md`. The fixture model assumes single-threaded execution within a class; collection state is shared.
- **Leaked fail-points.** Always `using`. If a test asserts mid-fail-point and the assertion throws, `Dispose` runs.
- **Silent skips.** `RequireServer` and `RequireEnvironment` throw `SkipException`, surfacing as "skipped" not "failed". Read the test output, don't trust a green status.
- **Fixture lifecycle confusion.** `MongoDatabaseFixture` is per-class (xUnit `IClassFixture`). `InitializeTestCase` runs before each method, but collections accumulate across the class run and are dropped only at class teardown (`Dispose`), not between individual tests. Per-method DB creation is **not** the model — share the DB.
- **Embedded JSON resource paths.** New spec-test JSON files dropped under the repo-root `specifications/` tree are already picked up by the single repo-wide `<EmbeddedResource>` glob in `MongoDB.Driver.Tests.csproj` (see the Specifications-area `AGENTS.md` for the glob); the failure modes are wrong on-disk location (under `tests/MongoDB.Driver.Tests/Specifications/` instead of repo-root `specifications/`) or a `PathPrefix` / `PathPrefixes` mismatch on the legacy runner. Both yield zero tests discovered, silently.
- **Per-mongos behavior.** Some tests must target a specific mongos (sharded). `DriverTestConfiguration` exposes per-mongos clients; reach for them when behavior depends on transaction routing.

## How to test the helpers

These are libraries — no tests of their own to run. Validate via the test projects that consume them:

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0
dotnet test tests/MongoDB.Bson.Tests/MongoDB.Bson.Tests.csproj -f net10.0
```

When changing helpers in a way that affects skip behavior, scan the test logs for unexpected skips before declaring done.
