# Contributing to the MongoDB C# Driver

## Overview

This repo contains the code and tests for the official MongoDB driver for .NET/C#.

## Find or create a tracking issue

Any bug fix should have a tracking issue in [Jira](https://jira.mongodb.org/projects/CSHARP/issues/). Check for an existing issue first and read any discussion and related PRs; if none exists, create one and wait for guidance from the team before starting work, so you don't build something that won't be accepted.

> Note: An issue is not required for a simple typo PR.

## Building and testing the code

Build the code and run the tests locally before submitting a pull request. This is straightforward but needs some one-time setup.

### Install the .NET SDKs

The driver and tests are multi-targeted across .NET Core 3.1, .NET 6, and .NET 10 (plus .NET Framework 4.7.2 on Windows). Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) to build; running the full test suite also needs the .NET 6 and .NET Core 3.1 runtimes, or you can [test a single target](#single-target-runs).

### Install git

Ensure [git](https://git-scm.com/downloads) is installed. It often comes with your IDE or other tools, such as [Homebrew](https://brew.sh/) on MacOS.

### Run MongoDB locally

> Note: These instructions were tested on MacOS; they should also work on Linux and Windows.

#### Docker (recommended)

Most tests run against a local MongoDB in Docker. Run [start-mongodb.sh](start-mongodb.sh) to start the container (via [docker-compose.yml](docker-compose.yml)) and wait until it is ready:

```bash
./start-mongodb.sh
```

Set `MONGODB_URI` to `mongodb://localhost:56665/?replicaSet=rs0` before running the tests. Edit the compose file to change the port.

> Note: Data persists in a Docker volume across restarts. If you change the MongoDB version in the compose file, run `docker compose down -v` first — the old data files are incompatible with a different server version, and the container will otherwise fail to start (exit code 62).

#### Atlas (search and vector tests)

The [Atlas Local CLI](https://www.mongodb.com/docs/atlas/cli/current/atlas-cli-deploy-local/) is not suitable for general driver testing because it does not support `--setParameter enableTestCommands=1`. The search and vector tests targeted at Atlas Search do not run by default; to enable them, set:

- `ATLAS_SEARCH_TESTS_ENABLED=1`
- `ATLAS_SEARCH_URI` — connection string for your Atlas Local instance

#### Encryption tests

Download the "Crypt Shared" archive for your platform from [MongoDB Enterprise Downloads](https://www.mongodb.com/try/download/enterprise-advanced/releases) and extract it:

```zsh
mkdir -p mongo_crypt_shared_v1-macos-arm64-enterprise-8.0.11
tar -xvzf mongo_crypt_shared_v1-macos-arm64-enterprise-8.0.11.tgz -C mongo_crypt_shared_v1-macos-arm64-enterprise-8.0.11
```

Then set:

- `CRYPT_SHARED_LIB_PATH` — the crypt shared library itself (the `.dll`, `.so`, or `.dylib` file)
- `MONGODB_BINARIES` — the folder containing the server binaries

### Fork and clone

[Fork](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo) the [repo](https://github.com/mongodb/mongo-csharp-driver) ("Fork", top right), then clone your fork:

```bash
git clone https://github.com/your-github-name/mongo-csharp-driver
```

### Build and run tests

Open `CSharpDriver.sln` in your IDE, or use the command line:

```zsh
dotnet build
dotnet test
```

#### Single-target runs

`dotnet test` runs every target by default. Note:

- The .NET Framework target cannot be used on Mac or Linux.
- Targets before .NET 6 cannot be used on ARM64.

Restrict to one target with `--framework`:

```zsh
dotnet test --framework net10.0
```

## Submit a PR

Push your branch to your fork once all tests pass. [Squash multiple commits into one](https://stackoverflow.com/questions/5189560/how-do-i-squash-my-last-n-commits-together) and rebase on the latest upstream first (use the "Sync fork" button on your fork's page).

![Syncing your fork with upstream changes](syncfork.png)

> Note: The final commit message must start with the JIRA issue number.

Open a PR with the "Compare and pull request" button on the [official repository](https://github.com/mongodb/mongo-csharp-driver), explaining what you changed and why.

# Appendix: Environment variables

Environment variables used across the codebase. `(SET)` marks a variable the code sets rather than reads. Paths under `src/` are production code; all others are test-only.

## Connection & URI

| Variable | Purpose | Used in |
|---|---|---|
| `MONGODB_URI` | Primary connection string (falls back to `MONGO_URI`, then `mongodb://localhost`) | `CoreTestConfiguration.cs`, `SmokeTests/.../InfrastructureUtilities.cs`, `LambdaFunction.cs`, `BenchmarkHelper.cs`, `UnifiedTestSpecRunner.cs` (SET) |
| `MONGO_URI` | Fallback for `MONGODB_URI` (default `mongodb://localhost`) | `CoreTestConfiguration.cs`, `SmokeTests/.../InfrastructureUtilities.cs` |
| `MONGODB_URI_WITH_MULTIPLE_MONGOSES` | Load-balancing / sharded-cluster scenarios (default `mongodb://localhost,localhost:27018`) | `CoreTestConfiguration.cs`, `UnifiedTestSpecRunner.cs` (SET), `LoadBalancingIntegrationTests.cs` (SET) |
| `ATLAS_SEARCH_URI` | Connection string for Atlas Search integration tests | `AtlasSearchTestsUtils.cs` |

## AWS authentication

| Variable | Purpose | Used in |
|---|---|---|
| `AWS_ACCESS_KEY_ID` | AWS auth credential | `AwsAuthenticationTests.cs`, `AwsAuthenticationExamples.cs`, `AwsLambdaExamples.cs` |
| `AWS_SECRET_ACCESS_KEY` | AWS auth credential | `AwsAuthenticationTests.cs`, `AwsAuthenticationExamples.cs`, `AwsLambdaExamples.cs` |
| `AWS_SESSION_TOKEN` | Temporary AWS session credential | `AwsAuthenticationExamples.cs`, `AwsLambdaExamples.cs` |
| `AWS_PROFILE` | Selects AWS credential profile (default `default`) | `AwsAuthenticationTests.cs` |
| `AWS_CONTAINER_CREDENTIALS_RELATIVE_URI` | ECS container credential resolution | `AwsAuthenticationTests.cs` |
| `AWS_CONTAINER_CREDENTIALS_FULL_URI` | Alternative ECS container credential endpoint | `AwsAuthenticationTests.cs` |
| `AWS_WEB_IDENTITY_TOKEN_FILE` | Web identity federation | `AwsAuthenticationTests.cs` |
| `AWS_ECS_ENABLED` | Detect ECS environment | `AwsAuthenticationTests.cs` |

## FaaS & serverless detection (production code)

All used in `src/.../Core/Servers/ServerMonitor.cs` and `src/.../Core/Connections/ClientDocumentHelper.cs`.

| Variable | Purpose |
|---|---|
| `AWS_EXECUTION_ENV` | Detect AWS Lambda (prefix `AWS_Lambda_`); adjusts heartbeat frequency and client metadata |
| `AWS_LAMBDA_RUNTIME_API` | Detect AWS Lambda; sets `faas.name` to `aws.lambda` |
| `AWS_REGION` | Populate `faas.region` for Lambda (`ClientDocumentHelper.cs` only) |
| `FUNCTIONS_WORKER_RUNTIME` | Detect Azure Functions; sets `faas.name` to `azure.func` |
| `K_SERVICE` | Detect Google Cloud Run/Functions; sets `faas.name` to `gcp.func` |
| `FUNCTION_NAME` | Detect Google Cloud Functions |
| `VERCEL` | Detect Vercel; sets `faas.name` to `vercel` |

## X.509 certificates

Both used in `CoreTestConfiguration.cs`, `AuthenticationTests.cs`, and `EncryptionTestHelper.cs`.

| Variable | Purpose |
|---|---|
| `MONGO_X509_CLIENT_CERTIFICATE_PATH` | Client certificate for X.509 authentication |
| `MONGO_X509_CLIENT_CERTIFICATE_PASSWORD` | Unlock password-protected certificate files |

## GSSAPI / Kerberos

| Variable | Purpose | Used in |
|---|---|---|
| `AUTH_GSSAPI` | Enable and configure Kerberos auth tests (throws if unset when needed) | `GssapiAuthenticationTests.cs`, `GssapiSecurityCredentialTests.cs` |
| `AUTH_HOST` | Kerberos authentication endpoint | `GssapiAuthenticationTests.cs` |

## OIDC

| Variable | Purpose | Used in |
|---|---|---|
| `OIDC_ENV` | OIDC test environment (`test`, `azure`, `gcp`, `k8s`) | `OidcAuthenticationProseTests.cs`, `UnifiedEntityMap.cs` |
| `OIDC_TOKEN_DIR` | Path to OIDC token files for testing | `OidcAuthenticationProseTests.cs`, `OidcCallbackAdapterFactory.cs` (as `OIDC_TOKEN_FILE`) |
| `TOKEN_RESOURCE` | OIDC token resource parameter | `OidcAuthenticationProseTests.cs`, `UnifiedEntityMap.cs` |

## Client-Side Field Level Encryption (CSFLE)

| Variable | Purpose | Used in |
|---|---|---|
| `CRYPT_SHARED_LIB_PATH` | Path to the MongoDB crypt_shared library | `CoreTestConfiguration.cs`, `AutoEncryptionTests.cs` |
| `LIBMONGOCRYPT_PATH` | Path to libmongocrypt (production) | `src/MongoDB.Driver.Encryption/LibraryLoader.cs` |
| `MONGODB_BINARIES` | Path to mongocryptd and other server binaries (default empty) | `EncryptionTestHelper.cs` |
| `FLE_MONGOCRYPTD_PORT` | Custom port for mongocryptd | `EncryptionTestHelper.cs` |
| `FLE_AWS_KEY` / `FLE_AWS_SECRET` | AWS KMS credentials for FLE | `EncryptionTestHelper.cs` |
| `FLE_AWS_KEY2` / `FLE_AWS_SECRET2` | Secondary AWS KMS credentials | `EncryptionTestHelper.cs` |
| `FLE_AZURE_TENANTID` / `FLE_AZURE_CLIENTID` / `FLE_AZURE_CLIENTSECRET` | Azure KMS credentials for FLE | `EncryptionTestHelper.cs` |
| `FLE_GCP_EMAIL` / `FLE_GCP_PRIVATEKEY` | GCP KMS credentials for FLE | `EncryptionTestHelper.cs` |
| `CSFLE_AWS_TEMPORARY_CREDS_ENABLED` | Enable AWS temporary credentials for FLE tests | `EncryptionTestHelper.cs` |
| `CSFLE_AWS_TEMP_ACCESS_KEY_ID` / `CSFLE_AWS_TEMP_SECRET_ACCESS_KEY` / `CSFLE_AWS_TEMP_SESSION_TOKEN` | Temporary AWS credentials for FLE | `EncryptionTestHelper.cs` |
| `CSFLE_AZURE_KMS_TESTS_ENABLED` | Enable Azure KMS integration tests | `ClientEncryptionProseTests.cs` |
| `CSFLE_GCP_KMS_TESTS_ENABLED` | Enable GCP KMS integration tests | `ClientEncryptionProseTests.cs` |

## GCP & Azure

| Variable | Purpose | Used in |
|---|---|---|
| `GCE_METADATA_HOST` | Custom GCE metadata server endpoint (default `metadata.google.internal`) | `src/.../GcpAuthenticationCredentialsProvider.cs`, `ClientEncryptionProseTests.cs` |
| `AZURE_IMDS_MOCK_ENDPOINT` | Mock Azure Instance Metadata Service endpoint | `ClientEncryptionProseTests.cs` |

## Testing & configuration

| Variable | Purpose | Used in |
|---|---|---|
| `MONGO_LOGGING` | Enable test logging output | `CoreTestConfiguration.cs` |
| `MONGO_SERVER_SELECTION_TIMEOUT_MS` | Override server selection timeout for tests | `CoreTestConfiguration.cs`, `DriverTestConfiguration.cs` |
| `MONGODB_API_VERSION` | Specify Server API version for tests | `CoreTestConfiguration.cs` |
| `SKIPTESTSTHATREQUIRESERVER` | Skip integration tests when no server is available | `RequireServer.cs` |
| `DRIVER_PACKAGE_VERSION` | Validate NuGet package versions in smoke tests | `ValidatePackagesVersionTests.cs` |
| OCSP test variables | Control OCSP certificate validation tests (variable name determined per test) | `OcspIntegrationTests.cs` |
