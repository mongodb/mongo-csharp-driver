---
area: MongoDB.Driver — public API surface (router)
scope: ["src/MongoDB.Driver/*.cs"]
reviewer-agent: [client-api-reviewer, builders-reviewer, aggregation-reviewer]
adjacent-areas: [Core/Operations, Core, Linq, GridFS, Search, Encryption, GeoJsonObjectModel, Authentication]
---

# MongoDB.Driver — AGENTS.md (router)

This file is the entry point for any work under `src/MongoDB.Driver/`. Over 200 `.cs` files at this directory root are predominantly the **public API surface** — client facades, builders DSL, fluent APIs, and option/result types — though a meaningful minority (~20%) are `internal` helpers that happen to live alongside it. Public API is **not confined to this directory**: `Core/`, `Core/Events/`, `Core/Logging/`, `GridFS/`, `Search/`, `Linq/`, `GeoJsonObjectModel/`, and the `MongoDB.Bson` project also expose public types. Subdirectories own their own deep concerns and have their own `AGENTS.md`. **This file holds substantive guidance only for code that actually lives at the root.**

## Area router

| If you're editing… | See |
|---|---|
| `Core/Clusters/`, `Core/Servers/`, `Core/Connections/`, `Core/ConnectionPools/`, `Core/WireProtocol/`, `Core/Compression/`, `Core/Configuration/`, `Core/Misc/` | `Core/AGENTS.md` (Connection & Transport / SDAM) |
| `Core/Operations/`, `Core/Bindings/`, plus `ClientSession*.cs`, `CoreSession*` | `Core/Operations/AGENTS.md` (Operations, Sessions & Transactions) |
| `Authentication/` | `Authentication/AGENTS.md` |
| `Linq/`, `Linq/Linq3Implementation/` | `Linq/AGENTS.md` (LINQ provider) |
| `GridFS/` | `GridFS/AGENTS.md` |
| `Search/` | `Search/AGENTS.md` (Atlas Search & Vector Search) |
| `Encryption/` (driver-side glue) | `Encryption/AGENTS.md` and the sibling `src/MongoDB.Driver.Encryption/AGENTS.md` (libmongocrypt) |
| `Core/Events/`, `Core/Logging/` | `Core/Events/AGENTS.md` and `Core/Logging/AGENTS.md` |
| `GeoJsonObjectModel/` | `GeoJsonObjectModel/AGENTS.md` |

The substantive sections below cover only files that live in `src/MongoDB.Driver/` directly.

## Scope (root files)

User-facing facades, fluent APIs, builders, and option/result records. The OperationExecutor at this layer dispatches to `Core/Operations/`; everything below the wire is in `Core/`.

## Client facades & settings

- `MongoClient.cs` / `IMongoClient.cs` — top-level entry. Constructors: parameterless (defaults to `mongodb://localhost`), or one accepting `MongoClientSettings`, `MongoUrl`, or a connection string. Holds the cluster reference; `Dispose()` shuts down connections.
- `IMongoDatabase.cs` / `MongoDatabase.cs`, `IMongoCollection.cs` / `MongoCollectionImpl.cs` (defining `MongoCollectionImpl<TDocument>`) — derived from the client; carry their own settings (read/write concern, read preference, serializer registry). The public abstractions are the interfaces `IMongoDatabase` and `IMongoCollection<TDocument>`; `MongoDatabase`, `MongoCollectionBase<TDocument>`, and `MongoCollectionImpl<TDocument>` are all `internal`.
- Settings types — `MongoClientSettings`, `MongoDatabaseSettings`, `MongoCollectionSettings`, `SslSettings`, `MongoUrl`, `MongoUrlBuilder`, `MongoServerAddress`, `MongoCredential` (and identity subtypes), `AutoEncryptionOptions`, `ClusterKey`. (`ServerApi` is part of the same surface — surfaced via `MongoClientSettings.ServerApi` — but the type lives at `Core/ServerApi.cs`, not at the driver root.) **`MongoUrl` vs `MongoUrlBuilder`:** `MongoUrl` is immutable (parsed from a connection string, exposes only getters and a `ToString()` round-trip); `MongoUrlBuilder` is the mutable builder counterpart used to assemble or mutate a connection string before calling `ToMongoUrl()` for the immutable form.

**Settings freeze invariant.** All `*Settings` types are mutable until `Freeze()` is called. The driver freezes settings when they're passed into a client/database/collection. After that, mutation throws. Treat settings as immutable once handed to the framework — clone before mutating.

