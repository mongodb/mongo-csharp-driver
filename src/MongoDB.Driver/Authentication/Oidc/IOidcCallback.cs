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
using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Authentication.Oidc
{
    /// <summary>
    /// Represents OIDC callback request.
    /// </summary>
    public sealed class OidcCallbackParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OidcCallbackParameters" /> class.
        /// </summary>
        /// <param name="version">Callback API version number.</param>
        /// <param name="userName">User name.</param>
        public OidcCallbackParameters(int version, string userName)
        {
            Version = version;
            UserName = userName;
        }

        /// <summary>
        /// Callback API version number.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// User name.
        /// </summary>
        public string UserName { get; }
    }

    /// <summary>
    /// Represents OIDC callback response.
    /// </summary>
    public sealed class OidcAccessToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OidcAccessToken" /> class.
        /// </summary>
        /// <param name="accessToken">OIDC Access Token string.</param>
        /// <param name="expiresIn">Expiration duration for the Access Token.</param>
        public OidcAccessToken(string accessToken, TimeSpan? expiresIn)
        {
            AccessToken = accessToken;
            ExpiresIn = expiresIn;
        }

        /// <summary>
        /// OIDC Access Token string.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Expiration duration for the Access Token.
        /// </summary>
        public TimeSpan? ExpiresIn { get; set; }
    }

    /// <summary>
    /// Represents OIDC callback provider.
    /// </summary>
    public interface IOidcCallback
    {
        /// <summary>
        /// Get OIDC callback response.
        /// </summary>
        /// <param name="parameters">The information used by callbacks to authenticate with the Identity Provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>OIDC callback response</returns>
        OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Get OIDC callback response.
        /// </summary>
        /// <param name="parameters">The information used by callbacks to authenticate with the Identity Provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>OIDC callback response</returns>
        Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken);
    }
}
