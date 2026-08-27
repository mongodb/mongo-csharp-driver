---
area: BSON & Serialization
scope: ["src/MongoDB.Bson/**/*.cs"]
reviewer-agent: bson-reviewer
adjacent-areas: [Driver/Linq, Driver/Encryption, Driver/Core/WireProtocol]
---

# MongoDB.Bson — AGENTS.md

The BSON library: object model, binary/JSON I/O, and the serialization framework. Foundation for everything else in the driver. Three primary subdirectories partition the work cleanly (additional folders like `Exceptions/` and various top-level files — `BsonExtensionMethods.cs`, `BsonUtils.cs`, etc. — also live here):

- `ObjectModel/` — the `BsonValue` hierarchy and value types
- `IO/` — readers, writers, byte buffers, JSON tokenization
- `Serialization/` — `IBsonSerializer<T>`, class maps, conventions, attributes, type-specific serializers

## Object model

- **Immutability is split.** `BsonValue` itself is abstract; leaf primitives (`BsonInt32`, `BsonString`, `BsonDouble`, `BsonBoolean`, `BsonObjectId`, `BsonDecimal128`, …) are immutable. Containers (`BsonDocument`, `BsonArray`) are **mutable**. `RawBsonDocument` / `RawBsonArray` are fully immutable wrappers over a raw byte buffer — prefer them for read-heavy paths where allocation matters.
- **Document structure.** `BsonDocument` keeps an ordered `List<BsonElement>` and lazily builds a name → index dictionary once the element count reaches `__indexesThreshold` (currently 8; see `BsonDocument.cs`). Insertion order is preserved.
- **Duplicate names.** With `AllowDuplicateNames = false` (default), adding an existing name throws. With it true, only the first occurrence is reachable by name; later duplicates are accessible only by integer index. Avoid relying on duplicates.
- **`ObjectId`** generation is spec-compliant: 4-byte timestamp + 5-byte process-scoped random (computed once at static-init — the same 5 bytes are embedded in every `ObjectId` produced by this process; they do not vary per ID) + 3-byte counter (`Interlocked.Increment(ref __staticIncrement) & 0x00ffffff`). See `ObjectId.cs` for the per-TFM seeding details if you're auditing the RNG.
- **`ObjectId` is not cryptographic.** Backed by `System.Random` (not a cryptographic RNG). If you need cryptographically unpredictable identifiers, mint them outside `ObjectId`.
- **`Decimal128` ↔ .NET `decimal`** is lossy in both directions: .NET `decimal` has a 96-bit mantissa (with sign + scale) inside a 128-bit representation, while `Decimal128` is IEEE 754-2008 decimal128 (34 significant decimal digits). Conversions outside the .NET `decimal` range overflow. When precision matters, stay in `Decimal128` end-to-end.
- **`BsonTimestamp` ≠ `BsonDateTime`.** Timestamp is the oplog/replication metadata type (BSON type 17); DateTime is BSON type 9. They sort differently, mean different things, and are easily confused.
- **Binary subtypes.** `BsonBinaryData` carries a subtype byte. The legacy `GuidRepresentationMode.V2` switch (and its accompanying global default) has been **removed** in this driver — there is no global GUID-representation default any more (see GUID notes below). The on-the-wire subtype is whatever the per-serializer / per-property `GuidRepresentation` resolves to: `Standard` maps to subtype 4 (new code); the legacy `CSharpLegacy`, `JavaLegacy`, `PythonLegacy` all map to subtype 3 (backward-compat reads).

## I/O

- **Reader/writer state machines.** `IBsonReader` / `IBsonWriter` enforce strict state transitions (`BsonReaderState`, `BsonWriterState`); calling out-of-order methods throws `InvalidOperationException`. When stuck, read the state — not the value.
- **`BsonBinaryReader` / `BsonBinaryWriter`** track positions on the underlying `BsonStream`. Documents are length-prefixed; truncated buffers throw rather than silently succeed. There is no resync.
- **JSON I/O.** `JsonReader` auto-detects extended JSON (e.g., `{"$oid": "..."}`); `JsonWriter` emits in the configured `JsonOutputMode` (`Shell`, `CanonicalExtendedJson`, `RelaxedExtendedJson`). Use `CanonicalExtendedJson` for round-trip; `Shell` is for human REPL output and is **not** parsed back losslessly.
- **Buffer ownership.** `IByteBuffer` and `BsonChunkPool` operate by acquire/release contract. If you didn't allocate the buffer, don't dispose it. Custom `Stream` adapters that bypass the pool will leak.

## Serialization framework

This is where most newcomer mistakes live.

