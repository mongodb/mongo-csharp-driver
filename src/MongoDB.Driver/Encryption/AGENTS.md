---
area: Encryption — driver-side glue
scope: ["src/MongoDB.Driver/Encryption/**/*.cs"]
reviewer-agent: encryption-reviewer
adjacent-areas: [src/MongoDB.Driver.Encryption (libmongocrypt wrapper), Driver/Core/WireProtocol]
---

# Driver Encryption Glue — AGENTS.md

The hooks the driver itself needs in order to delegate auto-encryption to libmongocrypt. The cryptographic engine lives in the sibling project `src/MongoDB.Driver.Encryption/` — this directory is the bridge.

## Files

- **`AutoEncryptionProviderRegistry`** / **`IAutoEncryptionProviderRegistry`** — service locator for the auto-encryption factory. `AutoEncryptionProviderRegistry` is an `internal sealed` class whose default instance is built by `CreateDefaultInstance()` and held by the driver's extension manager (`MongoClientSettings.Extensions`); it is not exposed as a `public static Instance` field. The encryption project registers a factory at startup; the driver looks it up to construct an `IAutoEncryptionLibMongoCryptController` per client. Registration is idempotent for the same factory reference; only re-registration with a *different* factory throws. Missing registration throws (`MongoConfigurationException`) when `AutoEncryptionOptions` is set on a `MongoClient`.
- **`IAutoEncryptionLibMongoCryptController`** — minimal contract used by the wire layer. Inherits `IBinaryCommandFieldEncryptor`, `IBinaryDocumentFieldDecryptor`, and `IDisposable`, so the surface is `EncryptFields` / `EncryptFieldsAsync`, `DecryptFields` / `DecryptFieldsAsync`, `CryptSharedLibraryVersion`, and `Dispose`. Implementation lives in `src/MongoDB.Driver.Encryption/AutoEncryptionLibMongoController.cs` (the file name omits "Crypt" even though the class inside is `AutoEncryptionLibMongoCryptController`).
- **`KmsProviderRegistry`** / **`IKmsProviderRegistry`** / **`IKmsProvider`** — name-keyed factory registry. `Register(string kmsProviderName, Func<IKmsProvider> factory)` adds a provider factory; `TryCreate(string providerName, out IKmsProvider provider)` resolves one by name when libmongocrypt asks for fresh credentials. `IKmsProvider` is the per-provider credential-refresh abstraction.
- **`AzureKmsProvider`**, **`GcpKmsProvider`** — cloud-specific credential refresh logic. Azure renews bearer tokens via IMDS; GCP validates and refreshes service-account JSON. AWS credential refresh happens inside libmongocrypt.
- **`KmsProvidersHelper`**, **`KmsProvidersEqualityHelper`** — convert/inspect the loosely-typed `IDictionary<string, IDictionary<string, object>>` form used in `AutoEncryptionOptions` and `ClientEncryptionOptions`.
- **`EncryptionExtraOptionsHelper`** — parses and validates the `extraOptions` dictionary on `AutoEncryptionOptions` (e.g. `mongocryptdURI`, `mongocryptdSpawnArgs`, `cryptSharedLibPath`, `cryptSharedRequired`) into typed values before they flow into the libmongocrypt wrapper.
- **`EncryptedCollectionHelper`** — utilities for QE collection setup. Validates that `SchemaMap` and `EncryptedFieldsMap` don't both name the same collection. Derives helper-collection names from the internal `HelperCollectionForEncryption` enum, whose two members are `Esc` and `Ecos`. `Esc` maps to the on-disk `esc` collection (the straightforward case — C# `Esc` ↔ on-disk `esc`). `Ecos` is the historical-artifact case: the member name dates from when ECOS/ECOC/ECC were three separate QE state collections; after ECC was removed from QE, `Ecos` was repurposed and now resolves to the on-disk `ecoc` collection (look at `GetAdditionalCollectionName`: `HelperCollectionForEncryption.Ecos` reads `ecocCollection` from `encryptedFields` and defaults to `enxcol_.<coll>.ecoc`). So the C# enum value `Ecos` ↔ on-disk collection `ecoc` is intentional, not a typo — the asymmetry is only on the `Ecos` member, not on `Esc`. `TryGetEffectiveEncryptedFields` merges per-context schema with `EncryptedFieldsMap`.
- **`NoopBinaryDocumentFieldCryptor`** — pass-through implementation kept as a test/scaffolding hook. It is **not** wired into the production registration path: `AutoEncryptionProviderRegistry.CreateAutoCryptClientController` throws `MongoConfigurationException("No AutoEncryption provider has been registered.")` when no factory is present, so an unregistered driver never silently falls back to this no-op. Treat the class as available for tests and future hooks; do not enable it on production paths — it bypasses all encryption.

## How encryption is wired in

1. User configures `MongoClientSettings.AutoEncryptionOptions` (the user-facing options type lives at the driver root, not under this directory).
2. `MongoClient` constructor checks for `AutoEncryptionOptions`; if present, calls into `AutoEncryptionProviderRegistry.CreateAutoCryptClientController(...)`.
3. The registry returns an `IAutoEncryptionLibMongoCryptController`, which the wire layer (in `Core/WireProtocol`) calls before sending each command and after receiving each reply.

## Common pitfalls

- **Registry not initialized.** If the encryption assembly isn't loaded (e.g., trimmed away in AOT), the registry has no factory; `MongoClient` construction throws `MongoConfigurationException` when auto-encryption is requested. Reference the encryption assembly explicitly to defeat trimming.
- **`SchemaMap` and `EncryptedFieldsMap` collisions.** Per `EncryptedCollectionHelper.EnsureCollectionsValid`, the same collection must not appear in both. Tooling that builds these from generated code can hit this.
- **Cloud credential refresh races.** Concurrent operations may both try to refresh expired Azure/GCP credentials. The provider implementations are responsible for coordinating; check them before adding new providers.

## How to test

```bash
CRYPT_SHARED_LIB_PATH=<…> KMS_MOCK_SERVERS_ENABLED=true \
  dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Encryption"
```

Cross-cutting tests that exercise auto-encryption end-to-end live in `tests/MongoDB.Driver.Tests/Specifications/client-side-encryption/`. See the env-var table in the root `AGENTS.md`.
