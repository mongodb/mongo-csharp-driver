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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Tests.Specifications.client_side_encryption
{
    public static class EncryptionTestHelper
    {
        public static void ConfigureDefaultExtraOptions(Dictionary<string, object> extraOptions)
        {
            Ensure.IsNotNull(extraOptions, nameof(extraOptions));

            if (!extraOptions.ContainsKey("mongocryptdSpawnPath"))
            {
                var mongocryptdSpawnPath = Environment.GetEnvironmentVariable("MONGODB_BINARIES") ?? "";
                extraOptions.Add("mongocryptdSpawnPath", mongocryptdSpawnPath);
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

            if (Environment.GetEnvironmentVariable("TEST_MONGOCRYPTD") == null)
            {
                var cryptSharedLibPath = CoreTestConfiguration.GetCryptSharedLibPath();
                if (cryptSharedLibPath != null)
                {
                    extraOptions.Add("cryptSharedLibPath", cryptSharedLibPath);
                }
            }
        }

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
    }
}
