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

        public void Dispose()
        {
        }

        public AWSCredentials GetCredentials(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var credentialsSource = DefaultAWSCredentialsIdentityResolver.GetCredentials(null);
            var immutableCredentials = credentialsSource.GetCredentials();
            return CreateAWSCredentials(immutableCredentials);
        }

        public async Task<AWSCredentials> GetCredentialsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var credentialsSource = await DefaultAWSCredentialsIdentityResolver.GetCredentialsAsync(null).ConfigureAwait(false);
            var immutableCredentials = await credentialsSource.GetCredentialsAsync().ConfigureAwait(false);
            return CreateAWSCredentials(immutableCredentials);
        }

        public void ResetCache()
        {
            // No-op: DefaultAWSCredentialsIdentityResolver owns credential caching and invalidates on
            // environment/config changes.
        }

        private AWSCredentials CreateAWSCredentials(ImmutableCredentials immutableCredentials)
        {
            var token = immutableCredentials.Token;
            return new AWSCredentials(immutableCredentials.AccessKey, immutableCredentials.SecretKey, string.IsNullOrEmpty(token) ? null : token);
        }
    }
}
