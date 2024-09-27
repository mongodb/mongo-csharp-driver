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
using MongoDB.Bson;
using MongoDB.Driver.Authentication.External;

namespace MongoDB.Driver.Encryption
{
    internal sealed class GcpKmsProvider : IKmsProvider
    {
        public const string ProviderName = "gcp";

        public static readonly IKmsProvider Instance = new GcpKmsProvider();

        public async Task<BsonDocument> GetKmsCredentialsAsync(CancellationToken cancellationToken)
        {
            var credentials = await ExternalCredentialsAuthenticators.Instance.Gcp.CreateCredentialsFromExternalSourceAsync(cancellationToken).ConfigureAwait(false);
            return new BsonDocument("accessToken", credentials.AccessToken);
        }
    }
}
