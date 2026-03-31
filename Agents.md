# Agents.md - CSharpDriver

## Overview
The C# driver for MongoDB.

## Tech Stack
- .NET library projects producing NuGet packages
- Multi-targeted from .NET Framework 4.7.2 through .NET 10
- xUnit + FluentAssertions for testing

## Project Structure
- `src/MongoDB.Bson/` - BSON for MongoDB
- `src/MongoDB.Driver/` - C# driver for MongoDB
- `MongoDB.Driver.Encryption` - Client encryption (CSFLE with KMS).
- `MongoDB.Driver.Authentication.AWS` - AWS IAM authentication
- `tests/MongoDB.Driver.Tests/` - Main C# driver tests
- `tests/MongoDB.Bson.Tests/` - BSON handling tests
- `tests/*/TestHelpers` - Common test utilities
- `tests/*` - Specialized tests; less common
- `tests/MongoDB.Driver.Tests/Specifications/` are JSON-driven tests using a common runner.

## Commands
- Build: `dotnet build CSharpDriver.sln`
- Run all tests: `dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0`
- Run a single test class: `dotnet test tests/MongoDB.Driver.Tests/MongoDB.Driver.Tests.csproj -f net10.0 --filter "FullyQualifiedName~ClassName"`

A MongoDB connection is always available locally, so "integration" tests can be run as well as unit tests. Some test suites also require additional environment variables — if you need to run those tests and the variables are not set, stop and tell the user which variables are needed rather than working around it.

| Feature area | Required environment variables |
|---|---|
| Atlas Search | `ATLAS_SEARCH_TESTS_ENABLED`, `ATLAS_SEARCH_URI` |
| Atlas Search index helpers | `ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED`, `ATLAS_SEARCH_URI` |
| CSFLE / auto-encryption | `CRYPT_SHARED_LIB_PATH` |
| CSFLE with KMS mock servers | `KMS_MOCK_SERVERS_ENABLED` |
| CSFLE with AWS KMS | `CSFLE_AWS_TEMPORARY_CREDS_ENABLED` |
| CSFLE with Azure KMS | `CSFLE_AZURE_KMS_TESTS_ENABLED` |
| CSFLE with GCP KMS | `CSFLE_GCP_KMS_TESTS_ENABLED` |
| AWS authentication | `AWS_TESTS_ENABLED` |
| GSSAPI / Kerberos | `GSSAPI_TESTS_ENABLED`, `AUTH_HOST`, `AUTH_GSSAPI` |
| OIDC authentication | `OIDC_ENV` |
| X.509 authentication | `MONGO_X509_CLIENT_CERTIFICATE_PATH`, `MONGO_X509_CLIENT_CERTIFICATE_PASSWORD` |
| PLAIN authentication | `PLAIN_AUTH_TESTS_ENABLED` |
| SOCKS5 proxy | `SOCKS5_PROXY_SERVERS_ENABLED` |

## Commit and PR Conventions

- Commit and PR messages start with a JIRA number: `CSHARP-1234: Description`
