# LINQ Translation Benchmarks

Measures the performance of the LINQ-to-aggregation translation layer — the CPU work the driver does to convert a C# expression tree into a MongoDB query/pipeline document. No database I/O is involved; these benchmarks isolate translator regressions from network and serialization noise.

## Running

```bash
cd benchmarks/MongoDB.Driver.Benchmarks

# All LINQ benchmarks
dotnet run -c Release -- --filter "*LinqTranslation*"

# Specific benchmark
dotnet run -c Release -- --filter "*MultiFieldSearch*"

# As part of the full perf suite (also runs DriverBench, BsonBench, etc.)
dotnet run -c Release -- --driverBenchmarks --anyCategories "LinqBench"
```

## Benchmark Inventory

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

### Projection benchmarks (2)

| Benchmark | Entry Point | Notes |
|---|---|---|
| `AggregationProjection` | `TranslateExpressionToProjection` | 4-field DTO with computed arithmetic (`Subtotal + Tax - Discount`) and nested `Items.Select(i => i.ProductId)` transform |
| `ProjectionSentinel` | `TranslateExpressionToProjection` | `x => x` — fast-path early return. Sentinel for fast-path breakage, not translator perf. |

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

## Code Path Coverage

Each benchmark exercises a specific subset of the translation pipeline:

| Component | Filters | FieldSelection | Projections | Sentinel | Update | IQueryable |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `LinqExpressionPreprocessor` | ✓ | ✓ | ✓ | — | values only | ✓ |
| `SerializerFinder` | ✓ | ✓ | ✓ | — | ✓ | ✓ |
| `ExpressionToFilterTranslator` (incl. Not, MemberAccess) | ✓ | — | — | — | — | ✓ (Where) |
| `ExpressionToFilterFieldTranslator` | — | ✓ | — | — | — | — |
| `ExpressionToAggregationExpressionTranslator` | — | — | ✓ | — | ✓ (values) | ✓ (Select) |
| `ExpressionToSetStageTranslator` | — | — | — | — | ✓ | — |
| `GroupByMethodToPipelineTranslator` | — | — | — | — | — | GroupBy only |
| `AstSimplifier` | ✓ | — | ✓ | — | ✓ | ✓ |
| `AstPipelineOptimizer` | — | — | — | — | — | ✓ |

This selectivity has been validated via targeted injection tests.

## Interpreting Results

**Allocation changes** are often more actionable than time changes. A new allocation in a hot path is a real regression even if the time delta is within noise. The `[MemoryDiagnoser]` columns (`Gen0`, `Allocated`) make allocation regressions visible.

**`OrFilter`** is the fastest filter (~7µs, ~5x faster than others) because it uses literal constants instead of captured variables, producing a simpler expression tree with less work at every stage. This makes it the most sensitive filter benchmark — small translator regressions that would be lost in the noise on slower benchmarks show up clearly here.

**`ProjectionSentinel`** at ~17ns is not measuring translation. It validates that `LinqProviderAdapter` still short-circuits `x => x` projections. Movement here means the fast-path detection broke, not that translation got slower.

## Regression Thresholds

Provisional thresholds based on 7-run M1 Max characterization (min/max range as a % of median). These should be tightened once calibrated on the perf-job hardware (`rhel90-dbx-perf-large`).

Three drift clusters emerged from characterization:

| Bucket | Threshold | Benchmarks | Observed range |
|---|---|---|---|
| Tight | 15% | `MultiFieldSearch`, `UpdatePipeline`, `BatchLookup`, `ArrayElementQuery` | 9–15% |
| Wider | 30% | `OrFilter`, `FieldSelection`, `AggregationProjection`, `QueryablePipeline`, `GroupByAggregation` | 20–29% |
| Sentinel | 100% | `ProjectionSentinel` | 5% |

`OrFilter` and `FieldSelection` land in the wider bucket for different reasons: `OrFilter` is ~7µs and proportional noise is large at that scale; `FieldSelection` is ~2µs with similar characteristics. The three complex benchmarks (`AggregationProjection`, `QueryablePipeline`, `GroupByAggregation`) show higher drift because they allocate more and exercise more GC pressure.
