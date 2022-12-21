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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    /// <summary>
    /// Request callback definition.
    /// </summary>
    /// <param name="principalName">The principal name.</param>
    /// <param name="saslResponse">The sasl response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A user response.</returns>
    public delegate BsonDocument RequestCallback(string principalName, BsonDocument saslResponse, CancellationToken cancellationToken);
    /// <summary>
    /// Request callback definition.
    /// </summary>
    /// <param name="principalName">The principal name.</param>
    /// <param name="saslResponse">The sasl response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A user response.</returns>
    public delegate Task<BsonDocument> RequestCallbackAsync(string principalName, BsonDocument saslResponse, CancellationToken cancellationToken);
    /// <summary>
    /// Request callback definition.
    /// </summary>
    /// <param name="principalName">The principal name.</param>
    /// <param name="saslResponse">The sasl response.</param>
    /// <param name="previousCallbackAuthenticationData">The previous callback authentication data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A user response.</returns>
    public delegate BsonDocument RefreshCallback(string principalName, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken);
    /// <summary>
    /// Request callback definition.
    /// </summary>
    /// <param name="principalName">The principal name.</param>
    /// <param name="saslResponse">The sasl response.</param>
    /// /// <param name="previousCallbackAuthenticationData">The previous callback authentication data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A user response.</returns>
    public delegate Task<BsonDocument> RefreshCallbackAsync(string principalName, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken);

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

    internal sealed class RequestCallbackProvider : IRequestCallbackProvider
    {
        #region static
        public static RequestCallbackProvider CreateIfConfigured(
            RequestCallback requestCallbackFunc,
            RequestCallbackAsync requestCallbackAsyncFunc) =>
            requestCallbackFunc != null || requestCallbackAsyncFunc != null
                ? new RequestCallbackProvider(requestCallbackFunc, requestCallbackAsyncFunc)
                : null;
        #endregion

        private readonly (RequestCallback Func, bool IsProvided) _requestCallbackFunc;
        private readonly (RequestCallbackAsync Func, bool IsProvided) _requestCallbackAsyncFunc;

        public RequestCallbackProvider(
            RequestCallback requestCallbackFunc,
            RequestCallbackAsync requestCallbackAsyncFunc)
        {
            Ensure.That(requestCallbackFunc != null || requestCallbackAsyncFunc != null, $"{MongoOidcAuthenticator.RequestCallbackName} must be provided.");
            _requestCallbackFunc = (requestCallbackFunc ?? ((principalName, saslResponse, cancellationToken) => requestCallbackAsyncFunc(principalName, saslResponse, cancellationToken).GetAwaiter().GetResult()), IsProvided: requestCallbackFunc != null);
            _requestCallbackAsyncFunc = (requestCallbackAsyncFunc ?? ((principalName, saslResponse, cancellationToken) => Task.FromResult(requestCallbackFunc(principalName, saslResponse, cancellationToken))), IsProvided: requestCallbackAsyncFunc != null);
        }

        public BsonDocument GetTokenResult(string principalName, BsonDocument saslResponse, CancellationToken cancellationToken) => _requestCallbackFunc.Func(principalName, saslResponse, cancellationToken);
        public Task<BsonDocument> GetTokenResultAsync(string principalName, BsonDocument saslResponse, CancellationToken cancellationToken) => _requestCallbackAsyncFunc.Func(principalName, saslResponse, cancellationToken);

        public override bool Equals(object obj)
        {
            if (obj is not RequestCallbackProvider requestCallbackProvider)
            {
                return false;
            }
            return
                Equals(_requestCallbackFunc, requestCallbackProvider._requestCallbackFunc) &&
                Equals(_requestCallbackAsyncFunc, requestCallbackProvider._requestCallbackAsyncFunc);

            static bool Equals<TFunc>((TFunc Func, bool IsProvided) a, (TFunc Func, bool IsProvided) b) =>
                // compare only if both callbacks are user provided
                a.IsProvided && b.IsProvided
                ? object.ReferenceEquals(a.Func, b.Func)
                // if no, they're equal only if both provided as nulls
                : !a.IsProvided && !b.IsProvided;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    internal sealed class RefreshCallbackProvider : IRefreshCallbackProvider
    {
        #region static
        public static RefreshCallbackProvider CreateIfConfigured(
            RefreshCallback refreshCallbackFunc,
            RefreshCallbackAsync refreshCallbackAsyncFunc) =>
            refreshCallbackFunc != null || refreshCallbackAsyncFunc != null
                ? new RefreshCallbackProvider(refreshCallbackFunc, refreshCallbackAsyncFunc)
                : null;
        #endregion

        private readonly (RefreshCallback Func, bool IsProvided) _refreshCallbackFunc;
        private readonly (RefreshCallbackAsync Func, bool IsProvided) _refreshCallbackAsyncFunc;

        public RefreshCallbackProvider(
            RefreshCallback refreshCallbackFunc,
            RefreshCallbackAsync refreshCallbackAsyncFunc)
        {
            Ensure.That(refreshCallbackFunc != null || refreshCallbackAsyncFunc != null, $"{MongoOidcAuthenticator.RefreshCallbackName} must be provided.");
            _refreshCallbackFunc = (refreshCallbackFunc ?? ((principalName, saslResponse, previousCallbackAuthenticationData, cancellationToken) => refreshCallbackAsyncFunc(principalName, saslResponse, previousCallbackAuthenticationData, cancellationToken).GetAwaiter().GetResult()), IsProvided: refreshCallbackFunc != null);
            _refreshCallbackAsyncFunc = (refreshCallbackAsyncFunc ?? ((principalName, saslResponse, previousCallbackAuthenticationData, cancellationToken) => Task.FromResult(refreshCallbackFunc(principalName, saslResponse, previousCallbackAuthenticationData, cancellationToken))), IsProvided: refreshCallbackAsyncFunc != null);
        }

        public BsonDocument GetTokenResult(string principalName, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken) => _refreshCallbackFunc.Func(principalName, saslResponse, previousCallbackAuthenticationData, cancellationToken);
        public Task<BsonDocument> GetTokenResultAsync(string principalName, BsonDocument saslResponse, BsonDocument previousCallbackAuthenticationData, CancellationToken cancellationToken) => _refreshCallbackAsyncFunc.Func(principalName, saslResponse, previousCallbackAuthenticationData, cancellationToken);

        public override bool Equals(object obj)
        {
            if (obj is not RefreshCallbackProvider requestCallbackProvider)
            {
                return false;
            }

            return
                Equals(_refreshCallbackFunc, requestCallbackProvider._refreshCallbackFunc) &&
                Equals(_refreshCallbackAsyncFunc, requestCallbackProvider._refreshCallbackAsyncFunc);

            static bool Equals<TFunc>((TFunc Func, bool IsProvided) a, (TFunc Func, bool IsProvided) b) =>
                // compare only if both callbacks are user provided
                a.IsProvided && b.IsProvided
                ? object.ReferenceEquals(a.Func, b.Func)
                // if no, they're equal only if both provided as nulls
                : !a.IsProvided && !b.IsProvided;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
