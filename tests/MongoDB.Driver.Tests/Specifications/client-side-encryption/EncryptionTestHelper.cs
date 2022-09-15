/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    public static class EncryptionTestHelper
    {
        private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>> __kmsProviders = new Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>>>(ConfigureDefaultKmsProviders, isThreadSafe: true);
        private const string KmsProviderFilterDelimiter = ";";
        private static readonly string __defaultMongocryptdPath = Environment.GetEnvironmentVariable("MONGODB_BINARIES") ?? "";
        private static readonly Lazy<(bool IsValid, SemanticVersion Version)> __defaultCsfleSetupState = new Lazy<(bool IsValid, SemanticVersion Version)>(IsDefaultCsfleSetupValid, isThreadSafe: true);

        private static IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> ConfigureDefaultKmsProviders()
        {
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>
            {
                {
                    "aws", new Dictionary<string, object>
                    {
                        { "accessKeyId", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_ACCESS_KEY_ID") },
                        { "secretAccessKey", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_SECRET_ACCESS_KEY") }
                    }
                },
                {
                    "local", new Dictionary<string, object>
                    {
                        { "key", new BsonBinaryData(Convert.FromBase64String(LocalMasterKey)).Bytes }
                    }
                },
                {
                    "azure", new Dictionary<string, object>
                    {
                        { "tenantId", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_TENANT_ID") },
                        { "clientId", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_CLIENT_ID") },
                        { "clientSecret", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AZURE_CLIENT_SECRET") }
                    }
                },
                {
                    "gcp", new Dictionary<string, object>
                    {
                        { "email", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_GCP_EMAIL") },
                        { "privateKey", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_GCP_PRIVATE_KEY") }
                    }
                },
                {
                    "kmip", new Dictionary<string, object>
                    {
                        { "endpoint", "localhost:5698" }
                    }
                },
            };

            if (Environment.GetEnvironmentVariable("FLE_AWS_TEMPORARY_CREDS_ENABLED") != null)
            {
                kmsProviders.Add(
                    "awsTemporary",
                    new Dictionary<string, object>
                    {
                        { "accessKeyId", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_ACCESS_KEY_ID") },
                        { "secretAccessKey", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_SECRET_ACCESS_KEY") },
                        { "sessionToken", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_SESSION_TOKEN") }
                    });
                kmsProviders.Add(
                    "awsTemporaryNoSessionToken",
                    new Dictionary<string, object>
                    {
                        { "accessKeyId", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_ACCESS_KEY_ID") },
                        { "secretAccessKey", GetEnvironmentVariableOrDefaultOrThrowIfNothing("FLE_AWS_TEMP_SECRET_ACCESS_KEY") }
                    });
            }

            return kmsProviders;

            string GetEnvironmentVariableOrDefaultOrThrowIfNothing(string variableName, string defaultValue = null) =>
                Environment.GetEnvironmentVariable(variableName) ??
                defaultValue ??
                throw new Exception($"{variableName} environment variable must be configured on the machine.");
        }

        private const string LocalMasterKey = "Mng0NCt4ZHVUYUJCa1kxNkVyNUR1QURhZ2h2UzR2d2RrZzh0cFBwM3R6NmdWMDFBMUN3YkQ5aXRRMkhGRGdQV09wOGVNYUMxT2k3NjZKelhaQmRCZGJkTXVyZG9uSjFk";

        public static void ConfigureDefaultExtraOptions(Dictionary<string, object> extraOptions)
        {
            if (CoreTestConfiguration.MaxWireVersion < WireVersion.Server42)
            {
                // CSFLE is supported starting from server 4.2
                return;
            }

            Ensure.IsNotNull(extraOptions, nameof(extraOptions));

            if (!extraOptions.TryGetValue("mongocryptdSpawnPath", out object value))
            {
                extraOptions.Add("mongocryptdSpawnPath", __defaultMongocryptdPath);

                if (!__defaultCsfleSetupState.Value.IsValid)
                {
                    throw new Exception($"The configured mongocryptd version {__defaultCsfleSetupState.Value.Version} doesn't match the server version {CoreTestConfiguration.ServerVersion}.");
                }
            }

            var mongocryptdPort = Environment.GetEnvironmentVariable("FLE_MONGOCRYPTD_PORT");
            if (mongocryptdPort != null)
            {
                if (!extraOptions.ContainsKey("mongocryptdURI"))
                {
                    extraOptions.Add("mongocryptdURI", $"mongodb://localhost:{mongocryptdPort}");
                }

                var portKey = "--port=";
                var portValue = $" {portKey}{mongocryptdPort}";
                if (extraOptions.TryGetValue("mongocryptdSpawnArgs", out var args))
                {
                    object effectiveValue;
                    switch (args)
                    {
                        case string str:
                            {
                                effectiveValue = str.Contains(portKey) ? str : str + portValue;
                            }
                            break;
                        case IEnumerable enumerable:
                            {
                                var list = new List<string>(enumerable.Cast<string>());
                                if (list.Any(v => v.Contains(portKey)))
                                {
                                    effectiveValue = list;
                                }
                                else
                                {
                                    list.Add(portValue);
                                    effectiveValue = list;
                                }
                            }
                            break;
                        default: throw new Exception("Unsupported mongocryptdSpawnArgs type.");
                    }
                    extraOptions["mongocryptdSpawnArgs"] = effectiveValue;
                }
                else
                {
                    extraOptions.Add("mongocryptdSpawnArgs", portValue);
                }
            }

            if (!extraOptions.ContainsKey("cryptSharedLibPath"))
            {
                var cryptSharedLibPath = CoreTestConfiguration.GetCryptSharedLibPath();
                if (cryptSharedLibPath != null)
                {
                    extraOptions.Add("cryptSharedLibPath", cryptSharedLibPath);
                }
            }
        }

        public static string CreateKmsProviderFilter(params string[] kmsProviders) => string.Join(KmsProviderFilterDelimiter, kmsProviders);

        public static BsonDocument CreateMasterKey(string kmsProvider) => kmsProvider switch
        {
            "local" => null,
            "aws" => new BsonDocument
            {
                { "region", "us-east-1" },
                { "key", "arn:aws:kms:us-east-1:579766882180:key/89fcc2c4-08b0-4bd9-9f25-e30687b580d0" }
            },
            "azure" => new BsonDocument
            {
                { "keyName", "key-name-csfle" },
                { "keyVaultEndpoint", "key-vault-csfle.vault.azure.net" }
            },
            "gcp" => new BsonDocument
            {
                { "projectId", "devprod-drivers" },
                { "location", "global" },
                { "keyRing", "key-ring-csfle" },
                { "keyName", "key-name-csfle" }
            },
            "kmip" => new BsonDocument(),
            _ => throw new ArgumentException($"Incorrect kms provider {kmsProvider}.", nameof(kmsProvider)),
        };

        public static Dictionary<string, SslSettings> CreateTlsOptionsIfAllowed(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
            Func<string, bool> allowClientCertificateFunc)
        {
            if (allowClientCertificateFunc != null)
            {
                Dictionary<string, SslSettings> tlsOptions = new();
                foreach (var kmsProvider in kmsProviders)
                {
                    var tlsSettings = CreateTlsOptionsIfAllowed(kmsProvider.Key, allowClientCertificateFunc);
                    if (tlsSettings != null)
                    {
                        tlsOptions.Add(kmsProvider.Key, tlsSettings);
                    }
                }

                return new Dictionary<string, SslSettings>(tlsOptions);
            }

            return null;
        }

        public static SslSettings CreateTlsOptionsIfAllowed(
            string kmsProvider,
            Func<string, bool> allowClientCertificateFunc)
        {
            if (allowClientCertificateFunc != null && allowClientCertificateFunc(kmsProvider))
            {
                var certificateFilename = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PATH");
                var password = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PASSWORD");
                var clientCertificate = new X509Certificate2(Ensure.IsNotNull(certificateFilename, nameof(certificateFilename)), Ensure.IsNotNull(password, nameof(password)));
                var effectiveClientCertificates = clientCertificate != null ? new[] { clientCertificate } : Enumerable.Empty<X509Certificate2>();
                return new SslSettings { ClientCertificates = effectiveClientCertificates };
            }

            return null;
        }

        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> GetKmsProviders(string filter = null) =>
            __kmsProviders
                .Value
                .Where(kms => filter == null || filter.Split(new[] { KmsProviderFilterDelimiter }, StringSplitOptions.None).Contains(kms.Key))
                .ToDictionary(
                    k => k.Key,
                    v => (IReadOnlyDictionary<string, object>)v.Value.ToDictionary(ik => ik.Key, iv => iv.Value)); // ensure that inner dictionary is a new instance

        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> ParseKmsProviders(BsonDocument kmsProviders, bool legacy = false)
        {
            var providers = new Dictionary<string, IReadOnlyDictionary<string, object>>();
            var kmsProvidersFromEnvs = GetKmsProviders();
            foreach (var kmsProvider in kmsProviders.Elements)
            {
                var kmsOptions = new Dictionary<string, object>();
                var effectiveKmsProviderName = kmsProvider.Name;
                switch (kmsProvider.Name)
                {
                    case "awsTemporary":
                    case "awsTemporaryNoSessionToken":
                        {
                            effectiveKmsProviderName = "aws";
                            goto default;
                        }
                    case "local":
                        {
                            byte[] bytes;
                            if (kmsProvider.Value.AsBsonDocument.TryGetElement("key", out var key) && !IsPlaceholder(key.Value))
                            {
                                bytes = key.Value.IsBsonBinaryData ? key.Value.AsBsonBinaryData.Bytes : Convert.FromBase64String(key.Value.AsString);
                            }
                            else
                            {
                                bytes = new BsonBinaryData(Convert.FromBase64String(LocalMasterKey)).Bytes;
                            }
                            kmsOptions.Add("key", bytes);
                        }
                        break;
                    default:
                        {
                            var kmsProviderDocument = kmsProvider.Value.AsBsonDocument;
                            if (legacy)
                            {
                                kmsProviderDocument.ElementCount.Should().Be(0);
                                kmsOptions = (Dictionary<string, object>)kmsProvidersFromEnvs[kmsProvider.Name];
                            }
                            else
                            {
                                foreach (var providedKmsInfo in kmsProviderDocument)
                                {
                                    kmsOptions.Add(
                                        providedKmsInfo.Name,
                                        IsPlaceholder(providedKmsInfo.Value)
                                            ? GetFromEnvVariables(kmsProvider.Name, providedKmsInfo.Name) // use initial kms name
                                            : providedKmsInfo.Value.AsString);
                                }
                            }
                        }
                        break;
                }

                providers.Add(effectiveKmsProviderName, kmsOptions);
            }

            return providers;

            bool IsPlaceholder(BsonValue value) => value.IsBsonDocument && value.AsBsonDocument.Contains("$$placeholder");
            string GetFromEnvVariables(string kmsProvider, string key) => kmsProvidersFromEnvs[kmsProvider][key].ToString();
        }

        // private methods
        /// <summary>
        /// Ensure that used mongocryptd corresponds to used server.
        /// </summary>
        private static (bool IsValid, SemanticVersion MongocryptdVersion) IsDefaultCsfleSetupValid()
        {
            var cryptSharedLibPath = CoreTestConfiguration.GetCryptSharedLibPath();
            if (cryptSharedLibPath != null)
            {
                var dummyNamespace = CollectionNamespace.FromFullName("db.dummy");
                var autoEncryptionOptions = new AutoEncryptionOptions(
                    dummyNamespace,
                    kmsProviders: GetKmsProviders(filter: "local"),
                    extraOptions: new Dictionary<string, object>() { { "cryptSharedLibPath", cryptSharedLibPath } });
                using (var cryptClient = CryptClientCreator.CreateCryptClient(autoEncryptionOptions.ToCryptClientSettings()))
                {
                    if (cryptClient.CryptSharedLibraryVersion != null)
                    {
                        // csfle shared library code path
                        return (IsValid: true, MongocryptdVersion: null);
                    }
                    else
                    {
                        // we will still try using mongocryptd
                    }
                }
            }

            var configuredMongocryptdVersion = GetMongocryptdVersion(__defaultMongocryptdPath);
            if (CompareSemanticVersionsAsReleased(configuredMongocryptdVersion, CoreTestConfiguration.ServerVersion) < 0)
            {
                return (IsValid: false, MongocryptdVersion: configuredMongocryptdVersion);
            }

            return (IsValid: true, MongocryptdVersion: configuredMongocryptdVersion);

            SemanticVersion GetMongocryptdVersion(string mongocryptdPath)
            {
                mongocryptdPath = File.Exists(mongocryptdPath) ? mongocryptdPath : Path.Combine(mongocryptdPath, "mongocryptd");
                using (var process = new Process())
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.Arguments = "--version";
                    process.StartInfo.FileName = mongocryptdPath;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;

                    if (!process.Start())
                    {
                        // skip it. This case can happen if no new process resource is started
                        // (for example, if an existing process is reused)
                    }

                    var output = process.StandardOutput.ReadToEnd();
                    var buildInfoBody = output.Substring(output.IndexOf("Build Info") + 12 /*key length*/);
                    return SemanticVersion.Parse(buildInfoBody);
                }
            }

            int CompareSemanticVersionsAsReleased(SemanticVersion a, SemanticVersion b)
            {
                var aReleased = new SemanticVersion(a.Major, a.Minor, a.Patch);
                var bReleased = new SemanticVersion(b.Major, b.Minor, b.Patch);
                return aReleased.CompareTo(bReleased);
            }
        }
    }
}
