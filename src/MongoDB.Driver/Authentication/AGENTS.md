---
area: Authentication
scope: ["src/MongoDB.Driver/Authentication/**/*.cs", "src/MongoDB.Driver.Authentication.AWS/**/*.cs"]
reviewer-agent: auth-reviewer
adjacent-areas: [Core/Connections (ConnectionInitializer), Driver root (MongoCredential)]
---

# Authentication — AGENTS.md

The authentication subsystem under `src/MongoDB.Driver/Authentication/` plus the optional sibling project `src/MongoDB.Driver.Authentication.AWS/`. Responsible for credential storage, mechanism negotiation, the SASL conversation, the `speculativeAuthenticate` handshake optimization, and integration with `Core/Connections/ConnectionInitializer`.

## High-level flow

1. User configures `MongoCredential` on `MongoClientSettings` (or a connection string).
2. `MongoClient` initialization calls `AuthenticatorFactory.Create()` → an `IAuthenticator` (typically `DefaultAuthenticator`).
3. During connection open, `ConnectionInitializer.SendHello()` may inject `speculativeAuthenticate` (via the authenticator's `CustomizeInitialHelloCommand`) to fold the first auth step into the hello.
4. If speculative auth completed (`speculativeAuthenticate.done == true`), nothing more to do.
5. Otherwise, `ConnectionInitializer.Authenticate()` invokes the authenticator. For SASL mechanisms, `SaslAuthenticator` orchestrates a `saslStart` / `saslContinue` loop, feeding bytes between each `ISaslStep` and the server.
6. Servers 7.0+ may demand re-auth mid-session via error 391 (`ReauthenticationRequired`, GA'd alongside OIDC); mechanisms implement `OnReAuthenticationRequired` to clear caches and reset.

## Common abstractions

- `IAuthenticator` — `Authenticate` / `AuthenticateAsync` / `CustomizeInitialHelloCommand`. Implementations: `DefaultAuthenticator` (negotiates SCRAM-SHA-1/256), `SaslAuthenticator` (wraps any `ISaslMechanism`), `MongoDBX509Authenticator`.
- `ISaslMechanism` — algorithm-specific. Properties `Name` and `DatabaseName`; methods `Initialize`, `CreateSpeculativeAuthenticationStep`, `CustomizeSaslStartCommand`, `OnReAuthenticationRequired`, `TryHandleAuthenticationException`.
- `ISaslStep` — one round-trip. `Execute` / `ExecuteAsync` return a `(byte[] BytesToSendToServer, ISaslStep NextStep)` value tuple — there is no named `SaslStepResult` type.
- `SaslAuthenticator` — drives the loop; calls `saslStart` (new conversation) or `saslContinue` (existing `conversationId`) per step.
- `MongoClientSettings.Extensions` is a `public static readonly IExtensionManager` accessor (not an instance property), so the registry is reached via the static call `MongoClientSettings.Extensions.SaslMechanisms` and returns an `ISaslMechanismRegistry` — pluggable mechanism factories. Built-in mechanisms register at startup; consumers register `MONGODB-AWS` by explicitly calling `MongoClientSettings.Extensions.AddAWSAuthentication()` from the AWS assembly — registration is not automatic.

## Mechanisms

### SCRAM-SHA-1 / SCRAM-SHA-256 (`ScramSha/`)

Files: `ScramShaSaslMechanism.cs`, `ScramShaFirstSaslStep.cs`, `ScramShaSecondSaslStep.cs`, `ScramShaLastSaslStep.cs`, `IScramShaAlgorithm.cs`, `ScramSha1Algorithm.cs`, `ScramSha256Algorithm.cs`, `ScramCache.cs`.

Conversation: client-first → server-first → client-final → server-final (RFC 5802). Salted password computed via PBKDF2; `ClientKey`, `StoredKey`, `ClientProof`, `ServerSignature` derived per the spec.

`DefaultAuthenticator` picks the mechanism: if the hello reply includes `saslSupportedMechs` and `SCRAM-SHA-256` is in the list, use SHA-256; otherwise SHA-1 (including the case where `saslSupportedMechs` is absent on older servers).

`ScramCache` keys on `(SaslPreppedPassword, salt, iterations)` and stores derived keys (`ClientKey`, `ServerKey`) as the cache **values**. The SASLprep-normalized password is held in the cache *key* (`ScramCacheKey`) as a `SecureString` — the same handling `UsernamePasswordCredential` uses for the in-memory password — so the key can gate lookups without keeping a managed `string` copy of plaintext-equivalent material in scope. Single-entry cache; primarily skips PBKDF2 on re-authentication.

Speculative auth supported here for SCRAM (SHA-1 and SHA-256). The hello carries `speculativeAuthenticate: { saslStart: ... }` and the server replies with the first server-first message inline. Across this directory, the mechanisms that speculate are: SCRAM (this section, both SHA variants), X.509 (non-SASL — see that section), and OIDC (only when a non-expired token is cached). PLAIN, GSSAPI, and MONGODB-AWS all return `null` from `CreateSpeculativeAuthenticationStep` and do not speculate.

### X.509 (`MongoDBX509Authenticator`)

Mechanism: `MONGODB-X509`. **Not** SASL — a single `authenticate` command after a TLS mutual-auth handshake. Username is optional; if omitted, the server uses the cert subject DN. Speculative auth supported.

Requires `tlsCertificateKeyFile` (or programmatic `SslSettings.ClientCertificates`). Never use without TLS — the entire security argument relies on it.

### GSSAPI / Kerberos (`Gssapi/`)

Files: `GssapiSaslMechanism.cs`, `GssapiFirstSaslStep.cs`, `GssapiNegotiateSaslStep.cs`, `GssapiInitializeSaslStep.cs`, `ISecurityContext.cs`, `SecurityContextFactory.cs` (platform dispatch — SSPI on Windows, libgssapi on Linux/macOS), `Sspi/` (Windows SSPI P/Invoke), `Libgssapi/` (Linux/macOS dynamic load).

Mechanism properties: `SERVICE_NAME` (default `mongodb`), `CANONICALIZE_HOST_NAME` (DNS-resolve to FQDN), `REALM` / `SERVICE_REALM`. SPN format: `<SERVICE_NAME>/<FQDN>[@REALM]`.

Native dependency. On Linux/macOS, `libgssapi` must be installed at runtime; missing library throws on first auth attempt, not at `MongoClient` construction. **No** speculative auth (stateful handshake; can't be embedded in hello).

Cross-platform fragile because of (a) DNS canonicalization variance, (b) SPN canonical form expectations on the KDC, (c) GSSAPI library version skew on the host.

### OIDC (`Oidc/`)

Files: `OidcSaslMechanism.cs`, `OidcConfiguration.cs`, `OidcCredentials.cs`, `IOidcCallback.cs`, `OidcCallbackAdapter.cs`, `OidcCallbackAdapterFactory.cs`, `FileOidcCallback.cs`, `AzureOidcCallback.cs`, `GcpOidcCallback.cs`, `HttpRequestOidcCallback.cs`, `OidcSaslStep.cs` (abstract base), `OidcCachedCredentialsSaslStep.cs`, `OidcObtainCredentialsSaslStep.cs`.

Two production flows, selected by the `ENVIRONMENT` mechanism property on `MongoCredential`:

- **Callback** — `ENVIRONMENT` unset; user supplies `IOidcCallback`. For custom integrations (CI tokens, vault, etc.).
- **Environment** — `ENVIRONMENT` set to one of the values in the private `__supportedEnvironments` set in `OidcConfiguration`: `test`, `azure` (IMDS), `gcp` (metadata service), `k8s` (mounted token). The `test` value is for the spec test harness; `azure`/`gcp`/`k8s` are the real cloud paths. (The set is `private static readonly` — there is no public `SupportedEnvironments` member to grep for.)

The `OIDC_ENV` environment variable is read **only by the test suite** to pick which environment-flow to exercise — see `tests/MongoDB.Driver.Tests/Specifications/auth/OidcAuthenticationProseTests.cs` (the `OidcEnvironmentName` constant) and `tests/MongoDB.Driver.Tests/UnifiedTestOperations/UnifiedEntityMap.cs`. Production code reads the `ENVIRONMENT` mechanism property.

`OidcCallbackAdapter` caches `OidcCredentials` (token + expiry + optional refresh token). Speculative auth engages **only when** there's a non-expired cached token; otherwise it returns null (so the main flow can refresh).

Mid-operation token expiry: `TryHandleAuthenticationException` can refresh and replay the SASL exchange. The retry is at the auth layer, not the operations layer — operation-level retry doesn't help here.

### PLAIN (`Plain/`)

`PlainSaslMechanism.cs`, `PlainSaslStep.cs`. Single round-trip: base64-encoded `\0username\0password` per RFC 4616. Used for LDAP-backed deployments. **TLS is mandatory** — PLAIN over plaintext is a credential leak. No speculative auth.

### AWS IAM (separate project: `src/MongoDB.Driver.Authentication.AWS/`)

Mechanism: `MONGODB-AWS`. Lives in its own NuGet package to avoid forcing the AWS SDK on every consumer. Files: `AWSSaslMechanism.cs`, `CredentialsSources/IAWSCredentialsSource.cs` (the abstraction), `CredentialsSources/AWSInstanceCredentialsSource.cs`, `CredentialsSources/AWSFallbackCredentialsSource.cs`, `SaslSteps/AWSFirstSaslStep.cs`, `SaslSteps/AWSLastSaslStep.cs`, `AWSSignatureVersion4.cs`. The `MONGODB-AWS` mechanism is wired into the global `ISaslMechanismRegistry` via `ExtensionManagerExtensions.cs`.

Credential resolution: explicit credentials supplied via `MongoCredential.CreateCredential("$external", username, password)` with `mechanism=MONGODB-AWS` (or via the connection-string equivalent) are handled by `AWSInstanceCredentialsSource`. There is no `CreateAwsIam` factory on `MongoCredential`. Anything else is delegated to the AWS SDK's `FallbackCredentialsFactory` (via `AWSFallbackCredentialsSource`), which owns the chain — see the AWS SDK for the authoritative ordering of sources (env vars, AppConfig/profile, `AssumeRoleWithWebIdentity`, ECS task role, EC2 instance profile via IMDS, etc.). Don't go hunting for chain logic in this tree; the SDK owns it.

Wire format: SigV4-signed `sts` service request (hard-coded in `AWSSignatureVersion4.cs` per the MONGODB-AWS spec) embedded in the SASL payload. Region is **derived from the STS host header** — `AWSSignatureVersion4.GetRegion(host)` hard-codes `us-east-1` for the exact host `sts.amazonaws.com`, splits any other host on `.` and takes the segment after the first dot (i.e. `split[1]`, e.g. `sts.us-west-2.amazonaws.com` → `us-west-2`), and falls back to `us-east-1` when no such segment is present. It is **not** parsed from the connection string or fetched from EC2 metadata. **No** speculative auth — `AWSSaslMechanism.CreateSpeculativeAuthenticationStep()` returns `null`; only SCRAM, X.509, and OIDC (with a non-expired cached token) speculate.

### External (`External/`)

Non-SASL credential providers shared by OIDC and AWS — `ExternalCredentialsAuthenticators` (singleton holder), `IExternalAuthenticationCredentialsProvider`, `CacheableCredentialsProvider`, plus `AzureAuthenticationCredentialsProvider` and `GcpAuthenticationCredentialsProvider`.

These fetch tokens from cloud metadata endpoints; the OIDC mechanism then sends the token over SASL. Caching with TTL prevents storms on the metadata service.

## Connection-time integration

Entry: `Core/Connections/ConnectionInitializer`. Sequence inside `BinaryConnection.Open`:

1. Open TCP/TLS stream.
2. `ConnectionInitializer.SendHello` — possibly with `speculativeAuthenticate` injected.
3. `ConnectionInitializer.Authenticate` — calls `IAuthenticator.Authenticate`. If `speculativeAuthenticate.done == true`, this is a no-op.
4. Compression negotiation.
5. `ConnectionDescription` is finalized (immutable).

Auth occurs **per connection**, not per operation. A pooled connection holds its authenticated state until reset (e.g., on pool clear, fork detection, or re-auth required).

## Common pitfalls

- **Default mechanism on old servers.** No `saslSupportedMechs` in hello → SCRAM-SHA-1. Don't assume SHA-256 is universal.
- **Explicit mechanism mismatch.** User sets `mechanism=SCRAM-SHA-256` against an old server → `NotSupportedException`. Use `MongoCredential.CreateCredential` (the default-mechanism path) and let `DefaultAuthenticator` negotiate; MONGODB-CR is removed and there is no `CreateMongoDBCR` factory.
- **GSSAPI library missing.** Throws at first auth, not at construction. Surface a clear error in deployment docs.
- **OIDC token expiry mid-operation.** Auth-layer retry refreshes the token, but if the user's callback blocks (e.g., interactive prompt), the operation stalls.
- **AWS metadata server blocked.** Some VPC configs disable IMDS. Provide explicit credentials or use a sidecar.
- **PLAIN without TLS.** The driver doesn't enforce TLS when PLAIN is configured — your deployment must. This is a known gap; enforcement could be added in `PlainSaslMechanism` by checking `SslSettings.UseTls` before proceeding, but it has not been done. Don't treat the absence of enforcement as intentional design.
- **Speculative auth assumption skew.** `DefaultAuthenticator` only reuses speculative results if the hello reply confirms. Don't shortcut by trusting the request.
- **X.509 username mismatch.** If you specify a username, it must match the cert subject DN/SAN; mismatched username is a confusing "auth failed".
- **Auth failures are not retried.** The Operations layer doesn't retry `MongoAuthenticationException` — the connection is closed, and a fresh connection re-auths from scratch. Re-auth-required (error 391) is handled at the mechanism layer, not retry.
- **Per-mechanism cache lifetime.** `ScramCache` is per mechanism instance; `OidcCallbackAdapter` caches across connections in the same client. New `MongoClient` → fresh cache.

## How to test

Required env vars by mechanism (see root `AGENTS.md` for the full table):

| Mechanism | Env vars |
|---|---|
| AWS IAM | `AWS_TESTS_ENABLED` |
| GSSAPI | `GSSAPI_TESTS_ENABLED`, `AUTH_HOST`, `AUTH_GSSAPI` |
| OIDC | `OIDC_ENV` (one of `test`, `azure`, `gcp`, `k8s`) |
| X.509 | `MONGO_X509_CLIENT_CERTIFICATE_PATH`, `MONGO_X509_CLIENT_CERTIFICATE_PASSWORD` |
| PLAIN | `PLAIN_AUTH_TESTS_ENABLED` |

```bash
dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 \
  --filter "FullyQualifiedName~Authentication"
```

JSON-driven runners under `tests/MongoDB.Driver.Tests/Specifications/auth/` exercise the cross-driver test suite.

If a test you need requires an env var that's not set, **stop and tell the user** — don't work around it.

## Boundaries

- **vs `Core/Connections`.** That layer drives connection establishment and calls into the authenticator. Authentication doesn't open sockets.
- **vs `Driver` root (`MongoCredential`, `MongoIdentity`, `MongoX509Identity`, etc.).** The root types are public API — adding/changing them is SemVer. The mechanisms here are internal.
- **vs `Driver.Authentication.AWS`.** Optional dependency; the driver functions without it. AWS-specific code stays in that project.
