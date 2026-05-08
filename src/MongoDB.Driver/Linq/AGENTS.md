---
area: LINQ Provider (Linq3)
scope: ["src/MongoDB.Driver/Linq/*.cs", "src/MongoDB.Driver/Linq/**/*.cs"]
reviewer-agent: linq-reviewer
adjacent-areas: [Driver root (aggregation fluent), Driver/Core/Operations, Bson/Serialization]
---

# LINQ Provider — AGENTS.md

Translates C# `Expression` trees from `IQueryable<T>` into MongoDB aggregation pipelines. The current implementation is **Linq3**; Linq2 is gone. Subdirectory layout:

- Top-level — split by visibility:
  - **Public surface:** `MongoQueryable`, `MongoEnumerable`, `IMongoQueryProvider`, `ExpressionNotSupportedException`.
  - **Internal helpers that live alongside it:** `LinqProviderAdapter` (`internal static`) and the internal forwarder `IMongoQueryableForwarder<TOutput>` used by it.
  - **Concrete `IQueryable<T>` implementation:** `MongoQuery<TDocument, TOutput> : MongoQuery<TOutput>, IOrderedQueryable<TOutput>, IAsyncCursorSource<TOutput>, IMongoQueryableForwarder<TOutput>` — with `MongoQuery<TOutput>` as an `internal abstract` base. Both live at `Linq3Implementation/MongoQuery.cs`, **not** at the top level despite their conceptual centrality. The `IAsyncCursorSource<TOutput>` implementation is what makes `ToList`/`ToCursor` work directly on the query — see the terminals discussion below.
- `Linq3Implementation/` — the engine; everything below.

## Pipeline at a glance

```
LINQ Expression Tree
      │
      ▼  PartialEvaluator (closures → constants)
      ▼  LinqExpressionPreprocessor (normalize)
      ▼  ExpressionToPipelineTranslator (per-method translators)
      ▼      ExpressionToFilterTranslators (for $match)
      ▼      ExpressionToAggregationExpressionTranslators (for fields/exprs)
      ▼      ExpressionToSetStageTranslators (for $set)
      ▼  AstPipeline (internal AST: Stages, Expressions, Filters)
      ▼  AstPipelineOptimizer (combine, hoist)
      ▼  Render → BsonDocument[] (the final pipeline)
      ▼  ExecutableQuery + IExecutableQueryFinalizer (terminal)
      ▼  IMongoCollection.Aggregate / AggregateAsync (which then constructs AggregateOperation / AggregateToCollectionOperation under Core/Operations)
```

## Key directories under `Linq3Implementation/`

- **`Translators/`** — the transformation engine. Subdirectories by translation phase:
  - `ExpressionToPipelineTranslators/` — top-level method calls (`Where`, `Select`, `GroupBy`, `OrderBy`, `Lookup`, `SelectMany`, `Skip`, `Take`, `Distinct`) → `$match`/`$project`/`$group`/`$sort`/etc.
  - `ExpressionToFilterTranslators/` — predicate expressions inside `$match`.
  - `ExpressionToAggregationExpressionTranslators/` — anything inside lambdas (binary ops, method calls, member access, `new {…}`).
  - `ExpressionToExecutableQueryTranslators/` + `Finalizers/` — terminal-method handling. Three things to know:
    - **Dispatched terminals.** Currently dispatched by method name: `First`, `FirstOrDefault`, `Single`, `SingleOrDefault`, `Count`, `LongCount`, `Any`, `All`, `Contains`, `Sum`, `Average`, `Min`, `Max`, `Last`, `ElementAt`, and their `*Async` variants. Treat this list as **illustrative, not authoritative** — terminals are added over time; the source of truth is the dispatcher itself under `ExpressionToExecutableQueryTranslators/`. Dispatch and runtime support are different things: some of these (notably `Last` and `ElementAt`) translate only when the source has an ordered cursor; in other shapes they throw `ExpressionNotSupportedException` at execution time.
    - **Shared finalizers.** The `Finalizers/` folder holds the four shared finalizers `FirstFinalizer`, `FirstOrDefaultFinalizer`, `SingleFinalizer`, `SingleOrDefaultFinalizer`. Most other terminal translators also use an `IExecutableQueryFinalizer`, either by reusing one of those shared finalizers (e.g. `CountMethodToExecutableQueryTranslator` reuses `SingleOrDefaultFinalizer<int>`) or by defining a private inline finalizer in their own translator file.
    - **`ToList` / `ToCursor` exception.** Neither is in this dispatcher. They are cursor-source terminals on `MongoQuery<TDocument, TOutput>` itself (via `IAsyncCursorSource<TOutput>`) and on the `IAsyncCursorSourceExtensions` static class — they do not flow through `ExpressionToExecutableQueryTranslator.Translate`.
  - `ExpressionToSetStageTranslators/` — for the `$set` aggregation stage.
