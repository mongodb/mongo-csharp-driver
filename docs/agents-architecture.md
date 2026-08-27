# Per-Area `AGENTS.md` & Reviewer Sub-Agents — Architecture

This document describes how the repo is partitioned into functional areas for the purpose of agent-aware tooling (Claude Code and similar). Each area gets a focused `AGENTS.md` (with a sibling `CLAUDE.md` pointing at it) so that subdirectory auto-loading scopes context to what's relevant, plus one read-only reviewer sub-agent in `.claude/agents/`.

## Why this exists

The driver is a large multi-project repo with very different concerns layered together (BSON serialization, SDAM, LINQ translation, CSFLE, change streams, …). Before this layout, the only repo-specific guidance was the root `AGENTS.md`. That file is general-purpose, so an agent working in `Linq/Linq3Implementation/` got the same context as one debugging connection-pool starvation. The per-area files keep the root file lean while letting deep areas carry their own invariants, pitfalls, and review checklists.

The reviewer sub-agents encode area-specific review focus — what to flag, what counts as a SemVer/spec-conformance event, what tests to run.

## Convention

- Every area has both `AGENTS.md` (substantive content) **and** `CLAUDE.md` (a one-line `@AGENTS.md` pointer). This matches the existing repo convention at the root.
- Each `AGENTS.md` carries a YAML frontmatter block with `area`, `scope` (globs), `reviewer-agent`, `adjacent-areas`. Claude Code doesn't parse it, but it's useful for humans and tooling.
- Reviewer sub-agents live at `.claude/agents/<name>-reviewer.md`. They are read-only (`tools: Read, Grep, Glob, Bash`); they report findings, they do not patch.

## Functional areas (15 numbered areas + 2 routers)

| # | Area | `AGENTS.md` location | Sub-agent |
|---|---|---|---|
| 0 | Repo router (existing) | `AGENTS.md` (root) | — |
| R | Driver router | `src/MongoDB.Driver/AGENTS.md` | — (substantive sections inline for client facades, builders DSL, aggregation/change-stream fluent API; deeper subdirs have their own files) |
| 1 | BSON & Serialization | `src/MongoDB.Bson/AGENTS.md` | `bson-reviewer` |
| 2 | Connection & Transport (SDAM) | `src/MongoDB.Driver/Core/AGENTS.md` (covers Clusters, Servers, Connections, ConnectionPools, WireProtocol, Compression, Configuration, Misc) | `transport-reviewer` |
| 3 | Operations, Sessions & Transactions | `src/MongoDB.Driver/Core/Operations/AGENTS.md` + cross-ref `src/MongoDB.Driver/Core/Bindings/AGENTS.md` | `operations-reviewer` |
| 4 | Authentication | `src/MongoDB.Driver/Authentication/AGENTS.md` + cross-ref `src/MongoDB.Driver.Authentication.AWS/AGENTS.md` | `auth-reviewer` |
| 5 | Client facades & settings | section inside the driver router file | `client-api-reviewer` |
| 6 | CRUD Builders DSL | section inside the driver router file (no dedicated dir) | `builders-reviewer` |
| 7 | Aggregation & Change Streams | section inside the driver router file (cross-link from `Core/Operations/AGENTS.md`) | `aggregation-reviewer` |
| 8 | LINQ Provider | `src/MongoDB.Driver/Linq/AGENTS.md` | `linq-reviewer` |
| 9 | GridFS | `src/MongoDB.Driver/GridFS/AGENTS.md` | `gridfs-reviewer` |
| 10 | Atlas Search & Vector Search | `src/MongoDB.Driver/Search/AGENTS.md` | `search-reviewer` |
| 11 | CSFLE / Queryable Encryption | `src/MongoDB.Driver.Encryption/AGENTS.md` + `src/MongoDB.Driver/Encryption/AGENTS.md` (driver-side glue) | `encryption-reviewer` |
| 12 | Diagnostics: Events | `src/MongoDB.Driver/Core/Events/AGENTS.md` | `diagnostics-reviewer` |
| 13 | Diagnostics: Logging | `src/MongoDB.Driver/Core/Logging/AGENTS.md` | `diagnostics-reviewer` |
| 14 | GeoJSON Object Model | `src/MongoDB.Driver/GeoJsonObjectModel/AGENTS.md` | `geojson-reviewer` |
| 15 | Spec conformance & test infra | `tests/MongoDB.Driver.Tests/Specifications/AGENTS.md` + `tests/MongoDB.Driver.TestHelpers/AGENTS.md` | `spec-conformance-reviewer` |

## Cross-cutting reviewers (3)

These reviewers have no per-area `AGENTS.md` and no path-scoping. They apply a single hygiene lens across the **entire** diff on every `/review-areas` invocation, regardless of which files changed.

| Sub-agent | Concern |
|---|---|
| `security-reviewer` | Credential exposure, TLS/cert misconfiguration, crypto misuse, unsafe deserialization, KMS plumbing leaks, RNG weakness, missing log redaction |
| `api-stability-reviewer` | Public API surface / SemVer breaks — signatures, defaults, attributes, exception types, visibility, nullability, enum members, interface shape |
| `async-reviewer` | Async/threading hygiene — sync-over-async, missing `ConfigureAwait`, `async void`, lost `CancellationToken` propagation, locks held across awaits, unawaited tasks |

Cross-cutting reviewers always run alongside whatever area reviewers are dispatched. They complement the area reviewers rather than replacing them: area reviewers own correctness of the logic; cross-cutting reviewers own their specific hygiene lens across the whole diff.

