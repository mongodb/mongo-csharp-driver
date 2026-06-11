---
area: Authentication — AWS IAM (cross-ref)
scope: ["src/MongoDB.Driver.Authentication.AWS/**/*.cs"]
reviewer-agent: auth-reviewer
adjacent-areas: [Driver/Authentication]
---

# MongoDB.Driver.Authentication.AWS — AGENTS.md

This is an **optional** project providing the `MONGODB-AWS` SASL mechanism. Kept separate from `MongoDB.Driver` so that consumers who don't need AWS IAM auth aren't forced to ship the AWS SDK / signing dependencies.

For full coverage — credential resolution (explicit MongoCredential vs the AWS SDK's `FallbackCredentialsFactory` chain; consult the AWS SDK for the authoritative source order), SigV4 signing of `sts`-service requests, region derivation from the STS host header (defaulting to `us-east-1`), and registration via the `ISaslMechanismRegistry` exposed by `MongoClientSettings.Extensions.SaslMechanisms` — see the AWS IAM section in `src/MongoDB.Driver/Authentication/AGENTS.md`.

Wiring entry point: consumers call `MongoClientSettings.Extensions.AddAWSAuthentication()` once to opt into this assembly. That call resolves to the `ExtensionManagerExtensions.AddAWSAuthentication` extension method on `IExtensionManager` (in `src/MongoDB.Driver.Authentication.AWS/ExtensionManagerExtensions.cs`), which registers both `MONGODB-AWS` with `SaslMechanisms` and the AWS KMS provider with `KmsProviders`. The two are registered together because they share the same `aws-sdk-net` dependency this assembly carries — having opted in to the AWS SDK at all, registering the KMS provider too is essentially free and avoids a second opt-in for CSFLE consumers; auth-only consumers can leave `KmsProviders` empty and the KMS-side registration is inert. Nothing in this project registers itself at assembly load; the call is explicit.

For CSFLE-side KMS configuration and the broader Client-Side Encryption pipeline, see `src/MongoDB.Driver.Encryption/AGENTS.md`.

Reviewer: `auth-reviewer`.