- **`IBsonSerializer<T>` is required.** Implement the **generic** interface, not just the non-generic `IBsonSerializer`. Many internal lookups cast to `IBsonSerializer<T>` and fail without it. Inherit from `SerializerBase<T>` — it gives you `Serialize(BsonSerializationContext, BsonSerializationArgs, T)` and `Deserialize(BsonDeserializationContext, BsonDeserializationArgs)` to override.
- **Registry resolution.** `BsonSerializer.LookupSerializer` delegates to `BsonSerializerRegistry.GetSerializer`. The shape:
  - **Cache.** A single `ConcurrentDictionary` cache populated via `_cache.GetOrAdd(type, _createSerializer)`. Explicit registrations (`BsonSerializer.RegisterSerializer(...)`) and provider-produced serializers share that cache — there is no two-tier "explicit, then providers" lookup; explicit registrations win only because `RegisterSerializer` inserts the entry directly before any provider gets asked.
  - **Default provider order (LIFO).** On a cache miss, `CreateSerializer` walks the `ConcurrentStack<IBsonSerializationProvider>` and returns the first non-null result. Because it's a stack, iteration is **LIFO over registration order**. The seven default providers are pushed by `BsonSerializer`'s static constructor in this order: `BsonClassMapSerializationProvider`, `DiscriminatedInterfaceSerializationProvider`, `CollectionsSerializationProvider`, `PrimitiveSerializationProvider`, `AttributedSerializationProvider`, `TypeMappingSerializationProvider`, `BsonObjectModelSerializationProvider` — so `BsonObjectModelSerializationProvider` is consulted first and `BsonClassMapSerializationProvider` is consulted last. This list doubles as a spec — verify against `BsonSerializer`'s static constructor before relying on the exact order, since a future reorder there will silently rot this list.
  - **User-registered providers.** Pushed on top of that stack via `BsonSerializer.RegisterSerializationProvider`, so they are consulted before any of the defaults.

  Discriminators are consulted by polymorphic class-map serializers, not by the top-level lookup — read `BsonSerializerRegistry`, `BsonSerializer`, and the providers under `Serialization/Serializers/` if precise ordering matters.
- **`BsonClassMap`** auto-maps on first use of a CLR type. After the map is **frozen** (automatically on first serialize, or via `Freeze()`), mutation throws. The classic bug is calling `BsonClassMap.RegisterClassMap<T>(...)` after a `T` has already been auto-mapped — you must register **before** the type is first serialized.
- **`BsonMemberMap`** controls element name, default value, ignore-if-null/default, BSON representation, and per-member custom serializer.
- **Conventions.** `ConventionRegistry` maps a `Type` (or filter predicate) to a `IConventionPack` — an ordered list of conventions applied during auto-mapping by `ConventionRunner` (see `Serialization/Conventions/ConventionRunner.cs`). The `__defaults__` and `__attributes__` packs are registered separately and merged by `ConventionRegistry.Lookup` into one ordered combined pack returned to the runner. The default packs (see `ConventionRegistry.cs` and `DefaultConventionPack`) put the attribute conventions **last** in that combined order, so attributes win any conflict — it's an ordering effect within the merged pack, not "conventions first, then attributes." Register custom packs (with a filter and name) once at startup, before any serialization. Note: `ConventionRegistry` synchronizes its merge with a private `lock (__lock)` rather than going through `BsonSerializer.ConfigLock` (see the threading section below); registration paths in the two areas are coordinated by their own locks, not a single global lock.
- **Attributes.** `[BsonElement]`, `[BsonIgnore]`, `[BsonRepresentation]`, `[BsonDefaultValue]`, `[BsonId]`, `[BsonExtraElements]`, `[BsonDiscriminator]`, `[BsonGuidRepresentation]` (the actual attribute type is `BsonGuidRepresentationAttribute` in `Serialization/Attributes/`; sets per-property GUID representation and overrides any class-level or serializer-level setting — there is no global mode any more, see below). This list is illustrative, not exhaustive — see `Serialization/Attributes/` for the full set (e.g. `[BsonRequired]`, `[BsonIgnoreIfDefault]`, `[BsonIgnoreIfNull]`, `[BsonDateTimeOptions]`, `[BsonTimeSpanOptions]`, `[BsonNoId]`, `[BsonConstructor]`, `[BsonFactoryMethod]`, `[BinaryVector]`). Bind at class-map freeze time.
- **Polymorphism.** Discriminators land in the `_t` field by default. Use `[BsonDiscriminator(RootClass = true, Required = true)]` on the abstract base, and `[BsonKnownTypes(typeof(SubA), typeof(SubB))]` to make the subtypes register on first lookup. Without knowing about a concrete type, the deserializer can't materialize it. **Security:** when deserializing untrusted BSON into open polymorphic types, an attacker-controlled `_t` value can drive instantiation of any registered known type. `[BsonKnownTypes]` is one path that pre-registers concrete subtypes; the other is `DiscriminatedInterfaceSerializationProvider`, which resolves an interface-typed member to a concrete subtype based on the wire `_t` value at deserialization time — both ride on the same discriminator lookup, and both should be limited to subtypes you actually intend to accept.
- **`[BsonExtraElements]`** is the forward-compat escape hatch: a `Dictionary<string, object>` (or `BsonDocument`) member that absorbs unknown fields. Without it, schema evolution on the server side breaks the client immediately.

