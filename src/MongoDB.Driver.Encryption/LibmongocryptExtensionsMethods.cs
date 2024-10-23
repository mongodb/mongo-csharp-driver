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

using System.Linq;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;

namespace MongoDB.Driver.Encryption
{
    internal static class LibmongocryptExtensionsMethods
    {
        public static BsonDocument CreateDocument(this RangeOptions rangeOptions)
        {
            return new BsonDocument
            {
                { "min", rangeOptions.Min, rangeOptions.Min != null },
                { "max", rangeOptions.Max, rangeOptions.Max != null },
                { "precision", rangeOptions.Precision, rangeOptions.Precision != null },
                { "sparsity", rangeOptions.Sparsity, rangeOptions.Sparsity != null },
                { "trimFactor", rangeOptions.TrimFactor, rangeOptions.TrimFactor != null }
            };
        }

        public static SslStreamSettings ToSslStreamSettings(this SslSettings sslSettings)
        {
            var clientCertificates = sslSettings.ClientCertificateCollection != null ? (sslSettings.ClientCertificateCollection).Cast<X509Certificate>() : Enumerable.Empty<X509Certificate>();
            return new SslStreamSettings(
                checkCertificateRevocation: sslSettings.CheckCertificateRevocation,
                clientCertificates: Optional.Create(clientCertificates),
                clientCertificateSelectionCallback: sslSettings.ClientCertificateSelectionCallback,
                enabledProtocols: sslSettings.EnabledSslProtocols,
                serverCertificateValidationCallback: sslSettings.ServerCertificateValidationCallback);
        }
    }
}
