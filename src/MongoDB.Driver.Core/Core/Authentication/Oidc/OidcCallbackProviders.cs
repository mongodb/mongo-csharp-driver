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
    /// Oidc client info.
    /// </summary>
    public sealed class OidcClientInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OidcClientInfo" /> class.
        /// </summary>
        /// <param name="principalName">The principal name.</param>
        public OidcClientInfo(string principalName)
        {
            PrincipalName = principalName;
            Version = 0;
        }

        /// <summary>
        /// The principal name.
        /// </summary>
        public string PrincipalName { get; }

        /// <summary>
        /// A version identifying breaking changes in the callback protocol.
        /// </summary>
        public int Version { get; }
    }

    /// <summary>
    /// Represents OIDC request callback provider.
    /// </summary>
    public interface IOidcRequestCallbackProvider
    {
        /// <summary>
        /// Get OIDC request token.
        /// </summary>
        /// <param name="clientInfo">The client info.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        BsonDocument GetTokenResult(OidcClientInfo clientInfo, BsonDocument saslResponse, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC request token.
        /// </summary>
        /// <param name="clientInfo">The client info.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        Task<BsonDocument> GetTokenResultAsync(OidcClientInfo clientInfo, BsonDocument saslResponse, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents OIDC refresh callback provider.
    /// </summary>
    public interface IOidcRefreshCallbackProvider
    {
        /// <summary>
        /// Get OIDC refresh token.
        /// </summary>
        /// <param name="clientInfo">The client info.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="previousCallbackAuthenticationData">The previous callback authentication data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        BsonDocument GetTokenResult(OidcClientInfo clientInfo, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC refresh token.
        /// </summary>
        /// <param name="clientInfo">The client info.</param>
        /// <param name="saslResponse">The server sasl response.</param>
        /// <param name="previousCallbackAuthenticationData">The previous callback authentication data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        Task<BsonDocument> GetTokenResultAsync(OidcClientInfo clientInfo, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken);
    }
}
