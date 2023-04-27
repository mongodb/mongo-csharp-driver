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
    public sealed class OidcRefreshParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OidcRefreshParameters" /> class.
        /// </summary>
        /// <param name="refreshToken">The refresh token.</param>
        public OidcRefreshParameters(string refreshToken)
        {
            RefreshToken = refreshToken;
        }

        /// <summary>
        /// The refresh token.
        /// </summary>
        public string RefreshToken { get; }
    }

    /// <summary>
    /// Represents OIDC request callback provider.
    /// </summary>
    public interface IOidcRequestCallbackProvider
    {
        /// <summary>
        /// Get OIDC request token.
        /// </summary>
        /// <param name="idpServerInfo">The information used by callbacks to authenticate with the Identity Provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        BsonDocument GetTokenResult(BsonDocument idpServerInfo, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC request token.
        /// </summary>
        /// <param name="idpServerInfo">The information used by callbacks to authenticate with the Identity Provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        Task<BsonDocument> GetTokenResultAsync(BsonDocument idpServerInfo, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents OIDC refresh callback provider.
    /// </summary>
    public interface IOidcRefreshCallbackProvider
    {
        /// <summary>
        /// Get OIDC refresh token.
        /// </summary>
        /// <param name="idpServerInfo">The information used by callbacks to authenticate with the Identity Provider.</param>
        /// <param name="parameters">The refresh callback parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        BsonDocument GetTokenResult(BsonDocument idpServerInfo, OidcRefreshParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC refresh token.
        /// </summary>
        /// <param name="idpServerInfo">The information used by callbacks to authenticate with the Identity Provider.</param>
        /// <param name="parameters">The refresh callback parameters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The jwt token.</returns>
        Task<BsonDocument> GetTokenResultAsync(BsonDocument idpServerInfo, OidcRefreshParameters parameters, CancellationToken cancellationToken);
    }
}
