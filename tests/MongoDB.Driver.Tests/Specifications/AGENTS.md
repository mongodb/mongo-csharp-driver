---
area: Spec conformance (cross-driver test suite)
scope: ["tests/MongoDB.Driver.Tests/Specifications/**/*"]
reviewer-agent: spec-conformance-reviewer
adjacent-areas: [tests/MongoDB.Driver.TestHelpers, every src area]
---

# Specifications — AGENTS.md

JSON-driven runners for the cross-driver test suite. The JSON test files mirror those at `https://github.com/mongodb/specifications` and are embedded as project resources; the C# code in this directory wires them up to xUnit.

## Two formats

- **Legacy v1** — older style. Three things to know:
  - **Base-class variance.** Some legacy runners extend `LoggableTestClass` (e.g. CRUD, InWindow, `connection-string/ConnectionStringTestRunner`); others are plain xUnit `[Theory]` classes with no shared base (e.g. `AuthTestRunner`, `ServerSelectionTestRunner`, `BsonCorpusTestRunner`, the `read-write-concern/ConnectionStringTestRunner`). Note the asymmetry across the two `ConnectionStringTestRunner` files: the one under `connection-string/` extends `LoggableTestClass`, the one under `read-write-concern/` does not. Don't assume a shared base — check the file. All of them pair with a custom `JsonDrivenTestCaseFactory` for resource discovery.
  - **`MongoClientJsonDrivenTestRunnerBase`** is a heavier shared base; in this repo only `ClientSideEncryptionTestRunner` derives from it. It adds resource matching, `runOn`/`minServerVersion`/`maxServerVersion` filtering, EventCapturer setup, FailPoint cleanup, lifecycle. Not used by other legacy runners.
  - **Path-prefix property naming is inconsistent across runners.** Most legacy runners override a singular `PathPrefix` property (e.g. `AuthTestRunner`, `BsonCorpusTestRunner`, `ClientSideEncryptionTestRunner`, the SDAM / CMAP runners), while a few override the plural `PathPrefixes` (e.g. `CrudTestRunner`, `server-selection/ServerSelectionTestRunner`, `server-selection/InWindowTestRunner`, `connection-string/ConnectionStringTestRunner`, the `client-side-encryption/prose-tests/ClientEncryptionProseTests`). Check the runner's existing override before adding a new resource namespace — guessing the wrong name yields zero tests discovered.
  - **Resource-suffix variance.** Spec-specific: CRUD uses `MongoDB.Driver.Tests.Specifications.crud.tests.v1` (with the `.v1` segment), while many other legacy runners use just `…tests` with no `.v1` segment.
- **Unified Test Format (UTF)** — newer; preferred for new specs. Discovered via `[UnifiedTestsTheory(...)]` methods on the `UnifiedTestSpecRunner` class (one class with many theory methods, not one class per spec). The `UnifiedTestsDiscoverer` reads UTF JSON files from embedded-resource namespaces and instantiates xUnit cases.
  - Resource-namespace gotcha: upstream spec resource paths use hyphens (`change-streams/`, `retryable-reads/`, `client-side-operations-timeout/`); not every hyphenated form has a matching on-disk folder under `Specifications/` (e.g. there is no `client-side-operations-timeout/` directory, the embedded-resource namespace exists nonetheless). Because C# resource names follow identifier rules, hyphens become underscores in the namespace — e.g. `change_streams.tests.unified`, `retryable_reads.tests.unified`, `client_side_operations_timeout.tests`. A handful also drop the `.unified` suffix entirely (`sessions.tests`, `versioned_api.tests`). Cross-check the `[UnifiedTestsTheory(...)]` argument against the actual embedded-resource list when you add a spec.

Many spec areas have **both** legacy and unified subdirectories during the migration window; new tests should land in unified.

## Layout

- `Runner/` — contains only `MongoClientJsonDrivenTestRunnerBase`. Handles schema validation, `runOn`/`minServerVersion`/`maxServerVersion` filtering, EventCapturer setup, FailPoint cleanup, lifecycle. Its resource-matching property is the singular `PathPrefix` (overridden by `ClientSideEncryptionTestRunner`).
- `tests/MongoDB.Driver.Tests/UnifiedTestOperations/` (sibling of this directory, **not** a subdirectory) — ~100+ operation classes implementing one of `IUnifiedTestOperation`, `IUnifiedEntityTestOperation`, `IUnifiedSpecialTestOperation`, or `IUnifiedOperationWithCreateAndRunOperationCallback` (e.g., `UnifiedAggregateOperation`, `UnifiedClientBulkWriteOperation`, `UnifiedAssertEventCountOperation`). New operations land there with matchers in the `Matchers/` subdirectory.
- One subdirectory per spec area. The on-disk list grows over time and is the authoritative source — `ls tests/MongoDB.Driver.Tests/Specifications/` is the only reliable enumeration. A few representative entries (not exhaustive): `crud/`, `server-discovery-and-monitoring/`, `connection-monitoring-and-pooling/`, `client-side-encryption/`, `change-streams/`, `auth/`, plus the prose-only folders `transactions/`, `sessions/`, `retryable-reads/`, `retryable-writes/` (called out separately in the next bullet).
  - **Prose-only C# folders.** For `transactions/`, `sessions/`, `retryable-reads/`, and `retryable-writes/`, the C# classes in those folders are prose-only — they hold only spec-prose tests written as ordinary xUnit, not a JSON runner class.
  - **Where their JSON cases live.** The JSON-driven UTF cases for the same specs sit in `tests/...` JSON files under the same folders and are consumed via their embedded-resource namespaces (`sessions.tests`, `transactions.tests.unified`, …) by `UnifiedTestSpecRunner` via `[UnifiedTestsTheory(...)]`, not by a per-folder runner class. There is no duplication — the same files back both the on-disk path and the embedded-resource lookup.

