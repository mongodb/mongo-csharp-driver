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

namespace MongoDB.Driver.Authentication.AWS.CredentialsSources
{
    internal sealed class AWSInstanceCredentialsSource : IAWSCredentialsSource
    {
        private readonly AWSCredentials _credentials;

        public AWSInstanceCredentialsSource(AWSCredentials credentials)
        {
            _credentials = credentials;
        }

        public void Dispose()
        {
        }

        public AWSCredentials GetCredentials(CancellationToken cancellationToken)
            => _credentials;

        public Task<AWSCredentials> GetCredentialsAsync(CancellationToken cancellationToken)
            => Task.FromResult(_credentials);

        public void ResetCache()
        {
        }
    }
}
