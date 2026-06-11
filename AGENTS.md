# AGENTS.md - CSharpDriver

## Overview
The C# driver for MongoDB.

## Tech Stack
- .NET library projects producing NuGet packages
- Multi-targeted to various .NET versions from .NET Framework 4.7.2 up
- xUnit + FluentAssertions for testing

## Project Structure
- `src/MongoDB.Bson/` - BSON for MongoDB
- `src/MongoDB.Driver/` - C# driver for MongoDB
- `src/MongoDB.Driver.Encryption/` - Client encryption (CSFLE with KMS).
- `src/MongoDB.Driver.Authentication.AWS/` - AWS IAM authentication
- `tests/MongoDB.Driver.Tests/` - Main C# driver tests
- `tests/MongoDB.Bson.Tests/` - BSON handling tests
- `tests/*/TestHelpers` - Common test utilities
- `tests/*` - Specialized tests; less common
- `tests/MongoDB.Driver.Tests/Specifications/` are JSON-driven tests using a common runner.

## Editing
- Be careful to preserve file BOMs.

## Versioning conventions

The driver follows **strict semantic versioning**: breaking changes to the public surface require a major-version bump. Breaks are always measured **against the latest released version of the assembly** (the most recent published NuGet package), **not** against the current state of `main`. A public API that was added and then changed or removed within the current unreleased development cycle never shipped, so it is not a break.