**`OperationExecutor` / `IOperationExecutor`** — `internal` seam between facades and `Core/Operations` (defined in `IOperationExecutor.cs` and `OperationExecutor.cs`; not part of the public API). All public methods bottom out by constructing a `Core/Operations` operation and handing it to the executor. Don't bypass it; retry, session attachment, and command-monitoring hooks live there.

## Builders DSL

Hub: `Builders<TDocument>` (`Builders.cs`) exposes the six core builder properties — listed alphabetically to match the source: `Filter`, `IndexKeys`, `Projection`, `SetFields`, `Sort`, `Update` — plus the Atlas Search builder hubs (`Search`, `SearchPath`, `SearchScore`, `SearchScoreFunction`, `SearchFacet`, `SearchSpan`). Each property returns a builder that produces an immutable `*Definition<TDocument>`.

The same `*Definition` / `*DefinitionBuilder` pattern is used by `FilterDefinition*`, `UpdateDefinition*`, `ProjectionDefinition*`, `SortDefinition*`, `IndexKeysDefinition*`, `SetFieldDefinitions*`. **`SetFields` is the odd one out** in that family: the builder is singular (`SetFieldDefinitionsBuilder<T>`) but the definition it produces is plural (`SetFieldDefinitions<T>`, a collection). Don't grep for a non-existent `SetFieldDefinitionBuilder`. `PipelineDefinition*` and `PipelineStageDefinition*` follow the same shape, but their helpers are the standalone static classes `PipelineDefinitionBuilder` / `PipelineStageDefinitionBuilder` rather than properties on `Builders<T>`. `FieldDefinition` and `ArrayFilterDefinition` are definition-only — polymorphic subtypes are constructed directly, with no builder hub. For fields specifically, there are two parallel generic shapes: `FieldDefinition<T>` (untyped field) with `StringFieldDefinition<T>` / `ExpressionFieldDefinition<T>` subclasses, and `FieldDefinition<T, TField>` (typed field, used when the projected field's CLR type matters) with `StringFieldDefinition<T, TField>` / `ExpressionFieldDefinition<T, TField>` subclasses. Renders bottom out in `RenderedFieldDefinition` / `RenderedFieldDefinition<TField>`.