- **`Ast/`** — internal AST. Three node families:
  - `Stages/` — pipeline stages (non-exhaustive: `AstMatchStage`, `AstProjectStage`, `AstGroupStage`, `AstLookupStage`, `AstGraphLookupStage`, `AstUnwindStage`, `AstFacetStage`, `AstSetWindowFieldsStage`, `AstDensifyStage`, …)
  - `Expressions/` — aggregation expressions (`AstFieldPathExpression` / `AstGetFieldExpression` for field references, `AstConstantExpression`, `AstBinaryExpression`, `AstFunctionExpression`, date/string/array operations)
  - `Filters/` — `$match` filter AST. Top-level filter shapes (`AstAndFilter`, `AstOrFilter`, `AstFieldOperationFilter`, …) compose with per-operator nodes whose names follow the `*FilterOperation` convention (`AstComparisonFilterOperation`, `AstInFilterOperation`, `AstRegexFilterOperation`, …).
  - Plus `Optimizers/` and `Visitors/` for rewriting.
- **`Serializers/`** — projection-output serializers. Two of them — `IEnumerableSerializer<TItem>` and `IGroupingSerializer<TKey, TElement>` — are `internal class`es whose names happen to start with `I`, *not* interfaces. Other serializers here include `DictionarySerializer`, `NestedAsOrderedQueryableSerializer`, and `ConvertIntegralTypeToEnumSerializer`. The client-side projection helpers live elsewhere: `ClientSideProjectionDeserializer<,>` is at the public root (`src/MongoDB.Driver/`), and the in-LINQ helper is `ClientSideProjectionHelper` under `Linq3Implementation/Misc/`.
- **`SerializerFinders/`** — `SerializerFinderVisitor` walks lambda bodies to infer the right serializer for the projection result. Per-node visit logic is split across files named `SerializerFinderVisit<Node>.cs` (e.g. `SerializerFinderVisitMethodCall.cs`, `SerializerFinderVisitMember.cs`); the `SerializerFinderVisitMethodCall.cs` file tends to be the large one because it knows about every supported MongoDB LINQ extension.
- **`Reflection/`** — reflection-metadata constants for recognized members. Three file shapes: `<Family>Method.cs` files hold `MethodInfo` constants (the vast majority); `<Family>Constructor.cs` files (e.g. `DateTimeConstructor.cs`, `EnumerableConstructor.cs`) hold `ConstructorInfo` constants; `<Family>Property.cs` files (e.g. `StringProperty.cs`, `DateTimeProperty.cs`) hold `PropertyInfo` constants. The framework families (`EnumerableMethod`, `QueryableMethod`, `StringMethod`, `DateTimeMethod`, `MathMethod`, …) sit alongside the driver-extension families that most new translators end up needing: `MongoQueryableMethod` / `MongoEnumerableMethod` host the driver-only LINQ-operator method infos (e.g. `Documents` / `$documents`, `Densify*` / `$densify`, `Lookup*` overloads); `LinqExtensionsMethod` hosts the `LinqExtensions` helpers; `MqlMethod` is the narrow set of `Mql.*` helpers (`Exists`, `Field`, `IsMissing`, etc.) for use inside expression bodies. Translators dispatch by reference-equality on these constants. When adding recognition for a new constructor or property, put it in the corresponding `*Constructor.cs` / `*Property.cs` file — don't invent a parallel scheme.
- **`Misc/`** — utilities: `PartialEvaluator` (closure capture), `LinqExpressionPreprocessor`, `ConvertHelper`, `DocumentSerializerHelper`, `ExpressionReplacer`, `NameGenerator` (for unique aggregation variable names like `_v0`), `SymbolTable` (the variable-binding scope referenced in the *Variable scoping* pitfall below).

