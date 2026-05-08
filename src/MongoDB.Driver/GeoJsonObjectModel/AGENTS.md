---
area: GeoJSON Object Model
scope: ["src/MongoDB.Driver/GeoJsonObjectModel/**/*.cs"]
reviewer-agent: geojson-reviewer
adjacent-areas: [Driver root (filter/index builders), Bson/Serialization]
---

# GeoJSON Object Model — AGENTS.md

Strongly-typed GeoJSON (RFC 7946) for use with MongoDB geospatial filters and indexes. Used by `FilterDefinitionBuilder.GeoIntersects` / `GeoWithin` / `Near` / `NearSphere`, `IndexKeysDefinitionBuilder.Geo2DSphere`, and the `$geoNear` aggregation stage. `PipelineStageDefinitionBuilder.GeoNear` exposes two `public static` convenience overloads — one taking a `GeoJsonPoint<TCoordinates>` and one taking a `double[]` — that both delegate to an `internal static` generic overload taking a `TPoint` directly. There is no `GeoNearOperation` class; geoNear is expressed as a pipeline stage.

## GeoJSON object types

Generic over a coordinate type `TCoordinates`. Two-tier hierarchy:

- **Geometries** — extend `GeoJsonGeometry<T>`, which itself extends `GeoJsonObject<T>`. The seven geometry types are `GeoJsonPoint<T>`, `GeoJsonLineString<T>`, `GeoJsonPolygon<T>`, `GeoJsonMultiPoint<T>`, `GeoJsonMultiLineString<T>`, `GeoJsonMultiPolygon<T>`, `GeoJsonGeometryCollection<T>`.
- **Features** — `GeoJsonFeature<T>` and `GeoJsonFeatureCollection<T>`. These extend `GeoJsonObject<T>` directly; they are **not** geometries and don't participate in the `GeoJsonGeometry<T>` hierarchy.

Geometries accept `GeoJsonObjectArgs<T>` (bounding box, CRS, extra members); `GeoJsonFeature<T>` accepts `GeoJsonFeatureArgs<T>`, a subclass of `GeoJsonObjectArgs<T>` that adds `Id` and `Properties`. The `args` parameter is **optional** on every geometry constructor (each type has both a `(coordinates)` and a `(args, coordinates)` overload) — omit it when you don't need a bounding box, CRS, or extra members.

## Coordinate types

- `GeoJson2DCoordinates` (raw `[x, y]`, no CRS opinion) — the driver does not prevent its use against a `2dsphere` index when `(x, y)` happen to be `(lon, lat)`, but `GeoJson2DGeographicCoordinates` is the documented choice so the lon/lat semantics are explicit at the type level. Note the two are **wire-compatible**: both render as a 2-element BSON array, so picking the "wrong" 2D type produces the same on-the-wire shape — the distinction is purely client-side type discipline. Discriminator routing is unaffected by this choice: coordinate types are not themselves serialized as standalone discriminated objects (they sit inside a geometry that carries the `type` discriminator), so swapping 2D coordinate flavours does not change how the enclosing geometry deserialises.
- `GeoJson2DGeographicCoordinates` (lon, lat) — most common
- `GeoJson2DProjectedCoordinates` (Easting, Northing)
- `GeoJson3DCoordinates`, `GeoJson3DGeographicCoordinates`, `GeoJson3DProjectedCoordinates`

All derive from `GeoJsonCoordinates`. Geometry instances, feature instances, and coordinate instances are immutable in the sense that their declared members have no public setters once constructed (coordinate types are not themselves geometries or features — they sit inside them). One caveat: `GeoJsonObject<T>` stores reference-typed `args` fields by reference (`_extraMembers = args.ExtraMembers`, `_coordinateReferenceSystem = args.CoordinateReferenceSystem`, `_boundingBox = args.BoundingBox` in the ctor). Of these, `ExtraMembers` is a `BsonDocument` which is plainly mutable; `CoordinateReferenceSystem` and `BoundingBox` are reference-held but their concrete types have no public mutators today — still, treat all three as **reference-shared** with the caller and don't mutate them post-construction (or rebuild the underlying instance). The `GeoJsonObjectArgs<T>` / `GeoJsonFeatureArgs<T>` carrier types passed to the geometry / feature constructors expose `{ get; set; }` properties and are mutable builders — don't keep mutating them after handing them to a constructor.

## Serializers