## PR-summary reviewer (external PR mode only)

One additional reviewer runs only when `/review-areas` is invoked with a PR number (external PR mode). It does not run for local branch reviews because there is no PR body to read.

| Sub-agent | Concern |
|---|---|
| `pr-summary-reviewer` | Holistic "what does this PR do, and is it a good change?" — pulls the PR body via `gh pr view`, synthesizes across the whole diff, and gives an opinion (`looks good` / `mixed` / `concerns`). Distinct from the per-area and cross-cutting reviewers, which look at correctness of individual pieces; this one judges the PR as a whole. |

It runs in parallel with the area and cross-cutting reviewers, and its output is rendered at the top of the consolidated report so the reader sees the high-level take before the line-level findings.

## Boundary decisions worth knowing

- **`src/MongoDB.Driver/AGENTS.md` is a router**, not a deep doc. Keep it short and section-organized — substantive content there only for code that actually lives at the directory root (facades, builders, aggregation/change-stream fluent API). Deeper subdirectory `AGENTS.md` files always *layer* additional context on top; nothing is lost.
- **Sessions/transactions** sprawl across three locations (`ClientSession*.cs` at the driver root, `CoreSession*` and bindings under `Core/`, transaction operations under `Core/Operations/`). The `operations-reviewer` owns all of it. The `client-api-reviewer` owns only the surface-level `IClientSessionHandle` exposure on `IMongoClient`.
- **Aggregation** spans the root fluent files and `Core/Operations/Aggregate*`. A single `aggregation-reviewer` covers both layers. Boundary with `operations-reviewer`: aggregation owns pipeline shape and stage semantics; operations owns retry, binding, and cursor lifecycle.
- **Builders DSL** files live next to facade files at the driver root. Two reviewers (`client-api-reviewer`, `builders-reviewer`) share the same directory; each is keyed by file-pattern globs in its `description` rather than directory placement.

## File templates

### `AGENTS.md` skeleton (per area)

```
---
area: <human name>
scope: [<glob>, <glob>]
reviewer-agent: <kebab-name>
adjacent-areas: [<name>, <name>]
---

# <Area> — AGENTS.md

## Scope
<one paragraph: what's in, what's out>

## Key entry points
- `<Type or file>` — <one-line role>

## Architecture notes
<call flow, key invariants, threading/async model, lifecycle>

## Boundaries with adjacent areas
- vs <area>: <where the line is, who owns the shared type>

## Common pitfalls
<2-6 bullets of recurring mistakes specific to this area>

## How to test
<filter expressions, env vars from root AGENTS.md table, spec test paths>

## Spec links
<paths under specifications/ when applicable>
```

### Sub-agent skeleton (`.claude/agents/<name>.md`)

```
---
name: <kebab-name>-reviewer
description: Reviews changes to <area>. Use proactively when modifying <glob list>. Boundary with <adjacent>: <one line>.
tools: Read, Grep, Glob, Bash
model: inherit
---

You are the <Area> reviewer for the MongoDB C# driver.

## Authoritative context
Read `<path-to-area-AGENTS.md>` first; then root `AGENTS.md` for build/test commands.

## Review focus
- <invariant 1>
- <invariant 2>
- <SemVer / wire protocol / spec compliance concern>

## Required checks before approving
1. <run filter…>
2. <verify spec tests under specifications/…>
3. <ABI / public-API check when applicable>

## Escalate to user (do not auto-approve) when
- <wire-protocol change, public API change, spec deviation, etc.>
```

All reviewers get `Read, Grep, Glob, Bash` (read-only review with the ability to spot-run a `dotnet test --filter`). None get `Edit/Write` — reviewers report, never patch.

## Verification

If you change the layout — add an area, move a file, rename a reviewer — re-run these checks:

1. **Auto-load chain.** Open a file in three representative areas (e.g. `src/MongoDB.Driver/Linq/Linq3Implementation/Translators/SomeTranslator.cs`, `src/MongoDB.Driver/Core/ConnectionPools/ConnectionPool.cs`, `src/MongoDB.Bson/Serialization/BsonClassMap.cs`). In a fresh agent session, confirm the root `AGENTS.md`, the driver router, and the matching area `AGENTS.md` all surface in context — and that *unrelated* area files (e.g. GridFS) do not.
2. **Convention compliance.** `find . -name CLAUDE.md` — every file is one line and starts with `@AGENTS.md`. `find . -name AGENTS.md` — every per-area file has the YAML frontmatter block (the existing root one stays as-is) and the standard section headers.
3. **Sub-agent dispatch.** Open a PR or staged diff that touches one area and confirm the matching reviewer is the obvious match by description + globs.
4. **Test-filter sanity.** From each area's "How to test" section, copy one `dotnet test … --filter` command and run it. It should pass on a clean main checkout.
5. **No build breakage.** `dotnet build CSharpDriver.sln` succeeds — these are documentation-only changes.

## Adding a new area

1. Pick a natural directory root for the area (or a glob set if it doesn't fit a directory).
2. Drop `AGENTS.md` (with frontmatter and the standard sections) and a sibling `CLAUDE.md` containing `@AGENTS.md`.
3. Add a reviewer file at `.claude/agents/<name>-reviewer.md` following the skeleton above.
4. Update the **Functional areas** table in the root `AGENTS.md` and the **Functional areas** table above.
5. Add a path-pattern row for the new reviewer in the **Step 2** table inside `.claude/commands/review-areas.md` so the `/review-areas` skill dispatches to it automatically.
6. Run the verification checks.