And the file most translator work touches (a single class, not a directory): **`Linq3Implementation/Translators/TranslationContext.cs`** — carries serializers, translation options, and a `SymbolTable` (with a `NameGenerator`) representing the current variable-binding scope. **The single most important type to understand** for translator work.

## Adding support for a new operator/method

1. Add a `MethodInfo` constant in `Reflection/<Family>Method.cs` — `EnumerableMethod`/`QueryableMethod` for BCL methods, `MongoQueryableMethod`/`MongoEnumerableMethod`/`LinqExtensionsMethod`/`MqlMethod` for driver-only methods.
2. For **expression-level** operators (used inside lambdas, e.g. `Select`, `Where`): create a translator in `Translators/ExpressionToAggregationExpressionTranslators/MethodTranslators/<NewMethod>MethodToAggregationExpressionTranslator.cs` (the `MethodToAggregationExpressionTranslator` suffix is the established convention — match it so the dispatcher's grep-by-name search finds it). **Then add a `case "<MethodName>": return …` branch in the `switch (expression.Method.Name)` block in `Translators/ExpressionToAggregationExpressionTranslators/MethodCallExpressionToAggregationExpressionTranslator.cs` — this dispatcher edit is required; without it the method is unreachable.** (That guidance reflects the dispatcher's current `switch`-on-name shape; if a future refactor converts it to a dictionary lookup or attribute-based registration, check the file before assuming this exact step still applies.) The per-method translator body should follow an existing method's pattern, but that's about body shape, not about whether you need the dispatch edit. The top-level dispatch is by method *name*; the per-method translators then verify the `MethodInfo` matches the canonical constants from `Reflection/`.
3. For **pipeline-level** operators (top-level LINQ methods like `Where`, `GroupBy`, `Skip`): create a translator under `ExpressionToPipelineTranslators/` instead and register it in the pipeline-level dispatcher `ExpressionToPipelineTranslator.Translate` (the `switch` on `Method.Name` at the top of that file).
4. Add a Jira-style integration test under `tests/MongoDB.Driver.Tests/Linq/Linq3Implementation/Jira/` asserting the rendered pipeline.

## Custom serializers in projections

If a `Select` produces a custom type, the `SerializerFinder` must know how to materialize it. Either register the serializer globally (`BsonSerializer.RegisterSerializer(...)`) or rely on class-map auto-mapping. For complex projections that cannot be expressed server-side, the finder falls back to `ClientSideProjectionHelper` — the `$project` includes all needed fields, and final shaping happens in the client.

## Debugging a translation

- Call `query.ToString()` on the queryable — it renders the resulting pipeline (or, on unsupported nodes, may throw `ExpressionNotSupportedException` carrying the offending sub-expression).
- `IMongoQueryProvider.LoggedStages` (populated after the query executes — returns `null` before execution) returns the rendered stages. The same property is also exposed directly on `MongoQuery<TDocument, TOutput>`, so you can read it off the query without an `IMongoQueryProvider` cast.
- For deeper debugging, set a breakpoint in `ExpressionToPipelineTranslator.Translate` and inspect `TranslationContext` (variable bindings, scope, current serializer).
- `ExpressionNotSupportedException` carries the offending sub-expression — read the message, not just the stack.

