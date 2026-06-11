---
area: GridFS
scope: ["src/MongoDB.Driver/GridFS/**/*.cs"]
reviewer-agent: gridfs-reviewer
adjacent-areas: [Driver/Core/Operations, Bson/Serialization]
---

# GridFS — AGENTS.md

GridFS stores binary files in two collections per bucket: `<bucket>.files` (metadata, one document per file) and `<bucket>.chunks` (binary data, many documents per file). Chunk size is configurable; default 255 KiB.

## Public API

- `IGridFSBucket<TFileId>` / `GridFSBucket<TFileId>` — generic over the file ID type. The non-generic `IGridFSBucket` / `GridFSBucket` is `ObjectId`-keyed.
- Operations: `UploadFromStream`, `UploadFromBytes`, `DownloadToStream`, `DownloadAsBytes`, `DownloadAsBytesByName`, `DownloadToStreamByName`, `OpenUploadStream`, `OpenDownloadStream`, `OpenDownloadStreamByName`, `Find`, `Delete`, `Drop`, `Rename`. All have sync + async pairs. Return shapes worth flagging: `Find` returns an `IAsyncCursor<GridFSFileInfo<TFileId>>` (`FindAsync` returns a `Task<…>` of that), `DownloadAsBytes*` returns a `byte[]`, and `OpenUploadStream*` / `OpenDownloadStream*` return the abstract stream types described below — the upload side returns a `GridFSUploadStream<TFileId>` whose `Id` property hands back the allocated `files_id`.
- Streams: the public types are the **abstract** `Stream` subclasses `GridFSUploadStream<TFileId>` and `GridFSDownloadStream<TFileId>`, **plus** their non-generic compat shims `GridFSUploadStream : DelegatingStream` and `GridFSDownloadStream : DelegatingStream` (declared in `GridFSUploadStreamCompat.cs` / `GridFSDownloadStreamCompat.cs`) — these wrap the `ObjectId`-keyed bucket and are part of the public surface, paralleling the `GridFSBucket` / `GridFSBucket<TFileId>` compat split. The concrete runtime implementations — `GridFSForwardOnlyUploadStream<TFileId>` for uploads, and either `GridFSForwardOnlyDownloadStream<TFileId>` (default) or `GridFSSeekableDownloadStream<TFileId>` (selected via `new GridFSDownloadOptions { Seekable = true }`) for downloads — are all `internal` and not part of the public surface.
- File metadata: `GridFSFileInfo<TFileId>` (with `GridFSFileInfoSerializer<TFileId>`).
- Options: `GridFSBucketOptions` (bucket name, `ChunkSizeBytes`, read/write concern, read preference); `GridFSUploadOptions` (`ChunkSizeBytes` override — same property name as the bucket-level default — plus custom `Metadata` and `BatchSize` (chunks per bulk insert into `.chunks`)); `GridFSDownloadOptions` (`Seekable`); `GridFSDownloadByNameOptions` extends it with `Revision`; `GridFSFindOptions<TFileId>` (with a non-generic `GridFSFindOptions : GridFSFindOptions<ObjectId>` compat alias for the `ObjectId`-keyed bucket).

## Architecture

`GridFSBucket` holds the cluster reference, an `IBsonSerializer<GridFSFileInfo<TFileId>>` (`_fileInfoSerializer`), and a `BsonSerializationInfo` for the `_id` field (`_idSerializationInfo`, which is what enforces the `TFileId` round-trip discussed in the pitfalls below). Mutating operations (`Delete`, `Rename`, `OpenUploadStream`, `Drop`) bind via `GetSingleServerReadWriteBinding`; reads (`DownloadAsBytes*`, `DownloadToStream*`, `OpenDownloadStream*`, `Find`) bind via `GetSingleServerReadBinding`. Either way, a single upload/download streams its chunks to the same server (avoids cross-server consistency surprises mid-operation). Note: `Find` returns an `IAsyncCursor`, and the single-server property holds for the **initial query** only — once the cursor's binding is disposed, subsequent `getMore` calls can be routed independently by the operations layer, so the "same server" guarantee does not extend across the cursor's full lifetime.

Indexes (`{filename:1, uploadDate:1}` on `.files`, `{files_id:1, n:1}` unique on `.chunks`) are created lazily on the first upload **only if the files collection is empty** (via an `IsFilesCollectionEmpty` check). The `bool` flag `_ensureIndexesDone` is the actual one-shot guarantor — once set, the short-circuit makes the path a single no-op for the rest of the bucket's lifetime. The accompanying `SemaphoreSlim` (`_ensureIndexesSemaphore`) only serializes concurrent first-upload callers so they don't race on the `IsFilesCollectionEmpty` check; without the `bool` flag, the semaphore alone would let every caller re-enter the slow path.