## Adding a new spec test

1. Pull the JSON test files from `mongodb/specifications`.
2. Drop the JSON files into the matching spec subdirectory:
   - **Location:** under the **repo-root `specifications/` tree** (`<repo-root>/specifications/<spec-name>/tests/...`), *not* under `tests/MongoDB.Driver.Tests/Specifications/`. The C# subdirectories here hold the runner classes (and any prose tests); the JSON test corpus is sibling to the C# tree at the repo root.
   - **How the project picks them up:** `MongoDB.Driver.Tests.csproj` contains a single repo-wide `<EmbeddedResource LinkBase="Specifications\" Include="..\..\specifications\**\*.json" />` glob. If your new files live under that repo-root tree, they are already included — no per-spec `<EmbeddedResource>` edits needed.
   - **`git add` is mandatory.** The `<EmbeddedResource>` glob is evaluated at build time against files on disk regardless of git state, so an unstaged file builds locally and silently disappears in CI (which checks out only what's tracked). Verify by running `dotnet build` and checking the test count.
   - **On-disk layout varies by spec.** Some files land at `tests/`, others at `tests/v1/` or `tests/unified/`, and a few use spec-specific subfolders. What matters is that the resulting embedded-resource namespace matches what the runner consumes.
3. For legacy specs: confirm the runner's `PathPrefix` / `PathPrefixes` override (whichever it uses — see the note in "Two formats" above) covers the new resource namespace.
4. For UTF specs: confirm the `[UnifiedTestsTheory(...)]` argument on the test method matches the resource namespace. If the spec uses an operation not yet implemented, add an `IUnifiedTestOperation` (or related interface) under `tests/MongoDB.Driver.Tests/UnifiedTestOperations/` **and** wire it into `UnifiedTestOperationFactory.CreateOperation` — that class is the centralized dispatcher (a nested `switch` over `targetEntityId` then `operationName`) that the unified runner consults via `UnifiedEntityMap`. An operation class with no entry in this factory is unreachable from JSON test cases.
5. Run locally; resolve skips by either fixing the implementation or adding a justified entry to the `__ignoredTests` / `__ignoredTestFiles` `HashSet<string>` statics on `UnifiedTestSpecRunner` itself (a single class hosts every UTF theory method, so the ignore lists are central, not per-spec).

## Skip / ignore patterns

- **Legacy** — JSON `skipReason`, `runOn`, `minServerVersion`, `maxServerVersion` honored by the runner.
- **UTF — discovery-time exclusion.** `UnifiedTestsDiscoverer` reads static `HashSet<string>` members on `UnifiedTestSpecRunner` (the single class hosting every UTF theory method) to exclude cases **before** xUnit instantiates them.
  - The defaults are `__ignoredTests` and `__ignoredTestFiles`.
  - Each `[UnifiedTestsTheory]` can override them via the `SkippedTestsProvider` / `SkippedFilesProvider` named arguments. The attribute is co-located with the discoverer under `tests/MongoDB.Driver.Tests/UnifiedTestOperations/`.
  - **Consequence:** a theory pointed at a different `HashSet<string>` static won't be filtered by the default `__ignoredTests`.
- **UTF — runtime skip.** `SkipNotSupportedTestCases` on `UnifiedTestSpecRunner` (`tests/MongoDB.Driver.Tests/Specifications/UnifiedTestSpecRunner.cs`) throws `SkipException` per case while the runner walks the JSON — the inline mechanism, separate from the discovery-time exclusion above.

## Common pitfalls

- **Resource path mismatch** — a forgotten `<EmbeddedResource>` glob or a wrong namespace prefix yields zero tests discovered. Always verify by checking the test count after adding.
- **Cross-test FailPoint leakage** — fail-points are server-global. If a test fails before its `FailPoint.Dispose()`, the server stays mis-configured for the next test. Always wrap fail-points in `using`.
- **Unsupported operations** masquerade as failures, not skips. If a JSON operation isn't yet wired in `UnifiedTestOperations/`, the test errors out with a confusing "operation X not found". Add the operation or the skip entry.
- **Server version drift** — the C# driver targets specific spec revisions. After a sync from upstream, expect breakage on operations the driver hasn't yet caught up to.
- **`runOn` constraints** can silently skip whole files when the local server is the wrong version/topology. Read the skip output rather than trusting "all green".
- **Tests cannot run in parallel** — root `AGENTS.md` rule. Spec tests share server state heavily; running in parallel corrupts results.

## How to run

```bash
# Whole suite (slow; full integration)
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Specifications"

# One spec area
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Specifications.crud"
```

Some areas require env vars — see the table in the root `AGENTS.md` (CSFLE, search, auth mechanisms, X.509, SOCKS5 …). If a required env var is missing, the runner skips silently. **If you need that area, stop and ask the user** rather than working around the skip.

## Boundaries

- **vs `tests/MongoDB.Driver.TestHelpers/`.** This area owns the spec-specific runners and operations; reusable fixtures, mocks, and common helpers live in TestHelpers.
- **vs each `src/` area.** A spec runner pulls in code from many functional areas; spec-conformance reviewer cross-cuts them all. When a spec test fails, the relevant area's reviewer often owns the fix — but the spec-conformance reviewer owns the runner and the JSON sync.