## Common pitfalls

- **Closure capture.** `PartialEvaluator` evaluates closures to constants before translation. If it misses one, the translator sees a `MemberExpression` it can't translate. Most fixes go in `PartialEvaluator` itself (an `internal static class` whose work is done by two `private class` nested `ExpressionVisitor`s — `Nominator` and `SubtreeEvaluator`), not the translator.
- **Projection vs document serialization** are different. A `Select(x => x.Name)` projection emits a `string` to BSON, not a `Person { Name = "…" }` wrapper. The `SerializerFinder` infers this; manually-registered serializers can break it if they assume document context.
- **Variable scoping.** `SelectMany`, `GroupBy`, and `$lookup` introduce nested scopes. `TranslationContext.SymbolTable` (see `Misc/SymbolTable.cs`) is the stack-shaped scope — check it when a translator misroutes a field reference.
- **Reference-equality on `MethodInfo`.** Open vs constructed generic methods compare unequal. Translators rely on the canonical `MethodInfo` constants from `Reflection/` — always dispatch through those.
- **Async vs sync terminals.** For dispatched terminals (`First` / `FirstAsync`, `Single` / `SingleAsync`, etc.) the sync and async sides route through the same `ExpressionToExecutableQueryTranslator` and, where applicable, parallel `Finalizers/` files for the sync vs `*Async` shape — translation is shared, finalization is per-variant. `ToList` / `ToListAsync` and `ToCursor` / `ToCursorAsync` are not in that dispatcher at all; they go through `IAsyncCursorSourceExtensions` on top of the same underlying cursor. Either way: don't add behavior to one variant without the other.
- **`ExpressionTranslationOptions`.** Two flags: `CompatibilityLevel` (typed `ServerVersion?`; targets a specific server version's translation behavior, and `null` — the default — means "current driver behavior, no version-specific compatibility shim"; the legal non-null values are the members of the `ServerVersion` enum at `src/MongoDB.Driver/ServerVersion.cs`), and `EnableClientSideProjections` (typed `bool?`; allow falling back to client-side post-projection — `null` = inherit driver default). When a previously-failing expression suddenly translates — or vice versa — check whether one of these changed.
- **`ToString()` reflects intent, not execution.** `query.ToString()` renders the pipeline; running the query may still fail at execution time on server-side validation.

## How to test

Most LINQ tests assert the **rendered pipeline** rather than executing against a server:

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~MongoDB.Driver.Tests.Linq.Linq3Implementation"
```

Pattern (new tests): extend `LinqIntegrationTest<TFixture>` with a `MongoCollectionFixture`, build a queryable, call `Translate(...)`, then `AssertStages(stages, "{ $match: { ... } }", "{ $project: { ... } }")`. `Linq3IntegrationTest` is the older base class still used by the existing test corpus — read it to understand the legacy pattern, but use the fixture-based `LinqIntegrationTest<TFixture>` for anything new. Hundreds of tests under `Linq/Linq3Implementation/Jira/` follow this pattern — read 2-3 before writing a new one.

## Boundaries

- **vs Aggregation fluent (`src/MongoDB.Driver/Aggregate*.cs`).** Both produce pipelines; LINQ generates them from expression trees, fluent generates them from explicit stage calls. Both bottom out at `AggregateOperation` indirectly — LINQ routes through `MongoQueryProvider` → `IMongoCollection.Aggregate`, which constructs the operation. Translators may borrow `PipelineDefinition` / `PipelineStageDefinition` types but never bypass them.
- **vs `Bson/Serialization`.** LINQ relies on the registered `IBsonSerializer<T>` for every materialized field; bugs frequently sit at the boundary (e.g., enum representation, custom converters).
- **vs `Core/Operations`.** LINQ stops at the rendered BSON pipeline. Cursor lifecycle, retry, and read-binding are owned by the operations layer — never call them directly.