`Delete` can leave **orphan chunks** if interrupted between its two operations — that is the user-visible hazard, so handle it first. The implementation removes the `.files` document first (GridFS calls `new DeleteRequest(filter)` with no explicit `Limit`; the `DeleteRequest` constructor's default `Limit=1` is what caps the match to a single `.files` document — there is no `Limit = 1` literal in the GridFS code itself), then — after that `.files` delete returns (whether it matched anything or not) — unconditionally removes any matching chunks. The chunks delete uses an explicit `Limit = 0` to remove every matching chunk for the file (the deliberate asymmetry vs. the singular `.files` delete). There is no `try/catch` around either call: an exception from the `.files` delete simply propagates and the chunks delete never runs. After both calls succeed, if the `.files` deletion matched zero documents, `GridFSFileNotFoundException` is thrown. The orphan window is any interruption between the two operations — a network error during the chunks delete, but equally a process crash after the `.files` delete succeeds and before the chunks call is issued. `Drop` is the only safe full cleanup.

There is no GridFS-specific JSON spec runner in this repo at present (no `tests/MongoDB.Driver.Tests/Specifications/gridfs/` directory). GridFS coverage lives in the regular xUnit tests under `tests/MongoDB.Driver.Tests/GridFS/`, plus the JSON-driven runners pulled in by other specs: `tests/MongoDB.Driver.Tests/JsonDrivenTests/JsonDrivenGridFs*.cs` and `tests/MongoDB.Driver.Tests/UnifiedTestOperations/UnifiedGridFs*Operation.cs` exercise GridFS as part of the unified / JSON-driven runners used by transactions, retryable reads/writes, etc.

## Common pitfalls

- **Generic file-ID mismatch.** If you pick a custom `TFileId` (not `ObjectId`), make sure a serializer is registered. Mismatched serializers silently corrupt the `_id` field on `.files`.
- **Chunk size vs throughput.** The default 255 KiB optimizes for typical web payloads. Very large files with the default size cause many round-trips; bump `ChunkSizeBytes` (must stay well below the 16 MiB BSON document limit, since each chunk is a BSON document) for large-file workloads.
- **Orphaned chunks on interrupted upload.** `OpenUploadStream` allocates a `files_id`; aborted uploads (exception, cancellation) leave the chunk documents but no metadata. There is no automatic cleanup *unless the caller invokes* `GridFSUploadStream<TFileId>.Abort` / `AbortAsync` (public abstract on the upload stream, implemented by `GridFSForwardOnlyUploadStream`), which deletes any chunks already written for the in-flight `files_id`. If the upload is interrupted before `Abort` is called — exception caught and swallowed, process crash, unmanaged cancellation — the chunks survive. Log + reconcile, or use `Drop` for periodic cleanup of test buckets.
- **Concurrent indexes.** First write under load can serialize on the index-creation semaphore. After indexes exist, the cost vanishes — but in fresh test environments expect a one-time delay.
- **Revision semantics.** The `DownloadAsBytesByName` / `DownloadToStreamByName` / `OpenDownloadStreamByName` family takes a `GridFSDownloadByNameOptions` whose `Revision` property defaults to `-1` (newest, matching the GridFS spec's default-revision semantics), so leaving the property at its default behaves identically to setting it explicitly to `-1`. Passing `options: null` likewise selects newest, since the bucket constructs a default `GridFSDownloadByNameOptions` internally. `Revision = 0` is "oldest". Off-by-one mistakes produce wrong-file failures that look like data corruption — read the spec.
- **`Stream` ownership.** Upload/download streams returned by the bucket must be disposed by the caller (`using`). Disposing closes the cursor and finalizes the upload metadata.

## How to test

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~GridFS"
```

GridFS uses a local MongoDB only — no extra environment variables required.

## Boundary with operations-reviewer

Chunk-level bulk writes into `.chunks`, the underlying `find` cursors over `.files` / `.chunks`, and retry semantics for the underlying CRUD operations belong to **operations-reviewer** (`Core/Operations/AGENTS.md`). Bucket orchestration — upload/download stream lifecycle, the index-creation bootstrap, `Abort` semantics, and the two-step `Delete` ordering described above — belongs here.

## Spec links

- GridFS spec: `https://github.com/mongodb/specifications/tree/master/source/gridfs`
