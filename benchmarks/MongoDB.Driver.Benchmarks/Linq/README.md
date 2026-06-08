# LINQ Benchmarks

This directory contains two LINQ benchmark suites with different goals:

- **Translation** (`LinqTranslationBenchmark`, category `LinqBench`) — measures the CPU work the driver does to convert a C# expression tree into a MongoDB query/pipeline document. These benchmarks execute no queries; they isolate translator regressions from network and serialization noise. (The two `IQueryable` benchmarks — `QueryablePipeline` and `GroupByAggregation` — create a `MongoClient` in `[GlobalSetup]` because `MongoQueryProvider<T>` is obtained via `collection.AsQueryable()`. No queries are executed against that client; its background cluster monitor is the only DB-side activity, and the measurement impact is expected to sit below the 2-4% drift floor.)
- **End-to-end** (`LinqEndToEndBenchmark`, also category `LinqBench` but excluded from its composite) — runs real queries against a server and measures how much of user-visible latency the translator accounts for under realistic result sizes. See [End-to-End Benchmarks](#end-to-end-benchmarks) below.

## Running

```bash
cd benchmarks/MongoDB.Driver.Benchmarks

# All translation benchmarks (no server needed)
dotnet run -c Release -- --filter "*LinqTranslation*"

# All end-to-end benchmarks (requires a reachable server; seeds and drops the linqbench database)
dotnet run -c Release -- --filter "*LinqEndToEnd*"

# Specific benchmark
dotnet run -c Release -- --filter "*MultiFieldSearch*"

# As part of the full perf suite (also runs DriverBench, BsonBench, etc.); LinqBench covers both LINQ suites
dotnet run -c Release -- --driverBenchmarks --anyCategories "LinqBench"
```

## Translation Benchmark Inventory

### Filter benchmarks (4)

Each calls `LinqProviderAdapter.TranslateExpressionToFilter`, exercising: preprocessor → SerializerFinder → ExpressionToFilterTranslator → AstSimplifier → render.

| Benchmark | Expression | Translator path exercised |
|---|---|---|
| `MultiFieldSearch` | `x.Status == s && x.CustomerName.StartsWith(prefix) && x.ShippingAddress.City == city && x.CreatedAt > cutoff && !x.IsPaid` | And → Comparison, MethodCall (StartsWith), nested MemberAccess, Not + boolean MemberAccess |
| `OrFilter` | 4-way `==` OR with literal constants | Or → Comparison. No closures — fastest filter, most sensitive to small regressions |
| `BatchLookup` | `ids.Contains(x.Id)` | MethodCall → ContainsMethodToFilterTranslator → `$in` |
| `ArrayElementQuery` | `x.Items.Any(i => i.Price > t)` | MethodCall → AllOrAnyMethodToFilterTranslator → `$elemMatch`, `@<elem>` symbol |

### Field benchmark (1)

| Benchmark | Expression | Translator path exercised |
|---|---|---|
| `FieldSelection` | `x => x.Items[0].ProductId` | `TranslateExpressionToField` → ExpressionToFilterFieldTranslator (MethodCall/get_Item, MemberAccess, Parameter sub-translators) |

### Projection benchmark (1)

| Benchmark | Entry Point | Notes |
|---|---|---|
| `AggregationProjection` | `TranslateExpressionToProjection` | 4-field DTO with computed arithmetic (`Subtotal + Tax - Discount`) and nested `Items.Select(i => i.ProductId)` transform |

### Update benchmark (1)

| Benchmark | Entry Point | Notes |
|---|---|---|
| `UpdatePipeline` | `TranslateExpressionToSetStage` | Sets 3 fields including a computed expression (`Subtotal + Tax - Discount`). Exercises `ExpressionToSetStageTranslator` with MemberInit pattern matching. |

### IQueryable benchmarks (2)

Both use `ExpressionToExecutableQueryTranslator.Translate` via `MongoQueryProvider` — the pipeline composition path users write with `collection.AsQueryable()`.

| Benchmark | Expression chain | Notes |
|---|---|---|
| `QueryablePipeline` | `Where → Select → OrderBy → Take` | Exercises filter, projection, sort, and limit pipeline stages |
| `GroupByAggregation` | `GroupBy → Select` with Count + Sum | Exercises `GroupByMethodToPipelineTranslator`, `$group` stage, IGroupingSerializer, accumulators |

## Interpreting Results

**Allocation changes** are often more actionable than time changes. A new allocation in a hot path is a real regression even if the time delta is within noise. The `[MemoryDiagnoser]` columns (`Gen0`, `Allocated`) make allocation regressions visible.

**`OrFilter`** is the fastest filter (~7µs, ~5x faster than others) because it uses literal constants instead of captured variables, producing a simpler expression tree with less work at every stage. This makes it the most sensitive filter benchmark — small translator regressions that would be lost in the noise on slower benchmarks show up clearly here.

## End-to-End Benchmarks

`LinqEndToEndBenchmark` executes real queries against a server. Each operation appears as a pair: a `*Linq` variant that translates the expression on every call, and a `*Raw` variant that sends a pre-built BSON document and skips translation. Both issue the same query shape (the raw shapes are built in `[GlobalSetup]` to match what the provider emits), so everything downstream — server execution, network, serialization, deserialization — is common to both, and the Linq−Raw delta approximates translation's contribution to that operation's end-to-end time.

The collection is seeded with 500 documents and indexed so server-side filter/group time stays small. This keeps the cross-benchmark deltas dominated by translation and serialization rather than COLLSCAN variance, while still returning a realistic result size so serialization is a visible part of the total.

| Pair | Operation |
|---|---|
| `MultiFieldSearch{Linq,Raw}` | compound `Find` filter |
| `OrFilter{Linq,Raw}` | 4-way `$or` `Find` filter |
| `GroupBy{Linq,Raw}` | `$group` aggregate (count + sum) |
| `Projection{Linq,Raw}` | `Find` with server-side inclusion projection |
| `InFilter{Linq,Raw}` | `$in` over a set of ids |
| `PagedQuery{Linq,Raw}` | `Where → OrderBy → ThenBy → Take` pipeline |

**What this suite is for — and what it is not.** Translation is a low-single-digit fraction of end-to-end latency (for `OrFilter`, whose result set serializes hundreds of KB, the translator's share is ~1.2%). That sits well under the suite's drift floor, so a *translator regression is not reliably visible here* — those are caught directly by the translation suite above. What the end-to-end suite uniquely shows, tracked over time, is movement in the *non-translation* costs — serialization, deserialization, server, network — that changes how large a share translation represents. The translation suite is blind to those by construction.

Because the numbers are dominated by server and network time, they carry confounds the translation suite does not (server version, hardware, network). They are tracked as a **trend**, not a pass/fail gate: read the series for how the translator's share drifts, not for single-run regressions.

## Regression Thresholds

Provisional thresholds for the **translation** benchmarks, based on local characterization (min/max range as a % of median). These should be tightened once calibrated on the perf-job hardware. The end-to-end benchmarks are tracked as a trend rather than gated — their server- and network-dominated timings make single-run thresholds unreliable, and translator regressions surface in the translation suite, not here.

Two drift clusters emerged from characterization:

| Bucket | Threshold | Benchmarks | Observed range |
|---|---|---|---|
| Tight | 15% | `MultiFieldSearch`, `UpdatePipeline`, `BatchLookup`, `ArrayElementQuery` | 9–15% |
| Wider | 30% | `OrFilter`, `FieldSelection`, `AggregationProjection`, `QueryablePipeline`, `GroupByAggregation` | 20–29% |

`OrFilter` and `FieldSelection` land in the wider bucket for different reasons: `OrFilter` is ~7µs and proportional noise is large at that scale; `FieldSelection` is ~2µs with similar characteristics. The three complex benchmarks (`AggregationProjection`, `QueryablePipeline`, `GroupByAggregation`) show higher drift because they allocate more and exercise more GC pressure.
