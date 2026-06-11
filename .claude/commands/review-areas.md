---
description: Fan-out review of the current branch, an external PR, or a branch in another clone — runs each per-area reviewer over the files it owns and aggregates findings. With --iterate, alternates review/fix passes until two consecutive clean reviews or the iteration cap is reached. Requires the superpowers plugin (errors out if it is not available).
argument-hint: "[--all] [--model opus|sonnet|haiku] [--iterate [--max-iterations N]] [<PR#> | <clone-path> [<base-ref> [<head-ref>]] | <base-ref> [<head-ref>]]"
allowed-tools: Bash, Read, Glob, Grep, Write, Agent, Skill
---

# /review-areas

Run the per-area reviewer sub-agents (`.claude/agents/*-reviewer.md`) over a diff range in parallel and produce one consolidated report. Three modes:

- **Local range mode** — diff the current repo's branch.
- **External PR mode** — fetch a GitHub PR and diff against its base.
- **External clone mode** — diff a branch in a *different* clone of this repo, while running with the current clone's reviewer briefs and `AGENTS.md` files. Useful when the branch being reviewed lacks the latest agent/architecture updates that live in the current clone.

User args: `$ARGUMENTS`

## Step 0 — Require the superpowers plugin (hard gate)

This skill depends on the **superpowers** plugin and will not run without it. Before doing anything else:

1. Check the session's available-skills list (the `<system-reminder>` skill listing) for entries whose names start with `superpowers:`. At minimum, confirm all of these are present: `superpowers:requesting-code-review`, `superpowers:receiving-code-review`, `superpowers:test-driven-development`, `superpowers:systematic-debugging`, `superpowers:verification-before-completion`, and `superpowers:brainstorming`.
2. If **any** of those are missing, **stop immediately**. Do not parse args, diff, or dispatch reviewers. Emit exactly one error message to the user naming which superpowers skills were not found and saying that `/review-areas` requires the superpowers plugin to be installed and enabled. Then end the response.
3. If all are present, invoke `superpowers:requesting-code-review` now to frame the entire run — this review *is* the code-review request it describes — then continue to Step 1. The superpowers skills are woven into the workflow at the points called out below (fixer pass, convergence gate); honor them there.

## Step 1 — Determine scope

