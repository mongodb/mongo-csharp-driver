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
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.External
{
    internal sealed class AwsCredentials : IExternalCredentials
    {
        private readonly string _accessKeyId;
        private readonly SecureString _secretAccessKey;
        private readonly string _sessionToken;

        public AwsCredentials(string accessKeyId, string secretAccessKey, string sessionToken)
            : this(accessKeyId, SecureStringHelper.ToSecureString(Ensure.IsNotNull(secretAccessKey, nameof(secretAccessKey))), sessionToken)
        {
        }

        public AwsCredentials(string accessKeyId, SecureString secretAccessKey, string sessionToken)
        {
            _accessKeyId = Ensure.IsNotNull(accessKeyId, nameof(accessKeyId));
            _secretAccessKey = Ensure.IsNotNull(secretAccessKey, nameof(secretAccessKey));
            _sessionToken = sessionToken; // can be null
        }

        public string AccessKeyId => _accessKeyId;

        /// <summary>
        /// Expiration and caching related logic happens on AWS.SDK side.
        /// </summary>
        public DateTime? Expiration => (DateTime?)null;
        public SecureString SecretAccessKey => _secretAccessKey;
        public string SessionToken => _sessionToken;
        public bool ShouldBeRefreshed => true;

        public BsonDocument GetKmsCredentials() =>
            new BsonDocument
            {
                { "accessKeyId", _accessKeyId },
                { "secretAccessKey", SecureStringHelper.ToInsecureString(_secretAccessKey) },
                { "sessionToken", _sessionToken, _sessionToken != null }
            };
    }

    internal class AwsAuthenticationCredentialsProvider : IExternalAuthenticationCredentialsProvider<AwsCredentials>, ICredentialsCache<AwsCredentials>
    {
        private readonly object _lock = new object();

        public void Clear()
        {
            lock (_lock)
            {
                FallbackCredentialsFactory.Reset();
            }
        }

        public AwsCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken)
        {
            AWSCredentials credentialsSource;
            lock (_lock)
            {
                // returns cached credentials source immediately. Only if cached source unavailable, makes quite heavy steps
                credentialsSource = FallbackCredentialsFactory.GetCredentials();
            }
            var immutableCredentials = credentialsSource.GetCredentials();
            return CreateAwsCredentials(immutableCredentials);
        }

        public async Task<AwsCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken)
        {
            AWSCredentials credentialsSource;
            lock (_lock)
            {
                // returns cached credentials source immediately. Only if cached source unavailable, makes quite heavy steps
                credentialsSource = FallbackCredentialsFactory.GetCredentials();
            }
            var immutableCredentials = await credentialsSource.GetCredentialsAsync().ConfigureAwait(false);
            return CreateAwsCredentials(immutableCredentials);
        }

        private AwsCredentials CreateAwsCredentials(ImmutableCredentials immutableCredentials)
        {
            var token = immutableCredentials.Token;
            return new AwsCredentials(immutableCredentials.AccessKey, immutableCredentials.SecretKey, string.IsNullOrEmpty(token) ? null : token);
        }
    }
}