Releases are tagged `v<major>.<minor>.<patch>` (e.g. `v3.9.0`). Find the baseline with the GitHub release list, **not** local `git tag` (a clone's tags are frequently stale and miss recent releases):
- Absolute latest published version: `gh release list --limit 1 --json tagName,isLatest`.
- A specific major's latest line: the highest `v<major>.*` from `gh release list --limit 100 --json tagName`.

Diff the file at that release tag against the working tree to confirm a change is observable to an upgrading consumer (`git fetch --tags` first if the tag isn't local, then `git show <tag>:<path>`).

What counts as a break: public signature / default / visibility changes; an interface member added to any public interface (the driver multi-targets `net472`, which can't consume default interface methods, so this is a hard break); behavior changes on an unchanged public signature; behavior changes affecting serialized BSON shape (element name, representation, discriminator, Guid representation).

What does **not** count as a break:
- Changes to anything `internal` — regardless of `InternalsVisibleTo` (which exists only to expose internals to first-party test/benchmark assemblies and Moq's proxy stub). Internal code is never part of the public surface.
- Changes to the exception type thrown for an **unsupported** feature (a not-yet-implemented LINQ operator, an unsupported mapping, a guard rejecting something the driver doesn't support). Only the exception type of a supported, documented operation is part of the contract.

## Async conventions

- **Paired sync/async public surface.** Public methods that perform I/O come in pairs: a sync method (`Foo`) and an async counterpart (`FooAsync`) that accepts a `CancellationToken`. When adding, changing, or `[Obsolete]`-marking either side, do the same to the other. Operations under `src/MongoDB.Driver/Core/Operations/` duplicate this pairing internally (`Execute` / `ExecuteAsync`) — bug fixes must land on both paths; do not collapse them by calling `Task.Delay(...).GetAwaiter().GetResult()` or similar sync-over-async shortcuts.
- **`ConfigureAwait(false)`.** All library `await`s use `ConfigureAwait(false)`; the driver does not assume a synchronization context. Application code calling the driver may use whatever it likes.
- **Cancellation / CSOT propagation.** `CancellationToken` and `OperationContext` (the `CancellationToken` + deadline bundle threaded through `Core/Operations/` and the wire layer) must be passed through unchanged. Substituting a fresh `OperationContext` or passing `CancellationToken.None` mid-stack drops the caller's deadline — see the discussion in `src/MongoDB.Driver/Core/AGENTS.md` and `src/MongoDB.Driver/Core/Operations/AGENTS.md`.

## Commands
- Build: `dotnet build CSharpDriver.sln`
- Run all tests: `dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0`
- Run a single test class: `dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 --filter "FullyQualifiedName~ClassName"`

## Testing
- Tests cannot be run in parallel.
- A MongoDB connection is always available locally, so "integration" tests can be run as well as unit tests. Some test suites also require additional environment variables — if you need to run those tests and the variables are not set, stop and tell the user which variables are needed rather than working around it.

| Feature area | Required environment variables |
|---|---|
| Atlas Search | `ATLAS_SEARCH_TESTS_ENABLED`, `ATLAS_SEARCH_URI` |
| CSFLE / auto-encryption | `CRYPT_SHARED_LIB_PATH` |
| CSFLE with KMS mock servers | `KMS_MOCK_SERVERS_ENABLED` |
| CSFLE with AWS KMS | `CSFLE_AWS_TEMPORARY_CREDS_ENABLED` |
| CSFLE with Azure KMS | `CSFLE_AZURE_KMS_TESTS_ENABLED` |
| CSFLE with GCP KMS | `CSFLE_GCP_KMS_TESTS_ENABLED` |
| AWS authentication | `AWS_TESTS_ENABLED` |
| GSSAPI / Kerberos | `GSSAPI_TESTS_ENABLED`, `AUTH_HOST`, `AUTH_GSSAPI` |
| OIDC authentication | `OIDC_ENV` |
| X.509 authentication | `MONGO_X509_CLIENT_CERTIFICATE_PATH`, `MONGO_X509_CLIENT_CERTIFICATE_PASSWORD` |
| PLAIN authentication | `PLAIN_AUTH_TESTS_ENABLED` |
| SOCKS5 proxy | `SOCKS5_PROXY_SERVERS_ENABLED` |

## Commit and PR Conventions

- The first commit message and the PR message start with a JIRA number: `CSHARP-1234: Description`
- The branch name will usually match the JIRA number: `CSHARP-1234`

## Functional areas

Each area below has its own `AGENTS.md` (auto-loaded when working in that subtree) and a corresponding read-only reviewer subagent in `.claude/agents/`.

| Area | Location | Reviewer |
|---|---|---|
| BSON & Serialization | `src/MongoDB.Bson/AGENTS.md` | `bson-reviewer` |
| Connection & Transport (SDAM) | `src/MongoDB.Driver/Core/AGENTS.md` | `transport-reviewer` |
| Operations, Sessions & Transactions | `src/MongoDB.Driver/Core/Operations/AGENTS.md` | `operations-reviewer` |
| Authentication | `src/MongoDB.Driver/Authentication/AGENTS.md` (+ `src/MongoDB.Driver.Authentication.AWS/AGENTS.md`) | `auth-reviewer` |
| Public API: client facades & settings | `src/MongoDB.Driver/AGENTS.md` (router) | `client-api-reviewer` |
| Public API: CRUD builders DSL | `src/MongoDB.Driver/AGENTS.md` (router) | `builders-reviewer` |
| Aggregation & Change Streams | `src/MongoDB.Driver/AGENTS.md` (router) + `Core/Operations/AGENTS.md` | `aggregation-reviewer` |
| LINQ Provider | `src/MongoDB.Driver/Linq/AGENTS.md` | `linq-reviewer` |
| GridFS | `src/MongoDB.Driver/GridFS/AGENTS.md` | `gridfs-reviewer` |
| Atlas Search & Vector Search | `src/MongoDB.Driver/Search/AGENTS.md` | `search-reviewer` |
| Client-Side Encryption (CSFLE / QE) | `src/MongoDB.Driver.Encryption/AGENTS.md` (+ `src/MongoDB.Driver/Encryption/AGENTS.md`) | `encryption-reviewer` |
| Diagnostics: Events | `src/MongoDB.Driver/Core/Events/AGENTS.md` | `diagnostics-reviewer` |
| Diagnostics: Logging | `src/MongoDB.Driver/Core/Logging/AGENTS.md` | `diagnostics-reviewer` |
| GeoJSON Object Model | `src/MongoDB.Driver/GeoJsonObjectModel/AGENTS.md` | `geojson-reviewer` |
| Spec conformance & test infra | `tests/MongoDB.Driver.Tests/Specifications/AGENTS.md` (+ `tests/MongoDB.Driver.TestHelpers/AGENTS.md`) | `spec-conformance-reviewer` |

## Cross-cutting reviewers

These reviewers have no per-area `AGENTS.md`; they apply a single hygiene lens across the whole diff and run on every invocation of the `/review-areas` skill.

| Concern | Reviewer |
|---|---|
| Security: secrets, TLS/crypto, redaction, deserialization safety | `security-reviewer` |
| Public API / SemVer | `api-stability-reviewer` |
| Async/threading hygiene | `async-reviewer` |

## PR-summary reviewer (external PR mode only)

When `/review-areas` is invoked with a PR number, an additional reviewer runs alongside the others:

| Concern | Reviewer |
|---|---|
| Holistic "what does this PR do, and is it a good change?" — reads the PR body and the full diff | `pr-summary-reviewer` |