Parse `$ARGUMENTS`:
- If `--all` is present, queue every reviewer in the tables below regardless of diff. Skip step 2's filtering.
- If `--model <name>` is present, capture `<name>` (must be one of `opus`, `sonnet`, `haiku`) and pass it on every `Agent` dispatch in step 3. If absent, omit the `model` parameter so each reviewer falls back to its frontmatter setting (currently `inherit` for all of them, which means the parent session's model).
- If `--iterate` is present, enable the iteration loop described in **Step 5**. It is only valid in **local range mode** and **external clone mode**; if combined with external PR mode (a `<PR#>` token), stop immediately and tell the user that `--iterate` cannot be used with external PRs (we don't push fixes back to PR branches). If `--max-iterations <N>` is also present, capture `<N>` as the cap (must be a positive integer, ≤ 26 because file letters run `a`–`z`); default to **10** if absent. `--max-iterations` without `--iterate` is an error — tell the user and stop.
- Examine the remaining non-flag tokens in priority order: first try external PR mode, then external clone mode, then local range mode.

  **External PR mode** — if the first non-flag token looks like a PR number (a bare integer such as `1234`, or a `#`-prefixed integer such as `#1234`), treat this as an external PR review:
  1. Run `gh pr view <PR#> --json number,title,url,baseRefName,headRefOid` and capture the JSON.
  2. Parse `<owner>/<repo>` from the PR URL (e.g. `https://github.com/mongodb/mongo-csharp-driver/pull/1991` → `mongodb/mongo-csharp-driver`).
  3. Run `git remote -v` and find the remote whose fetch URL contains `<owner>/<repo>` (case-insensitive; works for both HTTPS and SSH URLs). If multiple remotes match, pick the first. If none match, fall back to trying `upstream` then `origin`.
  4. Run `git fetch <remote> refs/pull/<PR#>/head` to bring the PR's head commit into `FETCH_HEAD`.
  5. Capture the head SHA: `git rev-parse FETCH_HEAD`.
  6. Ensure the base branch is locally available: `git fetch <remote> <baseRefName>` (safe even if already present). The tracking ref will be `<remote>/<baseRefName>`.
  7. Set **base ref** = `<remote>/<baseRefName>` and **head ref** = the SHA from step 5.
  8. Set **diff-repo** = the absolute path of the current repo (`git rev-parse --show-toplevel`).
  9. Record the PR metadata (number, title, URL) and the remote name for display in the step 4 report header.

  **External clone mode** — otherwise, if the first non-flag token resolves to an existing directory (`test -d "<token>"`), treat it as the path to another clone of this repo and review the branch checked out there:
  1. Capture an absolute path: `<clone> = $(cd "<token>" && pwd)` (or `realpath`). All subsequent git commands and file paths must use this absolute form.
  2. Confirm it's a git repo by running `git -C "<clone>" rev-parse --show-toplevel`. If that fails, stop and tell the user that `<clone>` is not a git checkout.
  3. Capture a head label for display and filename use: `git -C "<clone>" rev-parse --abbrev-ref HEAD`. If it returns the literal `HEAD` (detached), fall back to `git -C "<clone>" rev-parse --short HEAD`.
  4. Collect the remaining non-flag tokens (after the path) in order:
     - First → **base ref** (default: `main`)
     - Second → **head ref** (default: `HEAD`)
  5. Set **diff-repo** = `<clone>`. Every git command in the rest of this skill must be run with `git -C "<diff-repo>" …`, and every file path passed to reviewers must be absolute (`<diff-repo>/<repo-relative-path>`). The parent agent's own working directory stays in the current clone so that `AGENTS.md` files and reviewer briefs continue to load from there — that's the point of this mode.

  **Local range mode** — otherwise, the remaining non-flag tokens are refs in the current repo. Collect them in order:
  - First token → **base ref** (default: `main`)
  - Second token → **head ref** (default: `HEAD`)
  - Set **diff-repo** = the absolute path of the current repo (`git rev-parse --show-toplevel`).

Use `<base>...<head>` as the diff range throughout (three-dot syntax finds the merge base). Examples:
- `/review-areas` → `main...HEAD` in the current repo
- `/review-areas 1234` → external PR #1234 (fetches and diffs against its base branch)
- `/review-areas #1234` → same as above
- `/review-areas HEAD~3` → `HEAD~3...HEAD` in the current repo (last 3 commits only)
- `/review-areas abc123 def456` → `abc123...def456` in the current repo (arbitrary commit range)
- `/review-areas --all HEAD~5 HEAD~2` → all reviewers, commits HEAD~5 through HEAD~2 in the current repo
- `/review-areas ~/code/csharp-driver-pr` → review the current branch of the clone at `~/code/csharp-driver-pr` against its `main`
- `/review-areas ~/code/csharp-driver-pr release/3.0` → review that clone's `HEAD` against its `release/3.0`
- `/review-areas ~/code/csharp-driver-pr origin/main feature-x` → review `feature-x` in that clone against `origin/main`
- `/review-areas --iterate ~/code/csharp-driver-pr` → review the clone's branch, then loop review → fix → review until two consecutive clean passes (or 10 iterations)
- `/review-areas --iterate --max-iterations 5 ~/code/csharp-driver-pr` → same but cap at 5 iterations
- `/review-areas --iterate` → iterate on the current repo's branch against `main`

If `--all` was not passed, run `git -C "<diff-repo>" diff --name-only <base>...<head>`. If the result is empty, stop and tell the user the range has no changes — do not dispatch reviewers. In iterate mode this end-of-range check is performed at the **start of every iteration**; a mid-loop empty range means the latest fixer commit reverted all changes (unusual — flag it to the user and stop).

## Step 2 — Map changed files → reviewers

Match each changed file against the table. A file may match more than one reviewer; queue all matches. Track files that match nothing — they go in an "Unmapped changes" section of the final report so coverage gaps are visible.

| Reviewer | Path patterns |
|---|---|
| `bson-reviewer` | `src/MongoDB.Bson/**`; `tests/MongoDB.Bson.Tests/**`; any file elsewhere implementing `IBsonSerializer<T>` (grep for it if you suspect one) |
| `transport-reviewer` | `src/MongoDB.Driver/Core/{Clusters,Servers,Connections,ConnectionPools,WireProtocol,Compression,Configuration,Misc}/**`; `tests/MongoDB.Driver.Tests/Core/{Clusters,Servers,Connections,ConnectionPools,WireProtocol,Compression,Configuration,Misc}/**` |
| `operations-reviewer` | `src/MongoDB.Driver/Core/Operations/**`; `src/MongoDB.Driver/Core/Bindings/**` (incl. `CoreSession*.cs`, `CoreTransaction*.cs`); `src/MongoDB.Driver/ClientSession*.cs`; `tests/MongoDB.Driver.Tests/Core/Operations/**`; `tests/MongoDB.Driver.Tests/Core/Bindings/**` |
| `auth-reviewer` | `src/MongoDB.Driver/Authentication/**`; `src/MongoDB.Driver.Authentication.AWS/**`; `src/MongoDB.Driver/MongoCredential.cs`; `src/MongoDB.Driver/Core/Connections/ConnectionInitializer.cs`; `tests/MongoDB.Driver.Tests/Authentication/**`; `tests/MongoDB.Driver.Tests/Communication/Security/**` |
| `client-api-reviewer` | `src/MongoDB.Driver/{MongoClient,MongoDatabase,MongoClientSettings,MongoCredential,IMongoClient,IMongoDatabase,IMongoCollection}.cs`; `src/MongoDB.Driver/MongoCollectionImpl*.cs`; `src/MongoDB.Driver/MongoUrl*.cs`; `src/MongoDB.Driver/ServerApi*.cs`; `tests/MongoDB.Driver.Tests/MongoClient*.cs`; `tests/MongoDB.Driver.Tests/MongoDatabase*.cs`; `tests/MongoDB.Driver.Tests/MongoCollection*.cs`; `tests/MongoDB.Driver.Tests/IMongoClient*.cs`; `tests/MongoDB.Driver.Tests/IMongoDatabase*.cs`; `tests/MongoDB.Driver.Tests/IMongoCollection*.cs`; `tests/MongoDB.Driver.Tests/MongoUrl*.cs`; `tests/MongoDB.Driver.Tests/MongoCredential*.cs`; `tests/MongoDB.Driver.Tests/MongoIndex*.cs` |
| `builders-reviewer` | `src/MongoDB.Driver/Builders.cs`; `src/MongoDB.Driver/{FilterDefinition,UpdateDefinition,ProjectionDefinition,SortDefinition,IndexKeysDefinition,ArrayFilterDefinition,PipelineDefinition,PipelineStageDefinition,FieldDefinition,SetFieldDefinitions}*.cs`; `tests/MongoDB.Driver.Tests/{FilterDefinition,UpdateDefinition,ProjectionDefinition,SortDefinition,IndexKeysDefinition,PipelineDefinition,PipelineStageDefinition,PipelineUpdateDefinition,RenderDollarForm,FindFluent,IFindFluent}*.cs` |
| `aggregation-reviewer` | `src/MongoDB.Driver/{Aggregate,AggregateFluent,IAggregateFluent,ChangeStream,AggregateHelper,ChangeStreamHelper,AggregateExpressionDefinition}*.cs`; `src/MongoDB.Driver/Core/Operations/{AggregateOperation,AggregateToCollectionOperation,ChangeStreamOperation,ChangeStreamCursor}*.cs`; `tests/MongoDB.Driver.Tests/{Aggregate,AggregateFluent,IAggregateFluentExtensions,ChangeStream,NoPipelineInput}*.cs` |
| `linq-reviewer` | `src/MongoDB.Driver/Linq/**`; `tests/MongoDB.Driver.Tests/Linq/**` |
| `gridfs-reviewer` | `src/MongoDB.Driver/GridFS/**`; `tests/MongoDB.Driver.Tests/GridFS/**` |
| `search-reviewer` | `src/MongoDB.Driver/Search/**`; `src/MongoDB.Driver/{QueryVector,VectorSearchOptions,BinaryVectorExtensions,SearchIndexType,CreateSearchIndexModel,CreateVectorSearchIndexModel*,CreateAutoEmbeddingVectorSearchIndexModel}*.cs`; `tests/MongoDB.Driver.Tests/Search/**`; `tests/MongoDB.Driver.Tests/{QueryVector,Rerank}*.cs` |
| `encryption-reviewer` | `src/MongoDB.Driver.Encryption/**`; `src/MongoDB.Driver/Encryption/**`; `tests/MongoDB.Driver.Encryption.Tests/**`; `tests/MongoDB.Driver.Tests/Encryption/**` |
| `diagnostics-reviewer` | `src/MongoDB.Driver/Core/Events/**`; `src/MongoDB.Driver/Core/Logging/**`; `tests/MongoDB.Driver.Tests/Core/Events/**`; `tests/MongoDB.Driver.Tests/Core/Logging/**` |
| `geojson-reviewer` | `src/MongoDB.Driver/GeoJsonObjectModel/**`; `tests/MongoDB.Driver.Tests/GeoJsonObjectModel/**` |
| `spec-conformance-reviewer` | `tests/MongoDB.Driver.Tests/Specifications/**`; `tests/MongoDB.Driver.TestHelpers/**`; `tests/MongoDB.Bson.TestHelpers/**`; `tests/MongoDB.TestHelpers/**` |

**Meta-mapping for reviewer definitions:** a change to `.claude/agents/<name>-reviewer.md` is reviewed by that same reviewer (e.g. `.claude/agents/bson-reviewer.md` → `bson-reviewer`, `.claude/agents/security-reviewer.md` → `security-reviewer`). The reviewer is best placed to judge whether its own brief still accurately characterizes the area. Cross-cutting reviewer definitions map to themselves the same way. Exception: `pr-summary-reviewer` only runs in external PR mode, so a local-range or external-clone-mode change to `.claude/agents/pr-summary-reviewer.md` won't be self-reviewed — that file's review happens when the PR is run through `/review-areas <PR#>`.

If new area reviewers are added under `.claude/agents/`, update this table.

## Cross-cutting reviewers (always run)

These three reviewers run on every invocation of `/review-areas`, regardless of which files changed. They look across the whole diff for one specific concern. They are *additional to* — not part of — the path-mapping table above.

| Reviewer | Concern |
|---|---|
| `security-reviewer` | secrets, TLS/crypto, redaction, deserialization safety |
| `api-stability-reviewer` | public surface / SemVer breaks |
| `async-reviewer` | async/threading hygiene |

## PR-summary reviewer (external PR mode only)

In external PR mode, also dispatch the `pr-summary-reviewer` agent. It produces a holistic description of the PR (what it does, why) plus an opinion on whether it's a good change. It runs in parallel with everything else, and its output goes at the top of the consolidated report (before `## Summary`). Skip it in local range mode and external clone mode — there is no PR body to read.

## Step 3 — Dispatch reviewers in parallel

**Critical**: emit a single assistant message containing one `Agent` tool-use block per dispatched reviewer — the matched area reviewers from step 2, all three cross-cutting reviewers, *and* (in external PR mode only) `pr-summary-reviewer`. Multiple `Agent` calls in the same message run concurrently; sequential calls do not. Use `subagent_type: <reviewer-name>` for each. If `--model <name>` was parsed in step 1, set `model: <name>` on every block; otherwise omit the field.

In every template below, substitute:
- `<base>`, `<head>` — the diff range refs.
- `<diff-repo>` — the absolute path captured in step 1 (the current repo for local-range / external-PR mode; the other clone for external-clone mode).
- File lists — always passed as **absolute paths** of the form `<diff-repo>/<repo-relative-path>` so reviewer `Read` calls land on the right tree regardless of mode. Don't pass bare repo-relative paths.

### Area-reviewer prompt template

For each area reviewer, use this template (substitute `<base>`, `<head>`, `<diff-repo>`, and the per-reviewer file list as absolute paths):

```
You are running as part of a multi-area branch review. The diff range is <base>...<head> in the repo at <diff-repo>.

Files in scope for this iteration that fall in your area (absolute paths):
- <abs-file1>
- <abs-file2>
…

[Iteration N > 1 only — omit this block on iteration 1]
This is iteration <N> of an `--iterate` run. The file list above is narrowed to files the previous fixer commit touched plus files that still carry an unresolved [blocking]/[substantive] finding. The following findings were tagged [fix-in-code][nit] in a previous iteration and have not been fixed; do NOT re-emit them unless the surrounding code has changed materially since the previous iteration:
- <reviewer>: <file>:<line> — <message>
- <reviewer>: <file>:<line> — <message>
…
[end Iteration N > 1 block]

Read those files at their current state. Run git commands with `git -C "<diff-repo>" …` (e.g. `git -C "<diff-repo>" diff <base>...<head> -- <repo-relative-path>`) — the parent agent's working directory may not be <diff-repo>. Pull in adjacent context only as needed to judge the change.

**Verify every functional finding by running code before you report it (required).** A *functional* finding is any claim about runtime behavior: a thrown or uncaught exception, wrong LINQ→pipeline translation or query result, wrong serialized/persisted BSON shape, lost `CancellationToken` / `OperationContext` (CSOT) propagation, an SDAM/retry/connection-pool behavior change, a redaction that doesn't fire, etc. — as opposed to a naming / comment / doc / style nit or a purely source-level signature observation. Too many reported findings turn out not to reproduce, so you **must** reproduce a functional issue and confirm it is real before reporting it; if your repro does not reproduce the problem, do not report it.

**You can and should run tests on this machine to verify your assertions** — do not report a functional finding you have not exercised. A local MongoDB is always available here: the test harness connects to the `MONGODB_URI` connection string when it is set, and otherwise defaults to `mongodb://localhost` (a local `mongod` is running in this environment), so `dotnet test` runs end-to-end with no manual setup. You have `Read`/`Grep`/`Glob`/`Bash` but **no `Edit`/`Write` tool**, so scaffold the repro with `Bash` — e.g. write a temporary xUnit test file into the matching `<diff-repo>/tests/<TestProject>/<Area>/` folder with a here-doc, or stand up a small throwaway console project in a temp directory. Run everything against `<diff-repo>` (the parent's working directory may not be `<diff-repo>` — in external-clone mode it isn't), so use absolute `<diff-repo>/...` paths. Reproduce a finding by either:
- running the temporary test against the matching project, e.g. `dotnet test "<diff-repo>/tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj" -f net10.0 --filter "FullyQualifiedName~<YourTest>"` (use `MongoDB.Bson.Tests` for BSON-area findings, the matching test project otherwise), or
- running a small throwaway repro (`dotnet run`) that exercises the path.

The driver's test suite is **not** designed to run in parallel and reviewers run concurrently, all against the same local MongoDB. Scope every repro to the tightest `--filter` you can, prefer unique collection names, and if a result looks contaminated by a concurrent run, re-run it before trusting it.

**Clean up after yourself.** Delete any file you created to reproduce, so the diff-repo working tree is left exactly as you found it (in `--iterate` mode the parent diffs the tree between iterations — a stray test file would corrupt the next pass). Verifying a finding never means committing a test; the *fixer* adds the permanent regression test later.

Atlas Search / Vector Search tests need an Atlas cluster — but **if `ATLAS_SEARCH_TESTS_ENABLED` and `ATLAS_SEARCH_URI` are both set in this environment, you can and should run them too** (the search/vector suites read those two vars), so verify the finding rather than deferring it. Only when those vars are unset do Atlas findings stay `[external-action]`. The findings that stay `[external-action]` for lack of verification are those that genuinely cannot run here: Atlas tests when `ATLAS_SEARCH_*` is unset, CSFLE / QE paths needing `CRYPT_SHARED_LIB_PATH` or KMS env vars (`KMS_MOCK_SERVERS_ENABLED`, `CSFLE_*`), or an external auth mechanism (AWS / GSSAPI / OIDC / X.509 / PLAIN / SOCKS5 — each gated on its own env var; see root `AGENTS.md`). For those, tag `[external-action]` and name the exact test/command the user should run — you still may not merely assert the bug.

**Include the repro in the report** under each functional finding: the test code (or the commands you ran) plus the observed failing output (assertion message, exception, or wrong value). Repro blocks do not count against the 400-word limit.

Produce a report in exactly this shape, no preamble:

**Verdict**: one of `approve`, `flag`, `escalate`.
- approve = no concerns
- flag = non-blocking suggestions or nits
- escalate = blocking concern that needs user attention before merge (SemVer / public-API break, wire-protocol change, behavior change affecting serialized BSON, spec deviation, lost CSOT propagation, security regression)

**Findings**: bulleted list. Each bullet: `<file>:<line> — [fix-in-code|external-action][blocking|substantive|nit] **<TL;DR>** — <one-sentence problem> — <one-sentence fix or action>`.

The `<TL;DR>` is a terse headline (**≤8 words**) that names the issue at a glance, emitted in bold immediately after the tags and before the one-sentence problem. Examples: `**May throw NullReferenceException**`, `**Typo in skip message**`, `**Exception type changed**`, `**Lost CancellationToken on retry path**`, `**Breaks net472 target**`. Keep it noun-phrase terse — it is a label, not the explanation.

Every finding carries two tags. The first tag says *who can act on it*:
- `[fix-in-code]` — the finding can be resolved by an in-tree code change (edit a file, add a test, fix a typo, tighten a comment, change a throw type, fix a serializer, etc.) that the fixer agent can make mechanically without external information.
- `[external-action]` — the finding requires something outside this codebase: confirming a JIRA ticket (`CSHARP-NNNN`) exists, verifying CI / Evergreen matrix configuration, asking the user to confirm intent, auditing call sites in production code outside the diff, double-checking spec wording against an external source, a test that can only run against infrastructure you don't have (Atlas Search / Vector Search when `ATLAS_SEARCH_TESTS_ENABLED` / `ATLAS_SEARCH_URI` are unset, CSFLE / QE needing `CRYPT_SHARED_LIB_PATH` or KMS env vars, or an external auth mechanism gated on its own env var), or any other action the fixer agent cannot perform without leaving the repo. Note: a test that *can* run here — against the always-available local MongoDB, or against Atlas when `ATLAS_SEARCH_TESTS_ENABLED` / `ATLAS_SEARCH_URI` are set — is **not** an external action; you must run it yourself to verify the finding (see the verification requirement above), not defer it.

When unsure between fix-in-code and external-action, prefer `[external-action]` — it surfaces the concern without claiming the fixer can address it.

The second tag says *how important it is*:
- `[blocking]` — should land an `escalate` verdict and stop the merge. Wire-protocol / public-API / SemVer / spec-conformance / security breaks, or anything that can corrupt serialized data or drop the caller's CSOT deadline.
- `[substantive]` — real concern the fixer should address: wrong behavior, missing test for a non-trivial code path, broken sync/async pairing, layering violation, lost cancellation propagation, etc.
- `[nit]` — mechanical cosmetic: unused import, comment/message typo, misnamed local, missing trailing newline, wording fix-up in a skip message. Does not affect behavior; safe to defer indefinitely.

When unsure between substantive and nit, prefer `substantive` (conservative — it just means the fixer will act on it). When unsure between blocking and substantive, prefer `blocking` if the change can corrupt serialized data, break the public API contract, or compile/pass on one target framework (`netstandard2.1` / `net472` / `net6.0`) while failing on another.

Use repo-relative paths in findings (not absolute) so output is portable. Emit at most **5** findings per pass. If you have identified more than 5, sort by tag (blocking → substantive → nit) and drop the lowest-priority ones — do not pad the list with extra nits.

**Tests run / repros**: list every `dotnet test --filter` or `dotnet run` repro you actually executed, with pass/fail, and — for each functional finding — the repro test code or commands plus the observed output. If you reported no functional findings and ran nothing, write `none`.

Hard limit: 400 words total, **excluding** repro code/output blocks (those are uncapped — include them in full). Do not summarize the diff back; the parent agent has it.
```

### Cross-cutter prompt template

For each cross-cutting reviewer, use this template (substitute `<base>`, `<head>`, `<diff-repo>`, and the *full* changed-file list as absolute paths):

```
You are running as a cross-cutting reviewer in a multi-area branch review. The diff range is <base>...<head> in the repo at <diff-repo>.

Your concern is not scoped to a directory — it is a single hygiene lens applied across the diff. Files in scope for this iteration (absolute paths):
- <abs-file1>
- <abs-file2>
…

[Iteration N > 1 only — omit this block on iteration 1]
This is iteration <N> of an `--iterate` run. The file list above is narrowed to files the previous fixer commit touched plus files that still carry an unresolved [blocking]/[substantive] finding. The following findings were tagged [fix-in-code][nit] in a previous iteration and have not been fixed; do NOT re-emit them unless the surrounding code has changed materially since the previous iteration:
- <reviewer>: <file>:<line> — <message>
- <reviewer>: <file>:<line> — <message>
…
[end Iteration N > 1 block]

Use `git -C "<diff-repo>" diff <base>...<head>` to see the full picture and `git -C "<diff-repo>" diff <base>...<head> -- <repo-relative-path>` to focus. Read files at their current state where context matters. Skip files that are clearly irrelevant to your concern.

Produce a report in exactly the same shape as the area reviewers (Verdict / Findings / Tests run / repros; same format and 400-word cap excluding repro blocks; same verdict semantics; repo-relative paths in findings; the same bold `<TL;DR>` headline (≤8 words) before each finding's one-sentence problem; the same `[fix-in-code]` / `[external-action]` tag *and* the same `[blocking]` / `[substantive]` / `[nit]` severity tag on every finding; the same 5-finding cap; and the same carry-forward-nit rule in iteration N > 1). **The same verification requirement applies**: any functional finding (a real runtime-behavior claim — e.g. credentials reaching a log, redaction not happening, a behavior change on an unchanged signature, a dropped `CancellationToken`/CSOT deadline) must be reproduced by running a test or small repro before you report it — you can and should run tests here (a local MongoDB is always available: the harness uses `MONGODB_URI` if set, otherwise `mongodb://localhost`; Atlas Search / Vector tests also run when `ATLAS_SEARCH_TESTS_ENABLED` and `ATLAS_SEARCH_URI` are set), with the repro included in the report; only defer to `[external-action]` when it truly can't run here (Atlas with those vars unset, CSFLE/KMS infra, or an external auth mechanism). Findings must be specific to your concern — do not duplicate what an area reviewer would catch.
```

### PR-summary prompt template (external PR mode only)

For `pr-summary-reviewer`, use this template (substitute the PR fields, `<base>`, `<head>`, `<diff-repo>`, and the *full* changed-file list as absolute paths):

```
You are running as the PR summary reviewer in a multi-area branch review of an external pull request.

PR: #<number> — <title>
URL: <url>
Diff range: <base>...<head> in the repo at <diff-repo>

All files changed in this range (absolute paths):
- <abs-file1>
- <abs-file2>
…

Pull the PR body yourself with `gh pr view <number> --json body,labels,additions,deletions,changedFiles,author` to get the author's stated rationale. Use `git -C "<diff-repo>" diff <base>...<head>` for the diff and read files at their current state where context matters.

Produce a report in exactly the shape specified in your agent definition (Description / Assessment / Verdict; 500-word cap). Do not duplicate the per-area or cross-cutting reviewers' work.
```

## Step 4 — Aggregate

After all reviewers return, produce one consolidated report in this shape and show only this to the user (do not paste raw sub-agent transcripts).

For the report heading:
- **External PR mode**: `# PR review: #<number> — <title>` followed by the PR URL and `diff range: <base>...<head>` on separate lines.
- **External clone mode**: `# Clone review: <head-label> in <clone>` followed by `diff range: <base>...<head>` on a separate line. (`<head-label>` is the branch name or short SHA captured in step 1; `<clone>` is the absolute path.)
- **Local range mode**: `# Branch review: <base>...<head>`

```
# <heading from above>

## PR summary
(External PR mode only — paste the `pr-summary-reviewer`'s Description, Assessment, and Verdict verbatim. Omit this section in local range mode and external clone mode.)

## Summary
N reviewers ran (M area + 3 cross-cutting [+ 1 PR summary in external PR mode]). X approved, Y flagged, Z escalated. (PR-summary verdict is not counted in those totals — it's a separate lens.)

## Escalations
For each `escalate` verdict from any reviewer — reviewer name, then its findings verbatim. Omit section if none.

## Cross-cutting findings
Group by reviewer (`security-reviewer`, `api-stability-reviewer`, `async-reviewer`). For each: verdict + findings as bullets. Always show this section, even if all three approved (in which case list each as `<reviewer> — clean`).

## Area findings
For each area reviewer that ran: verdict + findings as bullets if `flag`, or `<reviewer> — clean` if `approve`. (Escalations are already covered above.)

## Unmapped changes
Files from the diff that didn't match any area reviewer. (Cross-cutters cover the diff regardless, so this is purely a coverage signal for the area mapping.) Omit section if none.
```

**Save to file** — after composing the report, determine the output filename. The save location is always the parent agent's current working directory (the current clone), never `<diff-repo>` when those differ.

- **External PR mode**: stem is `review<number>` (e.g. `review1991`). Always saved.
- **External clone mode**: stem is `review-<sanitized-head-label>`, where the head label is the branch name or short SHA captured in step 1 with every character that is not `[A-Za-z0-9._-]` replaced by `-` (e.g. branch `CSHARP-5943c` → `review-CSHARP-5943c`; branch `feature/foo` → `review-feature-foo`). Always saved.
- **Local range mode**: in single-pass mode, do not save (the report only appears inline). In **iterate mode**, save using stem `review-<sanitized-head-label>` derived the same way from `git rev-parse --abbrev-ref HEAD` of the current repo — each iteration needs a persistent artifact.

Then:
1. List files in the current directory matching `<stem>[a-z].md`.
2. Find the lowest letter (`a`–`z`) not already taken; use `a` if none exist yet.
3. Write the full report to `<stem><letter>.md` in the current directory using the Write tool.
4. In iterate mode, also prefix the report's `## Summary` section with a line `Iteration <N> of <max>.` so each file is self-identifying even if letters are non-contiguous.
5. Tell the user the filename at the end of your response (one line, e.g. `Saved to review1991a.md` or `Saved to review-CSHARP-5943c-a.md`).

**When `--iterate` is set**, after step 5 do **not** end your response — continue to Step 5 below. Otherwise stop after the file-saved line.

## Step 5 — Iterate (only when `--iterate` is set)

The loop alternates review and fix passes in `<diff-repo>` until two consecutive **clean** reviews or the iteration cap is hit. Mode prerequisite: only local-range and external-clone modes — external PR mode was already rejected in step 1.

**Before the first fixer dispatch**, emit a single user-visible sentence announcing the loop, the cap, and the diff-repo (so the user can interrupt if this isn't what they wanted). Example: `Iterating in /Users/foo/clone (max 10). Each non-clean iteration will commit fixes to that branch.`

### Clean criteria

An iteration's aggregated report is **clean** when **all** of these hold:

1. **No `escalate` verdicts** from any reviewer.
2. **No `[fix-in-code][blocking]` or `[fix-in-code][substantive]` findings.** Severity is the reviewer's call at emit time (see the rubric in Step 3) — the parent does *not* re-classify. `[fix-in-code][nit]` findings do not block convergence: they're surfaced in the report and carried forward to the next iteration's reviewers (so they don't get re-emitted on unchanged code) but the fixer is told to skip them in normal operation.
3. **`[external-action]` findings (any severity) are ignored** for this convergence check. They're real concerns that should be surfaced in every iteration's report (and in the closing summary so the user can act on them outside the loop), but they cannot be mechanically addressed by the fixer — counting them would prevent any loop from terminating. JIRA-ticket existence, CI / Evergreen-matrix verification, "audit callers outside this diff", "confirm spec source", and similar requests all fall here.

State your clean/not-clean call explicitly when you announce the iteration result, with a one-line reason: `Iteration <N>: clean — only [nit] findings remain (unused import, message typo); 2 [external-action] notes carried forward.` or `Iteration <N>: not clean — [fix-in-code][substantive]: lost CancellationToken on the async retry path.`

**Verification gate (required).** A "clean" call — and the final "two consecutive clean reviews → converged" announcement — is a completion claim. Before making either, invoke `superpowers:verification-before-completion` and satisfy it with evidence: do not declare an iteration clean on reviewer verdicts alone if the fixer's last commit changed code that should compile or pass tests. Run the relevant build/test command (a scoped `dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 --filter "<expr>"` for the touched area, or `dotnet build CSharpDriver.sln` when the concern is compilation) and cite the actual pass/fail output in your clean/not-clean announcement. If you cannot run verification (infrastructure the change needs isn't available locally — Atlas, CSFLE/KMS, an external auth mechanism), say so explicitly and treat the iteration as not independently verified rather than silently claiming clean.

### Loop shape

```
consecutive_clean = 0
narrowed_files = None          # iter 1 uses full <base>...<head>; iters >1 use the narrowed set
carry_forward_nits = []        # list of "<reviewer>: <file>:<line> — <message>" strings tagged [nit] in the prior iter
for iter in 1..max_iter:
    review_report = run_steps_1_through_4(
        iter, max_iter,
        file_filter = narrowed_files,         # None means "all files in <base>...<head>"
        carry_forward_nits = carry_forward_nits)
    save(review_report)                       # the file from Step 4
    if is_clean(review_report):
        consecutive_clean += 1
        if consecutive_clean >= 2:
            announce_success(iter); stop
    else:
        consecutive_clean = 0
    if iter == max_iter:
        announce_max_hit(iter); stop
    fixer_result = dispatch_fixer(review_report, iter, max_iter)
    if fixer_result.failed:
        announce_fixer_failure(iter); stop
    if fixer_result.no_op:
        announce_no_op_stop(iter); stop       # no code changed → re-reviewing the same tree would just re-roll LLM dice
    # Prepare narrowed scope for the next iteration:
    touched = git -C "<diff-repo>" diff --name-only HEAD~1 HEAD     # files the fixer just changed
    open_files = files mentioned in [blocking]/[substantive] findings still in the current report
    narrowed_files = union(touched, open_files)
    carry_forward_nits = every [fix-in-code][nit] finding in the current report
    # next iter will re-resolve HEAD in <diff-repo> via git rev-parse, picking up the new commit
```

**Iteration 1 vs later iterations.** Iteration 1 dispatches reviewers against the full diff (`git -C "<diff-repo>" diff --name-only <base>...<head>`) so the initial pass is exhaustive. From iteration 2 onward, the file set passed into Step 2 is restricted to `narrowed_files` — the union of files the previous fixer commit touched and files still carrying an unresolved `[blocking]` or `[substantive]` finding. This is the single biggest leverage on iteration count: stale `[nit]` findings on files no one touched stop regenerating, and reviewers focus on what actually changed. Cross-cutters use the same narrowed set — there is no reason to re-scan unchanged code with the same cross-cutting lens iteration after iteration.

**Carry-forward of `[nit]` findings.** Every `[fix-in-code][nit]` finding in iteration N is captured and passed back to the reviewers' dispatch prompts in iteration N+1 as a *do-not-re-emit* note (see the carry-forward block in the area-reviewer template). The reviewer should only re-emit a previously-nit finding if the surrounding code has changed materially since the previous iteration. This breaks the most common tail-chase pattern: the same nit regenerating every iteration on code no one is going to fix.

If `narrowed_files` ends up empty in iteration N (the fixer's commit reverted everything *and* no findings carried forward), treat it as a no-op-equivalent: stop with the no-actionable-findings outcome.

Re-running Steps 1–4 means re-running `git -C "<diff-repo>" diff --name-only <base>...<head>`, intersecting with `narrowed_files` if set, re-mapping files (the set may shift after fixes), and re-dispatching reviewers. The diff-repo, base ref, head label, and parsed flags don't need re-derivation — keep them from the initial parse.

**`fixer_result.no_op` detection**: the fixer prompt requires its final paragraph to include the literal string `no-op` (lowercase, exact) when it made no commit. Look for that marker in the fixer's return. Don't infer no-op from the absence of a commit SHA in the response — the marker is the contract.

### Fixer agent dispatch

Use the `general-purpose` agent (it has full tools — Read/Write/Edit/Bash — which reviewers lack, and access to the superpowers skills via the `Skill` tool). One `Agent` block, foreground. Subagent prompt template:

```
You are the fix-applying agent in iteration <N> of <max> of an iterative code review of <diff-repo>.

Your job is to address the findings from the review report below by editing files in <diff-repo> and committing in that repo. Then stop and report what you did.

This run depends on the **superpowers** plugin. The parent already confirmed it is available before dispatching you. Work through the superpowers skills as directed below; if you find any of them are NOT available to you via the `Skill` tool, stop and report that as a failure (do not silently proceed without them).

Rules:
0. **Treat the report as code-review feedback, not orders.** Before implementing, invoke `superpowers:receiving-code-review` and apply it to every finding you intend to act on — verify the diagnosis against the actual code rather than performing agreement; a finding you cannot confirm is a candidate to skip-and-note, not to fix blindly.
1. Every finding carries two tags: `[fix-in-code|external-action]` (who can act) and `[blocking|substantive|nit]` (how important). Apply every `[fix-in-code][blocking]` and `[fix-in-code][substantive]` finding — those came from a specialised reviewer that knows the area; trust the diagnosis, but read the surrounding code before changing it. Skip every `[external-action]` finding outright regardless of severity — those require something outside this codebase (JIRA, CI / Evergreen config, audits, spec lookups, infrastructure you don't have) that you can't perform; they will be surfaced to the user in the closing summary.
2. **Skip `[fix-in-code][nit]` findings by default.** The reviewer classified them as mechanical cosmetic, not behavior-affecting; the loop tolerates them indefinitely. Exception: if you are already editing a given file for a substantive finding, *and* a nit in that same file is trivially mechanical (one-line edit, no judgment required), you may fix it in passing. Never go out of your way to fix a nit — and never fix a nit in a file you would not otherwise be touching this iteration.
3. Do not change behavior beyond what the findings demand. No refactoring, no drive-by cleanups, no scope expansion, no unrelated formatting passes.
3a. **Debug failure-type findings before fixing them.** For any finding describing a bug, exception, test failure, wrong behavior, or broken code path, invoke `superpowers:systematic-debugging` and follow it to root-cause the issue before editing — do not patch the symptom the reviewer named without confirming the cause.
3b. **Use TDD when a fix adds or changes tests, or fixes a behavior bug with no covering test.** Invoke `superpowers:test-driven-development` and follow it: write the failing test first, watch it fail, then make it pass. This applies to spec-conformance and functional-test findings in particular. A local MongoDB is always available, so `dotnet test` runs here.
3c. **Brainstorm non-mechanical fixes.** If a `[fix-in-code]` finding's resolution is ambiguous, has multiple plausible designs, or requires inferring intent (not a one-line mechanical change), invoke `superpowers:brainstorming` to settle the approach before editing rather than guessing. If brainstorming reveals the fix genuinely needs user judgment, skip-and-note it instead (see rule 7).
4. Respect the codebase guidance. The parent agent's working directory has up-to-date `AGENTS.md` files — read them as needed via relative paths. In particular: preserve file BOMs; library code uses `ConfigureAwait(false)`; `CancellationToken` and `OperationContext` (CSOT deadline) flow through unchanged — never substitute a fresh `OperationContext` or pass `CancellationToken.None` mid-stack; public I/O methods come in paired sync (`Foo`) / async (`FooAsync`) form and a fix to one side must land on the other; the driver multi-targets `netstandard2.1` / `net472` / `net6.0`, so a fix must compile on all three (no default interface methods, no APIs unavailable on `net472`). The diff-repo at <diff-repo> may have *older* `AGENTS.md` files — prefer the up-to-date ones from the parent agent's cwd when they conflict.
5. All edits land in <diff-repo>. Use absolute paths under <diff-repo> when calling Edit/Write, and `git -C "<diff-repo>" …` for every git command.
6. When done editing, run `git -C "<diff-repo>" status --short`. If there are no changes, do not commit — report `no-op` and stop. Otherwise run `git -C "<diff-repo>" add -A` then `git -C "<diff-repo>" commit -m "[review-iter <N>] Address review findings"` (do NOT include any AI-attribution trailers; do NOT push; do NOT amend any previous commit).
7. If a `[fix-in-code]` finding turns out to be unactionable on closer inspection (the reviewer mis-tagged something that really requires user judgment, the area is unfamiliar enough you'd be guessing, etc.), skip it and note that in your final report. If every finding ends up skipped — fine, that's the expected exit signal: end with the `no-op` marker and the parent loop will stop cleanly. Don't invent makework fixes just to produce a commit.

Target diff-repo (for ALL edits and git commands): <diff-repo>

The review report you must address is below verbatim. Read carefully, then apply.

----- BEGIN REVIEW REPORT -----
<paste the full aggregated report from Step 4 here>
----- END REVIEW REPORT -----

Final output (one short paragraph): what you changed, what you skipped and why, the new commit SHA (or `no-op`), and any unexpected obstacles. Cap 200 words.
```

### After the loop

Emit a single closing summary to the user with:
- Outcome: `clean (stopped after two consecutive clean reviews)`, `max iterations reached`, `fixer failed at iteration <N>`, or `no actionable findings remaining at iteration <N>` (the fixer judged that nothing in the latest report could be addressed in code — the only remaining concerns are `[external-action]`).
- Number of iterations actually run.
- Path to the final report file.
- The final HEAD SHA in `<diff-repo>` so the user can inspect commits (`git -C <diff-repo> log <base>..HEAD --oneline`).
- A short bulleted list of every `[external-action]` finding still present in the final iteration's report, with its reviewer name and the action requested. These need handling outside the loop.

Do not paste the full report transcripts again — they're on disk.

## Notes

- **This skill hard-depends on the superpowers plugin** (Step 0). If `superpowers:requesting-code-review`, `superpowers:receiving-code-review`, `superpowers:test-driven-development`, `superpowers:systematic-debugging`, `superpowers:verification-before-completion`, or `superpowers:brainstorming` is not available, the skill errors out before doing any work. The framing skill runs at the start, the fixer applies receiving-code-review / systematic-debugging / TDD / brainstorming per its rules, and the convergence check is gated on verification-before-completion.
- Reviewers must not *fix* the source tree: each one is configured with `tools: Read, Grep, Glob, Bash` and has no `Edit` / `Write` / `Patch` tool, so they can't apply a fix — treat any suggested fix as something the parent/fixer applies, not the reviewer. They *can* and *must* use `Bash` to verify functional findings, which includes running `dotnet test`, scaffolding a throwaway repro test or project via a here-doc, and `git diff`. Any repro file a reviewer creates is temporary and must be deleted before the reviewer returns, leaving the working tree exactly as found (essential in `--iterate` mode, where the parent diffs the tree between iterations).
- A diff against a feature branch's own base is the typical use; pass an explicit `<base-ref>` for non-`main` cases (e.g. `/review-areas release/3.0`). Pass a second positional arg to cap the end of the range (e.g. `/review-areas HEAD~5 HEAD~1`).
- In external PR mode, the remote is inferred from the PR's repo URL, so PRs on forks (`upstream`, `origin`, or any named remote) are handled automatically without any extra flags.
- In external clone mode, the parent agent stays in the current clone so that `AGENTS.md` files, reviewer briefs, and any other repo guidance load from *here*, while the source code being reviewed and all git history come from `<clone>`. This is the whole point of the mode: review code on a branch that doesn't yet carry the latest agent/architecture updates, using the up-to-date briefs from the current clone. The pr-summary-reviewer does not run in this mode (no PR body to fetch); if you also want a holistic PR summary, run `/review-areas <PR#>` separately.
- `--iterate` makes commits in `<diff-repo>` (one per non-clean iteration). It never pushes. The user can inspect with `git -C <diff-repo> log <base>..HEAD` afterward and amend / squash / drop commits as they see fit. Severity classification (`[blocking]` / `[substantive]` / `[nit]`) is the reviewer's call at emit time, *not* the parent's call after the fact — reviewers see the rubric in their dispatch prompt and pick. The loop terminates as soon as no `[fix-in-code][blocking]` or `[fix-in-code][substantive]` findings remain (`[nit]` and `[external-action]` are carried forward, not counted). If a real substantive concern is leaking through tagged as `[nit]`, the answer is to fix the reviewer brief or surface it to the user — not to tighten the convergence rule.
- The loop has two convergence guards beyond the max-iterations cap. **`[external-action]` findings don't block convergence**: reviewers tag any finding that requires looking up a JIRA ticket, verifying CI / Evergreen config, auditing call sites outside the diff, running against infrastructure that isn't available locally, or otherwise leaving the codebase, and those tags exclude the finding from the clean check. The user still sees them — they're listed in every iteration's report and in the closing summary — but the fixer can't address them so they shouldn't pin the loop open. **A no-op fixer ends the loop**: if the fixer judges that nothing in the current report is mechanically actionable, the next iteration would just be re-rolling LLM verdicts on the same tree, so the loop stops with outcome `no actionable findings remaining` and surfaces the outstanding `[external-action]` items.
- Test-running by reviewers is **required for functional findings**, not opportunistic: reviewers can and should run tests on this machine, and a reviewer must reproduce any runtime-behavior claim by running code before reporting it — too many reported findings turn out not to reproduce. A local MongoDB is always available (the harness uses the `MONGODB_URI` connection string if set, otherwise `mongodb://localhost`; there is no auto-testcontainer — a local `mongod` is simply running), so `dotnet test` always runs here. Atlas Search / Vector Search tests also run when `ATLAS_SEARCH_TESTS_ENABLED` and `ATLAS_SEARCH_URI` are set — reviewers should run those too rather than defer. Include the repro in the report. The driver's test suite is not built to run in parallel and reviewers run concurrently against the same local server — scope repros to the tightest `--filter`. Only tests that need infrastructure not available locally — Atlas Search / Vector Search with `ATLAS_SEARCH_*` unset, CSFLE / QE (`CRYPT_SHARED_LIB_PATH`, KMS env vars), or an external auth mechanism (each gated on its own env var per root `AGENTS.md`) — stay `[external-action]`.
