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

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    /// <summary>
    /// Represents OIDC request callback provider.
    /// </summary>
    public interface IRequestCallbackProvider
    {
        /// <summary>
        /// Get OIDC request token.
        /// </summary>
        /// <param name="principalName">The principal name.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        BsonDocument GetTokenResult(string principalName, BsonDocument saslResponse, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC request token.
        /// </summary>
        /// <param name="principalName">The principal name.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        Task<BsonDocument> GetTokenResultAsync(string principalName, BsonDocument saslResponse, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents OIDC refresh callback provider.
    /// </summary>
    public interface IRefreshCallbackProvider
    {
        /// <summary>
        /// Get OIDC refresh token.
        /// </summary>
        /// <param name="principalName">The principal name.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="previousCallbackAuthenticationData">The previous callback authentication data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        BsonDocument GetTokenResult(string principalName, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC refresh token.
        /// </summary>
        /// <param name="principalName">The principal name.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="previousCallbackAuthenticationData">The previous callback authentication data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        Task<BsonDocument> GetTokenResultAsync(string principalName, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken);
    }
}
