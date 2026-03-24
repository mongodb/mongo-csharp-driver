# Contributing to the MongoDB C# Driver

## Overview

This repo contains the code and tests for the official MongoDB driver for .NET/C#.

## Find or create a tracking issue

Any bug fix should have an issue tracking it in the [Jira issue tracker for the MongoDB C# Driver](https://jira.mongodb.org/projects/CSHARP/issues/). First, check for any existing issue, and if found, make sure to read any discussion and look at any existing pull requests. If there is no existing issue covering your case, then create one and wait for feedback from the team for any guidance. This avoids you doing work that might not be accepted.

> Note: An issue is not required for a simple typo PR.

## Building and testing the code

It is important to ensure that any change works correctly and does not regress existing behaviors before submitting a pull request. To do this, you will need to build the code and run the tests on your local machine. This is straightforward, but does require some one-time set up.

### Install the .NET SDKs

The C# driver is multi-targetted so that it works across multiple .NET versions. However, building and testing requires .NET 10 (latest) and .NET 8 (LTS). Install the SDK from [Microsoft .NET home](https://dotnet.microsoft.com/download). You may wish to install other .NET runtime versions for testing.

### Install git

Ensure that git is installed on your system. It may already be installed as part of your IDE or other development tools, such as [Homebrew](https://brew.sh/) on MacOS. Alternately, it can be downloaded from directly from [git](https://git-scm.com/downloads).

### Install and run MongoDB locally

> Note: These instructions have been tested on MacOS. There is no reason that this should not work on Linux and Windows too, but that has not been tested.

#### Running MongoDB in Docker

Most tests can run against a local instance of MongoDB running in Docker. A Docker compose file called [docker-compose.yml](docker-compose.yml) is provided to start this server for you. There is also a shell script called [start-mongodb.sh](start-mongodb.sh) that will start the container and wait for it to be ready. This script executes the following commands:

Stop any existing containers:

```bash
docker compose down 2>/dev/null || true
```

Start MongoDB:

```bash
docker compose up -d
```

The connection string for this local MongoDB instance is, `mongodb://localhost:56665/?replicaSet=rs0&directConnection=true`. Set the environment variable `MONGODB_URI` to this value before running the tests. Adjust the docker compose file if you need to change the port.

#### Running Atlas in Docker

The [Atlas Local CLI](https://www.mongodb.com/docs/atlas/cli/current/atlas-cli-deploy-local/) is a great way to work with MongoDB locally. Unfortunately, it is not suitable for general driver testing because it does not support `--setParameter enableTestCommands=1` when starting the mongod server.

However, non-Atlas MongoDB cannot currently execute all the search and vector tests targetted at Atlas Search. Therefore, these tests do not run by default. To enable them, set the following environment variables:

- `ATLAS_SEARCH_TESTS_ENABLED` = 1
- `ATLAS_SEARCH_INDEX_HELPERS_TESTS_ENABLED` = 1
- `ATLAS_SEARCH_URI` to the connection string for your Atlas Local instance

#### Encryption tests

Additional libraries are needed to run the encryption tests. Download the "Crypt Shared" `tgz` or `zip` archive for your platform from [MongoDB Enterprise Downloads](https://www.mongodb.com/try/download/enterprise-advanced/releases).

Extract the archive to a location of your choice. For example:

```zsh
tar -xvzf mongo_crypt_shared_v1-macos-arm64-enterprise-8.0.11.tgz -C mongo_crypt_shared_v1-macos-arm64-enterprise-8.0.11
```

Finally, set environment variables to point to the extracted archive:

- `CRYPT_SHARED_LIB_PATH` points to the crypt shared library itself--that is, the `.dll`, `.so`, or `.dylib` file.
- `MONGODB_BINARIES` points to the folder containing the server binaries.

### Fork and clone the GitHub repo

You will make your changes in your own copy of the GitHub repo. To do this, go to the [MongoDB C# Driver repo](https://github.com/mongodb/mongo-csharp-driver) and choose "Fork" at the top right. See [Fork a repository](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/working-with-forks/fork-a-repo) for more information on creating an using forks.

You now have your own copy (fork) of the code in GitHub, so you can use `git clone` to work with that copy on your local machine. For example:

```bash
clone https://github.com/your-github-name/mongo-csharp-driver
```

### Build the code and run tests

The .NET solution "CSharpDriver.sln" can be used to work with the code in your favorite IDE. Alternately, the code can be built from the command line with:

```zsh
dotnet build
```

And tests can be run with:

```zsh
dotnet test
```

#### Notes on running tests locally

The tests are run against multiple targets by default. This means:

- The .NET Framework target cannot be used on Mac or Linux systems.
- Targets before .NET 6 cannot be used on ARM64 systems.

To run tests only against a single target, use the `--framework` option. For example:

```zsh
dotnet test --framework net10
```

## Submit a PR

Once the changes have been made and all tests are passing, create a branch with these changes and push to your GitHub fork. If your branch contains multiple commits, then please [squash these into a single commit](https://stackoverflow.com/questions/5189560/how-do-i-squash-my-last-n-commits-together) before submitting a PR. Also, you may need to rebase your PR on top of recent upstream changes to the official repo. Use the "Sync fork" button on your GitHub fork page to do this.

![Syncing your fork with upstream changes](syncfork.png)

> Note: Your final commit should have the JIRA issue number as the first part of the commit message.

Use the "Compare and pull request" button on the [official provider repo](https://github.com/mongodb/mongo-csharp-driver) to create a pull request, pulling changes from your fork.

Make sure to explain what you did and why in the pull request description.

# Appendix

## MongoDB C# Driver - Environment Variables Usage Summary

This document provides a comprehensive summary of all environment variables used across the MongoDB C# Driver codebase, including their purpose, location, and usage context.

---

## 1. Connection & URI Configuration

### `MONGODB_URI`
**Primary MongoDB connection string**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:258`
    - `tests/SmokeTests/MongoDB.Driver.SmokeTests.Sdk/InfrastructureUtilities.cs:28`
    - `tests/MongoDB.Driver.Examples/InfrastructureUtilities.cs:22`
    - `tests/FaasTests/LambdaTests/MongoDB.Driver.LambdaTest/LambdaFunction.cs:51`
    - `benchmarks/MongoDB.Driver.Benchmarks/BenchmarkHelper.cs:83`
    - `tests/AstrolabeWorkloadExecutor/Program.cs:106` (SET)
    - `tests/MongoDB.Driver.Tests/Specifications/UnifiedTestSpecRunner.cs:147` (SET)
- **Purpose:** Main connection string for tests and examples
- **Fallback:** Falls back to `MONGO_URI` or default `mongodb://localhost`

### `MONGO_URI`
**Alternative MongoDB connection string**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:259`
    - `tests/SmokeTests/MongoDB.Driver.SmokeTests.Sdk/InfrastructureUtilities.cs:29`
    - `tests/MongoDB.Driver.Examples/InfrastructureUtilities.cs:23`
- **Purpose:** Fallback for `MONGODB_URI`
- **Default:** `mongodb://localhost`

### `MONGODB_URI_WITH_MULTIPLE_MONGOSES`
**Connection string with multiple mongos servers**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:264`
    - `tests/MongoDB.Driver.Tests/Specifications/UnifiedTestSpecRunner.cs:148` (SET)
    - `tests/MongoDB.Driver.Tests/Core/LoadBalancingIntegrationTests.cs:741` (SET)
- **Purpose:** Testing load balancing and sharded cluster scenarios
- **Default:** `mongodb://localhost,localhost:27018`

### `ATLAS_SEARCH_URI`
**Atlas Search connection string**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Search/AtlasSearchTestsUtils.cs:28`
- **Purpose:** Connection string for Atlas Search integration tests
- **Required:** For Atlas Search tests only

---

## 2. Authentication - AWS

### `AWS_ACCESS_KEY_ID`
**AWS access key for authentication**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:114`
    - `tests/MongoDB.Driver.Examples/Aws/AwsAuthenticationExamples.cs:72, 117`
    - `tests/MongoDB.Driver.Examples/Aws/AwsLambdaExamples.cs:59`
- **Purpose:** AWS authentication credential

### `AWS_SECRET_ACCESS_KEY`
**AWS secret access key**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:114`
    - `tests/MongoDB.Driver.Examples/Aws/AwsAuthenticationExamples.cs:73, 118`
    - `tests/MongoDB.Driver.Examples/Aws/AwsLambdaExamples.cs:60`
- **Purpose:** AWS authentication credential

### `AWS_SESSION_TOKEN`
**AWS session token for temporary credentials**
- **Used in:**
    - `tests/MongoDB.Driver.Examples/Aws/AwsAuthenticationExamples.cs:74, 119`
    - `tests/MongoDB.Driver.Examples/Aws/AwsLambdaExamples.cs:61`
- **Purpose:** Temporary AWS session credential

### `AWS_PROFILE`
**AWS credential profile name**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:144`
- **Purpose:** Selects AWS credential profile
- **Default:** `default`

### `AWS_CONTAINER_CREDENTIALS_RELATIVE_URI`
**ECS container credentials endpoint**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:64, 128`
- **Purpose:** ECS container credential resolution

### `AWS_CONTAINER_CREDENTIALS_FULL_URI`
**Full URI for container credentials**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:64, 128`
- **Purpose:** Alternative ECS container credential endpoint

### `AWS_WEB_IDENTITY_TOKEN_FILE`
**Path to web identity token file**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:84`
- **Purpose:** Web identity federation for AWS

### `AWS_ECS_ENABLED`
**Indicates ECS environment**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/AwsAuthenticationTests.cs:63`
- **Purpose:** Detect ECS execution environment

---

## 3. FaaS & Serverless Environment Detection

### `AWS_EXECUTION_ENV`
**AWS Lambda execution environment**
- **Used in:**
    - `src/MongoDB.Driver/Core/Servers/ServerMonitor.cs:272`
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:147`
- **Purpose:** Detect AWS Lambda environment (checks if starts with `AWS_Lambda_`)
- **Effect:** Adjusts heartbeat frequency, modifies client metadata

### `AWS_LAMBDA_RUNTIME_API`
**AWS Lambda runtime API endpoint**
- **Used in:**
    - `src/MongoDB.Driver/Core/Servers/ServerMonitor.cs:273`
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:148`
- **Purpose:** Detect AWS Lambda environment
- **Effect:** Sets `faas.name` to "aws.lambda" in client metadata

### `AWS_REGION`
**AWS region**
- **Used in:**
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:177`
- **Purpose:** Populate `faas.region` in client metadata for Lambda

### `FUNCTIONS_WORKER_RUNTIME`
**Azure Functions runtime**
- **Used in:**
    - `src/MongoDB.Driver/Core/Servers/ServerMonitor.cs:274`
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:152`
- **Purpose:** Detect Azure Functions environment
- **Effect:** Sets `faas.name` to "azure.func" in client metadata

### `K_SERVICE`
**Google Cloud Run service name**
- **Used in:**
    - `src/MongoDB.Driver/Core/Servers/ServerMonitor.cs:275`
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:158`
- **Purpose:** Detect Google Cloud Run/Functions environment
- **Effect:** Sets `faas.name` to "gcp.func" in client metadata

### `FUNCTION_NAME`
**Generic function name**
- **Used in:**
    - `src/MongoDB.Driver/Core/Servers/ServerMonitor.cs:276`
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:158`
- **Purpose:** Detect Google Cloud Functions environment

### `VERCEL`
**Vercel environment indicator**
- **Used in:**
    - `src/MongoDB.Driver/Core/Servers/ServerMonitor.cs:277`
    - `src/MongoDB.Driver/Core/Connections/ClientDocumentHelper.cs:164`
- **Purpose:** Detect Vercel serverless environment
- **Effect:** Sets `faas.name` to "vercel" in client metadata

---

## 4. Authentication - X.509 Certificates

### `MONGO_X509_CLIENT_CERTIFICATE_PATH`
**Path to X.509 client certificate**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:145`
    - `tests/MongoDB.Driver.Tests/AuthenticationTests.cs:302`
    - `tests/MongoDB.Driver.Tests/Specifications/client-side-encryption/EncryptionTestHelper.cs:282`
- **Purpose:** Client certificate for X.509 authentication

### `MONGO_X509_CLIENT_CERTIFICATE_PASSWORD`
**Password for X.509 certificate**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:150`
    - `tests/MongoDB.Driver.Tests/AuthenticationTests.cs:303`
    - `tests/MongoDB.Driver.Tests/Specifications/client-side-encryption/EncryptionTestHelper.cs:283`
- **Purpose:** Unlock password-protected certificate files

---

## 5. Authentication - GSSAPI/Kerberos

### `AUTH_GSSAPI`
**GSSAPI/Kerberos authentication configuration**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/GssapiAuthenticationTests.cs:83, 95`
    - `tests/MongoDB.Driver.Tests/Authentication/Libgssapi/GssapiSecurityCredentialTests.cs:35`
- **Purpose:** Enable and configure Kerberos authentication tests
- **Required:** Throws exception if not set when needed

### `AUTH_HOST`
**Authentication host for Kerberos**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Communication/Security/GssapiAuthenticationTests.cs:90`
- **Purpose:** Kerberos authentication endpoint

---

## 6. Authentication - OIDC

### `OIDC_ENV`
**OIDC environment type**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Specifications/auth/OidcAuthenticationProseTests.cs:54, 604`
    - `tests/MongoDB.Driver.Tests/UnifiedTestOperations/UnifiedEntityMap.cs:548`
- **Purpose:** Controls OIDC test environment
- **Values:** `test`, `azure`, `gcp`, `k8s`

### `OIDC_TOKEN_DIR`
**Directory containing OIDC tokens**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Specifications/auth/OidcAuthenticationProseTests.cs:53, 559`
    - `src/MongoDB.Driver/Authentication/Oidc/OidcCallbackAdapterFactory.cs:65` (as `OIDC_TOKEN_FILE`)
- **Purpose:** Path to OIDC token files for testing

### `TOKEN_RESOURCE`
**OIDC token resource identifier**
- **Used in:**
    - `tests/MongoDB.Driver.Tests/Specifications/auth/OidcAuthenticationProseTests.cs:617`
    - `tests/MongoDB.Driver.Tests/UnifiedTestOperations/UnifiedEntityMap.cs:554`
- **Purpose:** OIDC token resource parameter

---

## 7. Client-Side Field Level Encryption (CSFLE)

### `CRYPT_SHARED_LIB_PATH`
**Path to crypt_shared library**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:255`
    - `tests/MongoDB.Driver.Tests/Encryption/AutoEncryptionTests.cs:129`
- **Purpose:** Path to MongoDB crypt_shared dynamic library

### `LIBMONGOCRYPT_PATH`
**Path to libmongocrypt library**
- **Used in:**
    - `src/MongoDB.Driver.Encryption/LibraryLoader.cs:31`
- **Purpose:** Path to libmongocrypt library for encryption

### AWS KMS Credentials (FLE)

#### `FLE_AWS_KEY`
- **Used in:** `EncryptionTestHelper.cs:45, 52`
- **Purpose:** AWS access key for FLE KMS

#### `FLE_AWS_SECRET`
- **Used in:** `EncryptionTestHelper.cs:46, 53`
- **Purpose:** AWS secret key for FLE KMS

#### `FLE_AWS_KEY2` / `FLE_AWS_SECRET2`
- **Used in:** `EncryptionTestHelper.cs:59, 60`
- **Purpose:** Secondary AWS credentials for FLE tests

### Azure KMS Credentials (FLE)

#### `FLE_AZURE_TENANTID`
- **Used in:** `EncryptionTestHelper.cs:84, 92`
- **Purpose:** Azure tenant ID for FLE

#### `FLE_AZURE_CLIENTID`
- **Used in:** `EncryptionTestHelper.cs:85, 93`
- **Purpose:** Azure client ID for FLE

#### `FLE_AZURE_CLIENTSECRET`
- **Used in:** `EncryptionTestHelper.cs:86, 94`
- **Purpose:** Azure client secret for FLE

### GCP KMS Credentials (FLE)

#### `FLE_GCP_EMAIL`
- **Used in:** `EncryptionTestHelper.cs:100, 107`
- **Purpose:** GCP service account email for FLE

#### `FLE_GCP_PRIVATEKEY`
- **Used in:** `EncryptionTestHelper.cs:101, 108`
- **Purpose:** GCP private key for FLE

### FLE Test Controls

#### `CSFLE_AWS_TEMPORARY_CREDS_ENABLED`
- **Used in:** `EncryptionTestHelper.cs:125`
- **Purpose:** Enable AWS temporary credentials for FLE tests

#### `CSFLE_AWS_TEMP_ACCESS_KEY_ID`
- **Used in:** `EncryptionTestHelper.cs:131, 139`
- **Purpose:** Temporary AWS access key for FLE

#### `CSFLE_AWS_TEMP_SECRET_ACCESS_KEY`
- **Used in:** `EncryptionTestHelper.cs:132, 140`
- **Purpose:** Temporary AWS secret key for FLE

#### `CSFLE_AWS_TEMP_SESSION_TOKEN`
- **Used in:** `EncryptionTestHelper.cs:133`
- **Purpose:** Temporary AWS session token for FLE

#### `CSFLE_AZURE_KMS_TESTS_ENABLED`
- **Used in:** `ClientEncryptionProseTests.cs:2028`
- **Purpose:** Enable Azure KMS integration tests

#### `CSFLE_GCP_KMS_TESTS_ENABLED`
- **Used in:** `ClientEncryptionProseTests.cs:2053`
- **Purpose:** Enable GCP KMS integration tests

### `MONGODB_BINARIES`
**Path to MongoDB binaries directory**
- **Used in:**
    - `EncryptionTestHelper.cs:35`
- **Purpose:** Path to mongocryptd and other MongoDB binaries
- **Default:** Empty string

### `FLE_MONGOCRYPTD_PORT`
**Port for mongocryptd process**
- **Used in:**
    - `EncryptionTestHelper.cs:174`
- **Purpose:** Custom port for mongocryptd service

---

## 8. GCP Integration

### `GCE_METADATA_HOST`
**Google Compute Engine metadata host**
- **Used in:**
    - `src/MongoDB.Driver/Authentication/External/GcpAuthenticationCredentialsProvider.cs:64`
    - `tests/MongoDB.Driver.Tests/Specifications/client-side-encryption/prose-tests/ClientEncryptionProseTests.cs:2078`
- **Purpose:** Custom GCE metadata server endpoint
- **Default:** `metadata.google.internal`

### `AZURE_IMDS_MOCK_ENDPOINT`
**Azure IMDS mock endpoint**
- **Used in:**
    - `ClientEncryptionProseTests.cs:2167`
- **Purpose:** Mock Azure Instance Metadata Service endpoint for testing
- **Required:** For Azure IMDS tests

---

## 9. Testing & Configuration

### `MONGO_LOGGING`
**Enable MongoDB driver logging**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:171`
- **Purpose:** Enable test logging output

### `MONGO_SERVER_SELECTION_TIMEOUT_MS`
**Server selection timeout**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:448`
    - `tests/MongoDB.Driver.TestHelpers/DriverTestConfiguration.cs:181`
- **Purpose:** Override default server selection timeout for tests

### `MONGODB_API_VERSION`
**MongoDB Server API version**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/CoreTestConfiguration.cs:281`
- **Purpose:** Specify Server API version for tests

### `SKIPTESTSTHATREQUIRESERVER`
**Skip server-dependent tests**
- **Used in:**
    - `tests/MongoDB.Driver.TestHelpers/Core/XunitExtensions/RequireServer.cs:31`
- **Purpose:** Skip integration tests when no server available

### `DRIVER_PACKAGE_VERSION`
**Expected driver package version**
- **Used in:**
    - `tests/SmokeTests/MongoDB.Driver.SmokeTests.Sdk/ValidatePackagesVersionTests.cs:31`
- **Purpose:** Validate NuGet package versions in smoke tests

---

## 10. Astrolabe Workload Executor

### `RESULTS_DIR`
**Output directory for Astrolabe results**
- **Used in:**
    - `tests/AstrolabeWorkloadExecutor/Program.cs:44`
- **Purpose:** Directory to write test results
- **Default:** Empty string

### `ASYNC`
**Run workload asynchronously**
- **Used in:**
    - `tests/AstrolabeWorkloadExecutor/Program.cs:54`
- **Purpose:** Control sync vs async execution
- **Required:** Must be set to `true` or `false`

---

## 11. OCSP Testing

### Custom OCSP test variables
- **Used in:**
    - `tests/MongoDB.Driver.Tests/OcspIntegrationTests.cs:116`
- **Purpose:** Control OCSP certificate validation tests
- **Variable name:** Dynamically determined per test

---

## Summary Statistics

- **Total unique environment variables:** ~60+
- **Production code usage:** 14 variables (FaaS detection, GCP auth, library paths)
- **Test-only usage:** ~46 variables
- **Categories:**
    - Connection/URI: 4
    - AWS Authentication: 8
    - FaaS Detection: 7
    - X.509 Certificates: 2
    - GSSAPI/Kerberos: 2
    - OIDC: 3
    - Client-Side Encryption: 20+
    - GCP/Azure: 3
    - Testing Configuration: 5
    - Astrolabe: 2
    - Other: 4+

---

## Production vs Test Usage

### Production Code (src/)
These variables affect production driver behavior:
- FaaS detection: `AWS_EXECUTION_ENV`, `AWS_LAMBDA_RUNTIME_API`, `FUNCTIONS_WORKER_RUNTIME`, `K_SERVICE`, `FUNCTION_NAME`, `VERCEL`, `AWS_REGION`
- GCP authentication: `GCE_METADATA_HOST`
- Library loading: `LIBMONGOCRYPT_PATH`

### Test Code (tests/)
All other variables are used exclusively in test code for:
- Test configuration
- Integration test credentials
- Test environment selection
- Mock/stub configuration
