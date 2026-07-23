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

using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;

namespace MongoDB.Driver.Authentication.AWS.CredentialsSources
{
    internal sealed class AWSFallbackCredentialsSource : IAWSCredentialsSource
    {
        public static readonly AWSFallbackCredentialsSource Instance = new();

        private readonly SemaphoreSlim _lock = new(1);
        private Amazon.Runtime.AWSCredentials _cachedCredentialsSource;

        public void Dispose() => _lock?.Dispose();

        public AWSCredentials GetCredentials(CancellationToken cancellationToken)
        {
            Amazon.Runtime.AWSCredentials credentialsSource;
            _lock.Wait(cancellationToken);
            try
            {
                // resolving the credentials source walks the full provider chain, so cache it and reuse until reset
                credentialsSource = _cachedCredentialsSource ??= DefaultAWSCredentialsIdentityResolver.GetCredentials(null);
            }
            finally
            {
                _lock.Release();
            }

            var immutableCredentials = credentialsSource.GetCredentials();
            return CreateAWSCredentials(immutableCredentials);
        }

        public async Task<AWSCredentials> GetCredentialsAsync(CancellationToken cancellationToken)
        {
            Amazon.Runtime.AWSCredentials credentialsSource;
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // resolving the credentials source walks the full provider chain, so cache it and reuse until reset
                credentialsSource = _cachedCredentialsSource ??= await DefaultAWSCredentialsIdentityResolver.GetCredentialsAsync(null).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }

            var immutableCredentials = await credentialsSource.GetCredentialsAsync().ConfigureAwait(false);
            return CreateAWSCredentials(immutableCredentials);
        }

        public void ResetCache()
        {
            _lock.Wait();

            try
            {
                _cachedCredentialsSource = null;
            }
            finally
            {
                _lock.Release();
            }
        }

        private AWSCredentials CreateAWSCredentials(ImmutableCredentials immutableCredentials)
        {
            var token = immutableCredentials.Token;
            return new AWSCredentials(immutableCredentials.AccessKey, immutableCredentials.SecretKey, string.IsNullOrEmpty(token) ? null : token);
        }
    }
}
