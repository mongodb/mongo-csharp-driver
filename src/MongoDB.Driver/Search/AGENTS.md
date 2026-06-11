---
area: Atlas Search & Vector Search
scope: ["src/MongoDB.Driver/Search/**/*.cs"]
reviewer-agent: search-reviewer
adjacent-areas: [Driver root (aggregation fluent + index helpers), Driver/Linq]
---

# Atlas Search & Vector Search — AGENTS.md

Builders for the `$search` and `$vectorSearch` aggregation stages. Atlas-only — there is no local MongoDB equivalent. All tests gated by `ATLAS_SEARCH_TESTS_ENABLED` and `ATLAS_SEARCH_URI`.

## Public API

Two builder hubs reachable from `Builders<TDocument>`:

- `Builders<T>.Search` — `SearchDefinition<TDocument>` builders for `$search`.
- `Builders<T>.SearchPath`, `Builders<T>.SearchScore`, `Builders<T>.SearchScoreFunction`, `Builders<T>.SearchFacet`, `Builders<T>.SearchSpan` — supporting builder hubs for path / scoring / score-function expressions / faceting / span queries.

**`SearchScoreFunction` name collision.** The name is shared by two unrelated types. `Builders<T>.SearchScoreFunction` (a property) returns a `SearchScoreFunctionBuilder<TDocument>`, which produces the generic `SearchScoreFunction<TDocument>` expression class for `Function(...)` score modifiers. Separately, at the driver root, `SearchScoreFunction` is *also* a plain enum (`src/MongoDB.Driver/SearchScoreFunction.cs`) used by `VectorSearchOptions.EmbeddedScoreMode`. They share a name but aren't overloads. Don't wire the enum into a `Function(...)` call.

Index helpers live at the root of `src/MongoDB.Driver/`: the non-generic `CreateSearchIndexModel`, plus the generic `CreateVectorSearchIndexModel<TDocument>` and `CreateAutoEmbeddingVectorSearchIndexModel<TDocument>` (both inheriting `CreateVectorSearchIndexModelBase<TDocument>`), and `SearchIndexType`. Index management goes through `IMongoCollection<T>.SearchIndexes`.

## Operators

`SearchDefinitionBuilder<T>` covers leaf operators returning `SearchDefinition<T>`: `Autocomplete`, `EmbeddedDocument`, `Equals`, `Exists`, `Facet`, `GeoShape`, `GeoWithin`, `HasAncestor`, `HasRoot`, `In`, `MoreLikeThis`, `Near`, `Phrase`, `QueryString`, `Range`, `Regex`, `Span`, `Text`, `Wildcard`, `VectorSearch`. `Compound` is a builder hub rather than a leaf — `Builders<T>.Search.Compound()` returns a `CompoundSearchDefinitionBuilder<TDocument>` for assembling `must` / `mustNot` / `should` / `filter` clauses with `minimumShouldMatch`.

## Vector search

Two paths:

- Top-level stage: `IAggregateFluent<T>.VectorSearch(path, queryVector, limit, options)` — preferred for pure vector queries.
- Inside compound: `Builders<T>.Search.VectorSearch(...)` — for hybrid (text + vector) queries.

Key types:

- `QueryVector` — note this type lives at the driver root (`src/MongoDB.Driver/QueryVector.cs`), not under `Search/`. Two ways in:
  - **Implicit conversions** (common case): from `double[]` / `float[]` / `int[]` for raw numeric arrays, from `ReadOnlyMemory<double|float|int>` for memory shapes, from `BinaryVectorInt8` / `BinaryVectorFloat32` / `BinaryVectorPackedBit` for the typed BinaryVector encodings, or from `string` for Atlas auto-embedding. The array forms route through a `private QueryVector(BsonArray)` ctor — there is no `public QueryVector(double[])` overload, so `new QueryVector(myDoubleArray)` does **not** compile; use the implicit conversion (or pass the array in to a method that accepts `QueryVector`).
  - **Explicit constructors** are limited to: `string` (auto-embedding), `BsonBinaryData` (the one input shape with **no** implicit conversion — must be called explicitly), and the three `ReadOnlyMemory<double|float|int>` overloads.

  See `src/MongoDB.Driver/QueryVector.cs` for the authoritative constructor list.