- A `*Definition<T>` is an abstract immutable that knows how to `Render(RenderArgs<T>)` to BSON. Most types render to a `BsonDocument`, with three notable exceptions. `UpdateDefinition<T>.Render` returns the broader `BsonValue` because pipeline-form updates render as a `BsonArray`; cast accordingly when asserting against rendered output. The two-generic `ProjectionDefinition<TSource, TProjection>.Render` returns `RenderedProjectionDefinition<TProjection>` (a wrapper of `BsonDocument` plus the projection-result serializer); only the single-generic `ProjectionDefinition<TSource>.Render` returns a bare `BsonDocument`. `PipelineDefinition<TInput, TOutput>.Render` returns `RenderedPipelineDefinition<TOutput>` (a wrapper of `IReadOnlyList<BsonDocument>` plus the output serializer), not a `BsonDocument`. **Signature outlier:** `ArrayFilterDefinition<TItem>.Render` does *not* take `RenderArgs<T>` — it exposes two overloads, the non-generic `Render(IBsonSerializer itemSerializer, IBsonSerializerRegistry serializerRegistry)` declared on the base `ArrayFilterDefinition` and the typed `Render(IBsonSerializer<TItem> itemSerializer, IBsonSerializerRegistry serializerRegistry)` on `ArrayFilterDefinition<TItem>` (see `ArrayFilterDefinition.cs:46` and `:103`). Don't waste time grepping for a `RenderArgs` overload on it.
- A `*DefinitionBuilder<T>` constructs concrete definitions via fluent methods.
- Implicit conversions vary by definition type. Most accept `BsonDocument` and `string` (parsed as JSON — a frequent overload-ambiguity hazard); the `FieldDefinition` family accepts only `string`; `SetFieldDefinitions<T>` accepts nothing. The full matrix:

  | Definition type | `BsonDocument` | `string` (JSON) | Other implicit conversions |
  |---|---|---|---|
  | `FilterDefinition<T>` | ✅ | ✅ | `Expression<Func<T, bool>>` |
  | `UpdateDefinition<T>` | ✅ | ✅ | `PipelineDefinition<T, T>` (pipeline-update form) |
  | `ProjectionDefinition<TSource>` | ✅ | ✅ | — |
  | `ProjectionDefinition<TSource, TProjection>` | ✅ | ✅ | `ProjectionDefinition<TSource>` (up-cast adding projection-result type) |
  | `SortDefinition<T>` | ✅ | ✅ | — |
  | `IndexKeysDefinition<T>` | ✅ | ✅ | — |
  | `ArrayFilterDefinition<TItem>` | ✅ | ✅ (JSON) | — |
  | `FieldDefinition<T>` | ❌ | ✅ (treated as a **literal field name** via `StringFieldDefinition<T>` — *not* JSON-parsed; this is the asymmetry vs. the other definitions' string conversions, and it applies family-wide — `FieldDefinition<TDocument, TField>` below behaves the same way) | — |
  | `FieldDefinition<TDocument, TField>` | ❌ | ✅ (also a literal field name) | up-cast **to** `FieldDefinition<TDocument>` (drops field-CLR-type parameter) |
  | `PipelineDefinition<TInput, TOutput>` | ❌ | — | `IPipelineStageDefinition[]`, `List<IPipelineStageDefinition>`, `BsonDocument[]`, `List<BsonDocument>` (arrays/lists of `BsonDocument`, **not** a singular `BsonDocument` — wrap a single doc in `new[] { doc }` to use the array conversion) |
  | `SetFieldDefinitions<T>` (plural, the collection type exposed via `Builders<T>.SetFields`; distinct from the singular `SetFieldDefinition<T>`, no `s`) | ❌ | ❌ | — |

  The four `PipelineDefinition<TInput, TOutput>` array/list conversions are a known overload-resolution hazard: a method overloaded on both `PipelineDefinition<…>` and one of those collection shapes may resolve to either depending on the call site. `Expression<Func<T, bool>>` is implicit-convertible **only** on `FilterDefinition<T>`; other definitions accept LINQ expressions exclusively via builder methods.

**Render is where bugs hide.** `Render` receives `RenderArgs<TDocument>` carrying the active serializer registry, document serializer, and translation options. A definition rendered with the wrong document serializer produces silently-wrong BSON — never compile errors. When changing builder behavior, add a test that compares the rendered BSON, not just round-trip behavior.

**Pitfall: lambda overload ambiguity.** Builders have many `Expression<Func<…>>` overloads. The C# compiler sometimes picks the wrong one — be explicit with type parameters when refactoring.

## Aggregation fluent API

- `IAggregateFluent<TResult>` / `AggregateFluentBase<TResult>` (public abstract) / `AggregateFluent<TInput,TResult>` (`internal abstract`) — fluent stage chaining over a `PipelineDefinition`.
- Stage options and result records: `AggregateOptions` (consult `AggregateOptions.cs` for the full list of properties; load-bearing ones include `AllowDiskUse`, `BatchSize`, `Comment`, `Hint`, `MaxTime`, `MaxAwaitTime`, `Collation`, `Let`, `BypassDocumentValidation`, and `TranslationOptions`; **`UseCursor` is `[Obsolete]`** and new code should not propagate it to any new API surface), `AggregateBucket*`, `AggregateFacet*`, `AggregateGraphLookupOptions`, `AggregateLookupOptions`, `AggregateUnwindOptions`. `AggregateHelper` is `internal static` (a render helper), not part of the public surface. `MaxAwaitTime` (here on `AggregateOptions`, mirrored on `ChangeStreamOptions`) is honored only by **tailable / change-stream** cursors (it bounds the server-side wait on `getMore`); it is **inert** for ordinary aggregation cursors — see `AggregateOptions.cs` for the property definition and surrounding context. Don't set it expecting it to bound a regular aggregation.
- `AggregateExpressionDefinition<TSource, TResult>` is separate — an aggregation-expression abstraction with concrete subclasses `BsonValueAggregateExpressionDefinition<TSource, TResult>`, `ExpressionAggregateExpressionDefinition<TSource, TResult>`, and `DocumentsAggregateExpressionDefinition<TDocument>`. The last is the input shape for `$documents` and is consumed via the `PipelineStageDefinitionBuilder.Documents(...)` helper, which produces the first stage on a client- or database-level `Aggregate(...)`; there is no collection-level fluent equivalent (collection-level aggregates run against the collection itself). Used by stage builders that take expression arguments, not itself a stage option.
- Bottoms out in `Core/Operations/AggregateOperation<T>` and `AggregateToCollectionOperation` (for `$out` / `$merge`).

Boundary with `operations-reviewer`: this layer owns **stage shape and pipeline semantics**. The Operations layer owns **cursor lifecycle, retry, and binding selection**.

## Change streams

- `ChangeStreamOptions`, `ChangeStreamPreAndPostImagesOptions`, `ChangeStreamStageOptions`. `ChangeStreamHelper` is `internal static` (an internal render helper), not part of the public surface.
- Entry points `IMongoClient.Watch` (cluster-wide), `IMongoDatabase.Watch` (db-wide), `IMongoCollection.Watch` (collection-scoped). Bottoms out in `Core/Operations/ChangeStreamOperation` and `ChangeStreamCursor`.

The change-stream pipeline input type is `ChangeStreamDocument<BsonDocument>` for client- and database-level watches and `ChangeStreamDocument<TDocument>` for collection-level watches; only the output type is user-defined. Stages that materialize results (`$out`, `$merge`) are rejected by the **server** in a change-stream pipeline — the driver does not validate this client-side. If client-side validation is ever added, treat the change in *which* stages are legal as a SemVer-relevant behavior change, not a pure bug-fix.

**Resume tokens are opaque.** The resume token round-trips as an opaque `BsonDocument` — `ResumeAfter` / `StartAfter` accept whatever shape the server emitted in `_id` on a prior change event. Do not normalize, reshape, prune fields from, or stringify-and-reparse the token: the server treats it as a black box, and any client-side rewrite risks producing a token the server will reject or — worse — that points to a different resume position.

## Sessions surface

- Public: `IClientSessionHandle` (declared in `IClientSession.cs` alongside `IClientSession`); the implementation `ClientSessionHandle` (in `ClientSessionHandle.cs`) is `internal sealed`. `ClientSessionOptions` is public; `AbortTransactionOptions` and `CommitTransactionOptions` are currently `internal sealed` — the user-facing public abstraction is just `ClientSessionOptions`. **The `internal` visibility on these two options types is gated on CSOT GA**; expect them to be promoted to `public` (a SemVer-additive change) when CSOT ships, so don't bake assumptions about their internal-ness into long-lived code.
- `ClientSessionHandle` wraps `ICoreSessionHandle` (declared in `Core/Bindings/ICoreSession.cs`; the implementation is `CoreSessionHandle` in `Core/Bindings/CoreSessionHandle.cs`, which wraps the underlying `CoreSession` from `Core/Bindings/CoreSession.cs`).

**Ownership boundary.** The `client-api-reviewer` owns *only* the surface-level interface (constructors, properties, `IMongoClient.StartSession*`). Session state, transaction state machine, cluster time, server pinning, and retryable-transaction semantics belong to the **operations-reviewer** (see `Core/Operations/AGENTS.md`).

## Find fluent API

- `IFindFluent<TDocument,TProjection>` / `FindFluentBase<TDocument,TProjection>` (public abstract) / `FindFluent<TDocument,TProjection>` (`internal`) — chain Skip/Limit/Sort/Project/etc.
- `FindOptions<TDocument,TProjection>`, `FindOneAndUpdateOptions`, `FindOneAndDeleteOptions`, `FindOneAndReplaceOptions`.
- `Count` is `[Obsolete]` on `IFindFluent` — use `CountDocuments` on the fluent surface. (`EstimatedDocumentCount` lives on `IMongoCollection<T>`, not on the find-fluent surface.) Per the SemVer rules in **Public-API change discipline** below, removing the obsolete `Count` requires a major-version bump even though it has carried `[Obsolete]` for several release cycles.

## Bulk write — two layers

Two parallel surfaces, do **not** mix them.

- **Collection-level** (`IMongoCollection<T>.BulkWriteAsync`): single collection. Models derive from `WriteModel<T>` (`InsertOneModel<T>`, `UpdateOneModel<T>`, `UpdateManyModel<T>`, `DeleteOneModel<T>`, `DeleteManyModel<T>`, `ReplaceOneModel<T>`). Result: `BulkWriteResult<TDocument>`. Failure: `MongoBulkWriteException<TDocument>` (the non-generic `MongoBulkWriteException` is the abstract base — `catch` blocks should target the sealed generic).
- **Client-level** (`IMongoClient.BulkWriteAsync`, MongoDB 8.0+): cross-collection. Models derive from `BulkWriteModel`. Result: `ClientBulkWriteResult`. Failure: `ClientBulkWriteException` (which carries partial results).

## Index management

- `IMongoIndexManager<T>` / `MongoIndexManagerBase<T>` for normal indexes.
- `CreateIndexModel<T>`, `CreateIndexOptions` — `Unique`, `Sparse`, `Background`, TTL (`ExpireAfter`), partial filter, collation.
- Atlas Search index helpers: `CreateSearchIndexModel`, `SearchIndexType`. Vector-search index models share a common base — `CreateVectorSearchIndexModelBase<TDocument>` — with concrete subclasses `CreateVectorSearchIndexModel<TDocument>` and `CreateAutoEmbeddingVectorSearchIndexModel<TDocument>`. Tests for these are gated by `ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED` (see root `AGENTS.md`).

## Vector search surface

- `QueryVector`, `BinaryVectorExtensions`, `VectorSearchOptions`, `VectorEmbeddingModality`, `VectorIndexingMethod`, `VectorQuantization`, `VectorSimilarity`.
- The fluent search builders themselves live under `Search/` — see `Search/AGENTS.md`.

## Exceptions

`MongoWriteException`, `MongoBulkWriteException`, `ClientBulkWriteException` derive from the Core exception hierarchy. `WriteError`, `WriteConcernError`, `BulkWriteError` carry per-operation failure details. Network/transient errors are surfaced via the Core layer (`MongoConnectionException`, `MongoServerException`, etc.) — see `Core/AGENTS.md`.

## Public-API change discipline

The driver root is a major SemVer surface — but not the only one. Public types also live under `Core/`, `Core/Events/`, `Core/Logging/`, `GridFS/`, `Search/`, `Linq/`, `GeoJsonObjectModel/`, and the `MongoDB.Bson` project. Treat additions as load-bearing; treat changes (signature, behavior, default options) as breaking, wherever they occur. Specifically:

- Adding overloads is fine; changing an existing default value is not.
- Renaming a property on a `*Options` record is breaking.
- Builders' `Render` output is a behavior contract — pipeline shape changes break user expectations even if the C# signature is unchanged. (LINQ3 translator changes are a separate exception class: pipeline-shape evolution there is governed by the LINQ provider's own translation-stability rules, not the builders contract — see `Linq/AGENTS.md`.)
- New methods on `IMongoClient` / `IMongoDatabase` / `IMongoCollection` break implementers (mocks, test doubles).
- **`Freeze()` timing is a behavioral contract.** Settings types (`MongoClientSettings`, `MongoDatabaseSettings`, `MongoCollectionSettings`, `SslSettings`, etc.) freeze when handed to the framework. Moving the freeze point earlier — or later — can break callers that currently mutate post-construction, even when no signature changes.

