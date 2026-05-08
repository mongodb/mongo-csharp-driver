# LINQ Translation Benchmarks

Measures the performance of the LINQ-to-aggregation translation layer — the CPU work the driver does to convert a C# expression tree into a MongoDB query/pipeline document. No database I/O is involved; these benchmarks isolate translator regressions from network and serialization noise.

## Running

```bash
cd benchmarks/MongoDB.Driver.Benchmarks

# All LINQ benchmarks
dotnet run -c Release -- --filter "*LinqTranslation*"

# Specific benchmark
dotnet run -c Release -- --filter "*EqualityById*"

# As part of the full perf suite (also runs DriverBench, BsonBench, etc.)
dotnet run -c Release -- --driverBenchmarks --anyCategories "LinqBench"
```

## Benchmark Inventory

### Filter benchmarks (9)

Each calls `LinqProviderAdapter.TranslateExpressionToFilter`, exercising: preprocessor → SerializerFinder → ExpressionToFilterTranslator → AstSimplifier → render.

| Benchmark | Expression | Notes |
|---|---|---|
| `EqualityById` | `x => x.Id == id` | Most common filter pattern |
| `CompoundFilter` | `x.Status == s && x.CreatedAt > cutoff` | Multi-condition with AND |
| `InListFilter` | `ids.Contains(x.Id)` | Batch-fetch pattern |
| `StringMethodFilter` | `x.CustomerName.StartsWith(prefix)` | String method translation |
| `ArrayAnyWithPredicate` | `x.Items.Any(i => i.Price > t)` | Array sub-document filtering |
| `NestedMemberFilter` | `x.ShippingAddress.City == c` | Embedded document navigation |
| `OrChainFilter` | 4-way `==` OR with literal constants | No closure capture — measures raw translation cost |
| `DateTimeMethodFilter` | `x.CreatedAt.Year == y` | DateTime member method |
| `InstanceFieldCaptureFilter` | `x.Status == _activeStatus` | Captures `this`-field instead of stack-local |

### Projection benchmarks (5)

| Benchmark | Entry Point | Notes |
|---|---|---|
| `WholeDocumentProjectionSentinel` | `TranslateExpressionToProjection` | `x => x` — fast-path early return. Sentinel for fast-path breakage, not translator perf. |
| `PocoProjection` | `TranslateExpressionToProjection` | 3-field DTO via MemberInit |
| `FindProjection` | `TranslateExpressionToFindProjection` | Same expression as PocoProjection, but through `AstFindProjectionSimplifier` |
| `WidePocoProjection` | `TranslateExpressionToProjection` | 15-field DTO — scaling behavior |
| `ProjectionWithNestedTransform` | `TranslateExpressionToProjection` | `Items.Select(i => i.ProductId)` inside projection |

### Composition benchmark (1)

| Benchmark | Entry Point | Notes |
|---|---|---|
| `IQueryableComposition` | `ExpressionToExecutableQueryTranslator.Translate` | `Where → Select → OrderBy → Take` via MongoQueryProvider. Covers the IQueryable code path users write with `collection.AsQueryable()`. |

## Code Path Coverage

Each benchmark exercises a specific subset of the translation pipeline:

| Component | Filters | Projections | Sentinel | IQueryable |
|---|:---:|:---:|:---:|:---:|
| `LinqExpressionPreprocessor` | ✓ | ✓ | — | ✓ |
| `SerializerFinder` | ✓ | ✓ | — | ✓ |
| `ExpressionToFilterTranslator` | ✓ | — | — | ✓ (Where) |
| `ExpressionToAggregationExpressionTranslator` | — | ✓ | — | ✓ (Select) |
| `AstSimplifier` | ✓ | ✓ | — | ✓ |
| `AstFindProjectionSimplifier` | — | FindProjection only | — | — |
| `AstPipelineOptimizer` | — | — | — | ✓ |

This selectivity has been validated via targeted injection tests (see `workdocs/LINQ-benchmarks/work_context.md`).

## Interpreting Results

**Allocation changes** are often more actionable than time changes. A new allocation in a hot path is a real regression even if the time delta is within noise. The `[MemoryDiagnoser]` columns (`Gen0`, `Allocated`) make allocation regressions visible.

**`OrChainFilter`** is the fastest filter at ~7µs (~5x faster than others) because it uses literal constants instead of captured variables, producing a simpler expression tree with less work at every stage. This makes it the most sensitive filter benchmark — small translator regressions that would be lost in the noise on slower benchmarks show up clearly here.

**`WholeDocumentProjectionSentinel`** at ~17ns is not measuring translation. It validates that `LinqProviderAdapter` still short-circuits `x => x` projections. Movement here means the fast-path detection broke, not that translation got slower.

## Regression Thresholds

Provisional thresholds based on M1 Max local characterization. These should be tightened once calibrated on the perf-job hardware (`rhel90-dbx-perf-large`).

| Bucket | Threshold | Benchmarks |
|---|---|---|
| Default | 15% | Most filters, `PocoProjection`, `FindProjection`, `IQueryableComposition` |
| Wider | 30% | `OrChainFilter` (very fast, proportional noise is large), `WidePocoProjection`, `ProjectionWithNestedTransform` |
| Sentinel | 100% | `WholeDocumentProjectionSentinel` |