- `VectorSearchOptions<TDocument>` — for the **top-level** `IAggregateFluent<T>.VectorSearch` stage. Carries `Filter` (a `FilterDefinition<TDocument>` pre-filter), `NestedFilter`, `NumberOfCandidates` (search-set size for ANN), `Exact` (true → ENN, ignores candidates), plus auto-embedding-related fields `IndexName`, `AutoEmbeddingModelName`, `ReturnStoredSource`, and `EmbeddedScoreMode`. The compound-embedded form (`Builders<T>.Search.VectorSearch`) takes the related but distinct `VectorSearchOperatorOptions<TDocument>` — don't conflate them. `VectorSearchOperatorOptions<TDocument>` has a deliberately smaller surface (it omits the auto-embedding fields and `EmbeddedScoreMode` that only make sense at the top-level stage); reach for it only inside a compound query.

Tuning rule: for approximate (ANN) searches `NumberOfCandidates` should be much larger than `Limit` — typical ratio 10×–20×. `Exact = true` switches to exact nearest-neighbor and ignores `NumberOfCandidates`.

## Score modifiers, highlighting, faceting

- `SearchScoreDefinition<T>` / builders for `Boost`, `Constant`, `Function` (with custom expressions). Apply via `score: …` on most operators.
- `SearchHighlight`, `SearchHighlightOptions` for hit highlighting in returned documents.
- `SearchMetaResult` (non-generic) carries facet counts (returned as the result of `$searchMeta`).
- `SearchSpanDefinition<TDocument>` (built via `Builders<T>.SearchSpan`) describes span queries — sequence/proximity/order constraints over the text positions of subordinate clauses.
- `Builders<T>.SearchFacet` builds string / numeric / date facets to embed in the search stage.

## Common pitfalls

- **`$search` must be the first stage** in the pipeline. `$vectorSearch` likewise. The driver doesn't enforce this — Atlas does, with an error that's easy to misread. If you have predicates that should run before, fold them into the search stage's `compound.filter` (free, indexed) rather than a downstream `$match` (server-side, after scoring).
- **Index naming.** The `index` parameter on `$search`/`$vectorSearch` refers to an Atlas Search index by name, not a MongoDB collection name. Wrong index → empty results, no error.
- **`NumberOfCandidates` < `Limit`.** Approximate search degrades silently to lower-quality results. The driver doesn't validate the relationship.
- **Atlas-only.** Don't ship search-using code paths to environments that may run on local MongoDB without an Atlas backend. Tests gated by `ATLAS_SEARCH_TESTS_ENABLED` exist to keep local CI runs green.
- **`UseConfiguredSerializers` on value-comparing operators.** Default is **true** (registered serializers are honored, e.g. enum-as-string). The extension is `SearchDefinitionExtensions.UseConfiguredSerializers` — declared on `SearchDefinition<TDocument>` but downcasts to `OperatorSearchDefinition<TDocument>` and throws `NotSupportedException` if the receiver isn't one. Only three operators actually read the flag — `Equals`, `In`, and `Range` (see the three `if (_useConfiguredSerializers)` checks in `OperatorSearchDefinitions.cs`); on any other `OperatorSearchDefinition<TDocument>` the flag is stored but never consulted, so the call is silently a no-op. Setting it to `false` makes those three operators compare against raw BSON, ignoring custom serializers — the breaking case is flipping it off and silently changing the search semantics.
- **Compound clause precedence.** `must` requires every clause; `should` adds score but does not require; `filter` requires without affecting score; `mustNot` excludes. Mixing these is the most common source of "why are these documents missing".

## How to test

```bash
ATLAS_SEARCH_TESTS_ENABLED=true ATLAS_SEARCH_URI=<atlas-uri> \
  dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Search"
```

Index-helper tests additionally require `ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED=true`. See the env-var table in the root `AGENTS.md`.

Where index management is being changed without an Atlas environment available, **stop and ask** — local MongoDB cannot fake the search index API.