When in doubt, add an `[Obsolete]` overload first and route through the new path.

## How to test (root-level changes)

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~MongoCollectionImplTests|FullyQualifiedName~FilterDefinitionBuilderTests|FullyQualifiedName~UpdateDefinitionBuilderTests|FullyQualifiedName~ProjectionDefinitionBuilderTests|FullyQualifiedName~SortDefinitionBuilderTests|FullyQualifiedName~IndexKeysDefinitionBuilderTests|FullyQualifiedName~PipelineDefinitionBuilderTests|FullyQualifiedName~AggregateFluentTests"
```

For builder rendering specifically: most tests under `tests/MongoDB.Driver.Tests/` at the root assert against rendered BSON via `.Render(...)` rather than executing against a server — fast and deterministic. Prefer adding to that pattern.

JSON-driven CRUD spec tests in `tests/MongoDB.Driver.Tests/Specifications/crud/` exercise the public API end-to-end — see `tests/MongoDB.Driver.Tests/Specifications/AGENTS.md`.

## Common pitfalls

- Mutating settings after they've been handed to a `MongoClient`/`MongoDatabase`/`MongoCollection`. Frozen → throws.
- Passing the wrong `TDocument` to a builder, then rendering produces silently-wrong BSON. Tests should assert rendered BSON.
- Confusing `WriteModel<T>` (collection bulk) with `BulkWriteModel` (client bulk). Different result types and different exception types.
- Forgetting that change-stream pipelines cannot end with materializing stages — the **server** rejects `$out`/`$merge` in this position; the driver does not validate it.
- Adding a new method to `IMongoCollection<T>` without considering test doubles in user code — prefer extension methods on `IMongoCollectionExtensions` (a `public static class`, paralleling `IMongoClientExtensions`) when the new functionality can compose existing methods. **Naming note:** this codebase keeps the `I` prefix on the type itself, not just the file name — the `I`-prefixed name on a non-interface is the established convention here, so match it.
- Marking sync methods `Obsolete` without keeping the async version — sync APIs are still load-bearing for many consumers.