`GeoJsonObjectModel/Serializers/` — one per concrete type. There are **two** parallel discriminator dispatchers: `GeoJsonObjectSerializer<T>` (used when the static type is `GeoJsonObject<T>`) and `GeoJsonGeometrySerializer<T>` (used when the static type is constrained to `GeoJsonGeometry<T>`, e.g. inside a `GeometryCollection`). Each reads the `type` discriminator (`Point`, `LineString`, `Polygon`, …) via a `switch` over the discriminator string and routes to the right concrete serializer. Built on `ClassSerializerBase`. **Adding a new geometry type** requires three coordinated changes: (1) create the new geometry type and its dedicated serializer under `GeoJsonObjectModel/Serializers/`; (2) update the discriminator `switch` in both `GeoJsonObjectSerializer<T>` and `GeoJsonGeometrySerializer<T>` so the new `type` discriminator routes to it; and (3) annotate the new type with `[BsonSerializer(typeof(NewTypeSerializer))]` — this is the established pattern used by every existing geometry (see `GeoJsonObject.cs`, `GeoJsonPoint.cs`, …). An explicit `BsonSerializer.RegisterSerializer(typeof(NewType), new NewTypeSerializer())` at startup is only required as a fallback if you cannot decorate the type (e.g. third-party type you don't own). The dispatcher edit in step (2) handles the case where the caller statically holds a `GeoJsonObject<T>` / `GeoJsonGeometry<T>` reference and the discriminator decides the concrete subtype; step (3) handles the case where code holds the new type directly. Both paths are needed.

## Common pitfalls

- **[lon, lat] order.** GeoJSON is `[longitude, latitude]`, not `[lat, lon]`. The single most common bug. `2dsphere` index queries with swapped coords return wildly wrong results without erroring.
- **Polygon winding.** Follow the RFC 7946 right-hand rule — outer rings **counter-clockwise**, holes (inner rings) **clockwise** — for portability. Modern `2dsphere` is more lenient than older versions about winding (recent server releases interpret a non-CCW ring as the smaller of the two regions when no CRS is supplied), but relying on that lenience varies by server version and CRS, and reversed winding has historically caused queries to match the entire Earth, nothing, or fail at index/query time with `BadValue` (e.g. for self-intersecting or oversized rings). Validate with the `2dsphere` index against the server version you target, not just a generic GeoJSON validator.
- **Antimeridian crossing.** A polygon that crosses ±180° longitude must be split, or use `$geoWithin` with `$centerSphere` carefully on a `2dsphere` index — `2dsphere` interprets each ring on a sphere.
- **CRS confusion.** The driver sets **no** client-side CRS default — `CoordinateReferenceSystem` is `null` unless you supply one. (Older references to a "default WGS84 (degrees)" describe the *server-side* `2dsphere` assumption, not a client-side default; do not "fix" this paragraph back to say the driver defaults to WGS84.) Coordinate interpretation is up to the server-side index type: a `2dsphere` index assumes WGS84 lon/lat in degrees. Note that legacy `2d` indexes are **not** the target of this object model — they require legacy `[x, y]` arrays or embedded docs and do not consume GeoJSON documents. Projected coordinate systems require explicit CRS via `GeoJsonObjectArgs.CoordinateReferenceSystem`. Within a single geometry the driver's compile-time split (`GeoJson2DGeographicCoordinates` vs `GeoJson2DProjectedCoordinates` — sibling subclasses of `GeoJsonCoordinates` with no inheritance link, so neither is convertible to the other) prevents accidental mixing; the hazard is across *different* documents in the same collection (one stored as geographic, another as projected, both lacking CRS) — distance and area computations on that data will be silently wrong.
- **2D vs 3D types.** Choose at compile time. Mixing 2D and 3D in the same collection is legal at the BSON level but breaks most clients' assumptions. The third dimension (altitude) is ignored by all standard geospatial query operators.
- **Bounding boxes (`bbox`).** Optional. Server-side `2dsphere` does **not** key off the user-supplied `bbox` field — it computes its own S2 cell covering — so `bbox` is informational/client-side only (e.g., your code may use it for prefiltering). An inaccurate `bbox` cannot cause server-side spatial-index misses, but it can mislead any client-side filters that consult it.

## How to test

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~GeoJsonObjectModel"
```

Local MongoDB only; no environment variables. For any modified geometry type, include a round-trip serializer test that serializes to BSON and deserializes back. Prefer asserting **BSON-byte equality** (compare the raw bytes / `BsonDocument` against an expected document) — it catches element-ordering and representation drift that a field-level deep-equality check would miss.

## Spec links

- GeoJSON RFC 7946: `https://datatracker.ietf.org/doc/html/rfc7946`
- MongoDB geospatial server operators: `$geoIntersects`, `$geoWithin`, `$near`, `$nearSphere`, `2dsphere` index (the corresponding `FilterDefinitionBuilder` methods are `GeoIntersects` / `GeoWithin` / `Near` / `NearSphere`)