### Guid representation

Long-running migration. **There is no global GUID representation mode any more** — the legacy `GuidRepresentationMode.V2` / `V3` switch was removed, and representation must always be chosen per-serializer or per-property. `GuidSerializer` requires an explicit `GuidRepresentation`; constructing one with `GuidRepresentation.Unspecified` (or leaving a `Guid` property without an explicit representation) succeeds at construction time, but its `Serialize` / `Deserialize` methods **throw `BsonSerializationException`** on the first call.

For new code, do one of:

- Decorate `Guid` properties with `[BsonGuidRepresentation(GuidRepresentation.Standard)]` (subtype 4, bytes in RFC 4122 order — the in-memory `Guid` mixed-endian byte order is normalized at serialize time), **or**
- Register `new GuidSerializer(GuidRepresentation.Standard)` per type.

The common silent-corruption mode is **mismatched `GuidRepresentation` across serializers** — e.g. one service writes with `Standard`, another reads with `CSharpLegacy`, or a discriminator-selected polymorphic path picks a different representation than the writer. Anything reading documents written by older C# drivers may need `CSharpLegacy`; cross-language deployments need `Standard`.

## Threading & lifecycle

- BSON serialization is **thread-safe** for steady-state reads. `BsonSerializerRegistry` itself is backed by a `ConcurrentDictionary` plus a `ConcurrentStack` of provider lookups, so **cache-hit lookups are lock-free**. Cache misses still enter the provider-walk path and may trigger class-map freezing under `BsonSerializer.ConfigLock` — that's a `ReaderWriterLockSlim` that broadly coordinates config-time state across `BsonSerializer` (class-map freezing and serializer registration, plus discriminator-convention lookup, known-type registration, and assorted deserialization helpers all enter it). Treat it as the shared config-time lock, not a registration-only lock.
- Class maps and member maps freeze on first use. Configuration **must happen at startup** before anything is serialized.
- For very large documents, prefer `BsonBinaryReader` / `BsonBinaryWriter` over a stream rather than materializing as `BsonDocument`.

## Common pitfalls

- Implementing only the non-generic `IBsonSerializer` — typed lookups (`BsonSerializerRegistry.GetSerializer<T>`) do a hard cast to `IBsonSerializer<T>` and throw `InvalidCastException` rather than silently falling back.
- Calling `RegisterClassMap<T>` after `T` was auto-mapped — throws.
- Mixing attributes and explicit class-map registration without realizing attributes win at conflict (because `AttributeConventionPack` runs last in the default packs).
- Mismatched `GuidRepresentation` across services / serializers — round-trips silently corrupt; `Unspecified` throws.
- Decimal128 → .NET decimal precision loss / overflow.
- Forgetting `[BsonExtraElements]`; client breaks the moment the server adds a field.
- Forgetting to register a discriminator — polymorphic deserialization throws "no concrete type" at runtime, not compile.
- Mutating a frozen class map (e.g., late convention registration in test setup).
- Disposing a pooled `IByteBuffer` you didn't acquire — corrupts the pool.

## How to test

```bash
dotnet test tests/MongoDB.Bson.Tests/MongoDB.Bson.Tests.csproj -f net10.0
```

Single-area filters:

```bash
dotnet test tests/MongoDB.Bson.Tests/MongoDB.Bson.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~ObjectModel"

dotnet test tests/MongoDB.Bson.Tests/MongoDB.Bson.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Serialization"

dotnet test tests/MongoDB.Bson.Tests/MongoDB.Bson.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Jira"  # regression tests
```

BSON corpus / spec tests are JSON-driven via the runner under `tests/MongoDB.Driver.Tests/Specifications/bson-corpus/` (the runner lives in the Driver test project, not in `tests/MongoDB.Bson.Tests/`) and exercise the canonical, relaxed, and degenerate JSON forms across BSON types.

## Spec links

- BSON spec: `https://bsonspec.org/`
- Extended JSON / BSON corpus runner: `tests/MongoDB.Driver.Tests/Specifications/bson-corpus/`
