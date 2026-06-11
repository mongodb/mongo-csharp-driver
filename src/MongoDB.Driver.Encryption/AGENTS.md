---
area: Client-Side Encryption (libmongocrypt wrapper)
scope: ["src/MongoDB.Driver.Encryption/**/*.cs"]
reviewer-agent: encryption-reviewer
adjacent-areas: [Driver/Encryption (in-driver glue), Driver/Core/WireProtocol, Bson/Serialization]
---

# MongoDB.Driver.Encryption — AGENTS.md

This project wraps **libmongocrypt** (the C library that implements CSFLE and Queryable Encryption) and exposes both an explicit-encryption API (`ClientEncryption`) and the auto-encryption hook used by the main driver. Anything cryptographic happens here. The driver's user-facing `AutoEncryptionOptions` and provider registry live one layer up under `src/MongoDB.Driver/Encryption/` (see that AGENTS.md).

## Public API

- `ClientEncryption` — explicit encryption surface, incl. `CreateDataKey`, `RewrapManyDataKey`, `Encrypt`, `EncryptExpression`, `Decrypt`, `GetKey`, `GetKeyByAlternateKeyName`, `AddAlternateKeyName`, `RemoveAlternateKeyName`, `DeleteKey`, `GetKeys`, `CreateEncryptedCollection`. All have sync + async pairs and accept a `CancellationToken`. Backed by `ExplicitEncryptionLibMongoCryptController`.
- `ClientEncryptionOptions` — `KeyVaultClient` (typically a separate `IMongoClient` for the key vault), `KeyVaultNamespace`, `KmsProviders` (per-provider credentials), `TlsOptions`, `KeyExpiration` (DEK cache TTL; the C# property defaults to `null`, which causes libmongocrypt to apply its 60-second default; `Zero` = never expire). Validates KMS option values are `byte[]` or `string`; rejects per-provider TLS settings that supply a `ServerCertificateValidationCallback` (insecure-by-construction). Other `SslSettings` knobs (custom CAs via the standard validation callback chain, client certificates, etc.) are not blocked.
- `EncryptOptions`, `EncryptionAlgorithm`, `DataKeyOptions`, `RewrapManyDataKeyOptions`, `RangeOptions`, `TextOptions` (with `PrefixOptions`, `SubstringOptions`, `SuffixOptions` for the QE TextPreview surface), `CsfleSchemaBuilder` (a fluent builder for **CSFLE** `$jsonSchema`-style schemas suitable for `AutoEncryptionOptions.SchemaMap`; QE encrypted-field schemas are configured via `AutoEncryptionOptions.EncryptedFieldsMap`, not via this builder).

## Controllers

- `LibMongoCryptControllerBase` — shared state-machine driver. Owns the `CryptClient`, lazy `_keyVaultCollection`, KMS credentials, TLS config, and HTTP/socket factories. Implements the loop: feed input → check `CryptContext` state → handle `NEED_MONGO_KEYS` / `NEED_MONGO_COLLINFO` / `NEED_MONGO_MARKINGS` / `NEED_KMS` / `NEED_KMS_CREDENTIALS` → produce result on `READY`/`DONE`.
- `ExplicitEncryptionLibMongoCryptController` — small surface; backs `ClientEncryption`. Drives the contexts for explicit `Encrypt` / `Decrypt`, `CreateDataKey`, and `RewrapManyDataKey`, all of which still talk to KMS via `NEED_KMS` / `NEED_KMS_CREDENTIALS`. The difference from auto-encryption is that there is **no auto-analysis** (no mongocryptd / crypt_shared involvement) — the application chooses what to encrypt and which key to use, rather than libmongocrypt extracting markings from a schema.
- `AutoEncryptionLibMongoCryptController` — the heavy one. Hooks into the driver's command pipeline; encrypts outbound, decrypts inbound. Owns lazy-initialized auxiliary clients (`_keyVaultClient`, `_metadataClient`, optional `_mongocryptdClient`).

## libmongocrypt P/Invoke layer

- `Library` — declares P/Invoke delegates for every libmongocrypt C function. Loads the native library lazily through `LibraryLoader`.
- `LibraryLoader` + `OperatingSystemHelper` — platform-specific load. Tries assembly directory, `LIBMONGOCRYPT_PATH` (override for the libmongocrypt shared library), then OS defaults. Mismatch between the assembly RID and the bundled binary is the canonical "encryption doesn't work in production" failure. Note: `LIBMONGOCRYPT_PATH` points to libmongocrypt itself; `CRYPT_SHARED_LIB_PATH` (a separate env var) points to the `crypt_shared` library loaded *by* libmongocrypt for query analysis — the two serve different purposes.
- `CryptClient` / `CryptClientFactory` — `mongocrypt_t` lifecycle. One `CryptClient` per `MongoClient` (or per `ClientEncryption`). Methods: `StartEncryptionContext`, `StartDecryptionContext`, `StartCreateDataKeyContext`, plus the explicit-encryption variants `StartExplicitEncryptionContext`, `StartExplicitDecryptionContext`, and `StartRewrapMultipleDataKeysContext` (rewraps every DEK matching the filter). Property: `CryptSharedLibraryVersion` (null means crypt_shared not loaded → mongocryptd fallback). KMS providers and other configuration values are serialized into the underlying `mongocrypt_t` via `CryptOptions` (see `CryptOptions.cs`) before `CryptClientFactory.Create` is called.
- `CryptContext` — per-operation `mongocrypt_ctx_t`. State machine with values from the `StateCode` enum, each prefixed `MONGOCRYPT_CTX_` in the underlying C enum (e.g. `MONGOCRYPT_CTX_READY` ↔ C# `StateCode.Ready`): `ERROR`, `NEED_MONGO_COLLINFO`, `NEED_MONGO_MARKINGS`, `NEED_MONGO_KEYS`, `NEED_KMS`, `NEED_KMS_CREDENTIALS`, `READY`, `DONE`. **Single use** — never reuse.
- SafeHandles: `MongoCryptSafeHandle` (`mongocrypt_t`), `ContextSafeHandle` (`mongocrypt_ctx_t`), `BinarySafeHandle` (`mongocrypt_binary_t`), `StatusSafeHandle` (`mongocrypt_status_t`). Finalizer order matters — see pitfalls.

## KMS abstraction

- `KmsCredentials`, `KmsKeyId` — provider-specific credentials & key references. `KmsKeyId.SetCredentials` registers credentials with the libmongocrypt context before encryption/DEK creation.
- `KmsRequest`, `KmsRequestCollection` — when the context enters `MONGOCRYPT_CTX_NEED_KMS`, the controller iterates the requests in `KmsRequestCollection` and issues HTTP to each provider's endpoint over a TLS socket, feeding replies back into the context. Not a recurring polling loop — it's per-state-step iteration.
- Providers: AWS KMS, Azure Key Vault, GCP Cloud KMS, KMIP, local. Each has its own URL, auth flow, and credential refresh story.

## Crypto callbacks

libmongocrypt asks managed code for AES / HMAC / random / RSA signing via callbacks. The bundled libmongocrypt build the .NET driver ships routes crypto through these managed callbacks rather than depending on OpenSSL on the host — relevant when reading libmongocrypt upstream docs that describe a default OpenSSL link:

- `CipherCallbacks` — AES-CBC and AES-ECB (in `CipherMode.CBC` / `CipherMode.ECB`) via `System.Security.Cryptography.Aes`. Both modes are required by the libmongocrypt callback contract regardless of what the host's platform crypto offers, because libmongocrypt asks for ECB and CBC primitives separately and composes them into the higher-level key-derivation / key-wrap constructions. ECB is only used by libmongocrypt internally for key wrap / derivation primitives — never to encrypt user data records. See the upstream libmongocrypt sources for the exact constructions.
- `HmacShaCallbacks` — HMAC-SHA-256 / HMAC-SHA-512.
- `SecureRandomCallback` — `RandomNumberGenerator.GetBytes`.
- `SigningRSAESPKCSCallback` — RSASSA-PKCS1-v1_5 with SHA-256 (`RSACryptoServiceProvider.SignData`), used by KMIP and Azure key wrapping. The name contains "PKCS" which correctly reflects the signing scheme (not RSA-OAEP, which is an encryption/key-wrap scheme). The PKCS#8 private-key import path requires .NET Core / .NET 5+; on .NET Framework the callback throws `PlatformNotSupportedException`.

## Schema & algorithms

- `CsfleSchemaBuilder` — fluent builder for **CSFLE** `$jsonSchema`-style schemas (composes with `AutoEncryptionOptions.SchemaMap`). The duplicate-namespace check is incidental: `Encrypt<T>(CollectionNamespace, Action<EncryptedCollectionBuilder<T>>)` calls `_schemas.Add(...)` on the underlying `Dictionary<string, BsonDocument>`, which throws `ArgumentException` on a duplicate key — there is no explicit validation step beyond that. **Not** the entry point for Queryable Encryption — QE schemas are configured via `AutoEncryptionOptions.EncryptedFieldsMap`.
- `EncryptionAlgorithm` is a single flat enum — values are not partitioned in the type system, only by usage convention:
  - **CSFLE-only by convention** — `AEAD_AES_256_CBC_HMAC_SHA_512_Deterministic` (equality-queryable, same plaintext → same ciphertext), `AEAD_AES_256_CBC_HMAC_SHA_512_Random` (no queries possible).
  - **QE-only by convention** — `Indexed` (equality with contention), `Range` (range queries), `TextPreview` (preview), `Unindexed`. Server-version availability (preview vs GA) for each algorithm is a server-side concern; consult the MongoDB server release notes rather than relying on driver-side enum metadata. The "Preview" suffix on `TextPreview` reflects the server's preview status — but `EncryptionAlgorithm` is a **public enum**, so the value itself is SemVer-covered: renaming or removing `TextPreview` (e.g. once the server feature GAs as `Text`) requires an `[Obsolete]` deprecation cycle, not an in-place rename. The migration shape is additive-then-deprecate: introduce a new `Text` enum member alongside `TextPreview`, mark `TextPreview` `[Obsolete]`, and only remove it in a later major version — never reuse the existing enum value, since the integer is part of the on-the-wire contract for any caller that has it baked in.
  Server-side enforcement decides which value is valid in a given context. Confusing CSFLE and QE algorithms is a recurring bug.

## Mongocryptd vs crypt_shared

- `MongocryptdFactory` — spawns a local `mongocryptd` process if needed. Default URI `mongodb://localhost:27020`. Controlled by `extraOptions["mongocryptdURI"]`, `mongocryptdSpawnArgs`. Skipped if `BypassQueryAnalysis = true`.
- **`crypt_shared` is preferred** — it's a shared library loaded by libmongocrypt, no separate process. Set `CRYPT_SHARED_LIB_PATH` to point libmongocrypt at it. **QE (Indexed/Range/TextPreview) requires `crypt_shared`**; mongocryptd does not implement QE.

## Threading & lifecycle

- `CryptClient` (wrapping `mongocrypt_t`) is shareable across threads; `CryptContext` (wrapping `mongocrypt_ctx_t`) is **not** — see libmongocrypt's threading docs for the authoritative contract.
- `CryptContext` is **not** reusable. New context per operation.
- SafeHandles: parent (`MongoCryptSafeHandle`) must outlive children (`ContextSafeHandle`). The codebase uses `GC.KeepAlive` and explicit ordering — preserve those when refactoring.
- KMS HTTP is synchronous within the state-machine loop. There's no pipelining; one provider at a time per context.

## Common pitfalls

- **Wrong-RID native library.** Project ships libmongocrypt for several RIDs; cross-RID deployments fail at runtime with `DllNotFound` (or worse, load and silently misbehave). CI must run on macOS, Linux, and Windows.
- **State-machine misuse.** Calling `Encrypt` / `Decrypt` before `InitContext`, or feeding wrong-shape input, corrupts the context. Always drive contexts to `DONE`.
- **KMS credential expiry mid-operation.** AWS STS, Azure IMDS, GCP service tokens can expire. libmongocrypt asks for fresh credentials via `NEED_KMS_CREDENTIALS`; the controller must refetch and resupply. Failing to handle this looks like sporadic "auth failed" errors under load.
- **DEK cache staleness.** `KeyExpiration` is the cache-pruning lever — TTL expiry evicts a DEK on next lookup, not in the background. Long-lived processes that never re-encrypt may accumulate cache entries. `RewrapManyDataKey` is **key rotation** (re-encrypts each DEK with a new KEK in the key vault); it does not itself prune the local DEK cache, but is the canonical way to rotate keys on a schedule — entries then expire normally per `KeyExpiration`.
- **CSFLE vs QE algorithms confused.** `Deterministic`/`Random` are CSFLE-only; `Indexed`/`Range`/`TextPreview`/`Unindexed` are QE-only. The wrong combination on the server side fails with cryptic schema errors.
- **SafeHandle ordering bug.** A `ContextSafeHandle` outliving its parent `MongoCryptSafeHandle` dereferences a destroyed pointer. Don't rearrange disposal order without checking `GC.KeepAlive` calls.
- **TLS callback security.** `ClientEncryptionOptions` rejects insecure TLS callbacks at construction. Don't add a "for testing" bypass that disables this — tests should use the mock KMS instead.

## How to test

Most encryption tests are integration. Required env vars:

| Test target | Env vars |
|---|---|
| CSFLE / auto-encryption | `CRYPT_SHARED_LIB_PATH` |
| Mock KMS servers | `KMS_MOCK_SERVERS_ENABLED` |
| AWS KMS | `CSFLE_AWS_TEMPORARY_CREDS_ENABLED` |
| Azure KMS | `CSFLE_AZURE_KMS_TESTS_ENABLED` |
| GCP KMS | `CSFLE_GCP_KMS_TESTS_ENABLED` |

```bash
CRYPT_SHARED_LIB_PATH=<…> KMS_MOCK_SERVERS_ENABLED=true \
  dotnet test tests/MongoDB.Driver.Encryption.Tests/MongoDB.Driver.Encryption.Tests.csproj -f net10.0
```

The `MongoDB.Driver.Encryption.Tests.csproj` does not declare its own `TargetFrameworks` — it inherits them from `tests/BuildProps/Tests.Build.props`. `-f net10.0` works as long as that shared list still contains `net10.0`; if you change the shared TFM list or hit a "framework not in target list" error, drop `-f` to test against all configured TFMs.

If a required env var is unset for a test you need to run, **stop and tell the user** rather than working around it.

## Spec links

- `tests/MongoDB.Driver.Tests/Specifications/client-side-encryption/` (JSON-driven runner; covers both CSFLE and QE)
