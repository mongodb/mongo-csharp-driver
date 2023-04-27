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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;
using Reflector = MongoDB.Bson.TestHelpers.Reflector;

namespace MongoDB.Driver.Core.TestHelpers.Authentication
{
    public delegate BsonDocument OidcTokenRequestCallback(BsonDocument idpServerInfo, CancellationToken cancellationToken);
    public delegate Task<BsonDocument> OidcTokenRequestCallbackAsync(BsonDocument idpServerInfo, CancellationToken cancellationToken);
    public delegate BsonDocument OidcTokenRefreshCallback(BsonDocument idpServerInfo, OidcRefreshParameters parameteres, CancellationToken cancellationToken);
    public delegate Task<BsonDocument> OidcTokenRefreshCallbackAsync(BsonDocument idpServerInfo, OidcRefreshParameters parameteres, CancellationToken cancellationToken);

    public sealed class RequestCallbackProvider : IOidcRequestCallbackProvider
    {
        private readonly bool _autoGenerateMissedCallback;
        private readonly OidcTokenRequestCallback _requestCallbackFunc;
        private readonly OidcTokenRequestCallbackAsync _requestCallbackAsyncFunc;

        public RequestCallbackProvider(OidcTokenRequestCallback requestCallbackFunc, OidcTokenRequestCallbackAsync requestCallbackAsyncFunc = null, bool autoGenerateMissedCallback = true)
        {
            _autoGenerateMissedCallback = autoGenerateMissedCallback;
            Ensure.That(requestCallbackFunc != null || requestCallbackAsyncFunc != null, "At least one request callback must be provided.");
            _requestCallbackFunc = requestCallbackFunc;
            _requestCallbackAsyncFunc = requestCallbackAsyncFunc;
        }

        public BsonDocument GetTokenResult(BsonDocument idpServerInfo, CancellationToken cancellationToken) =>
            _requestCallbackFunc != null
                ? _requestCallbackFunc(idpServerInfo, cancellationToken)
                : (_autoGenerateMissedCallback ? _requestCallbackAsyncFunc(idpServerInfo, cancellationToken).GetAwaiter().GetResult() : null);
        

        public Task<BsonDocument> GetTokenResultAsync(BsonDocument idpServerInfo, CancellationToken cancellationToken) =>
            _requestCallbackAsyncFunc != null
                ? _requestCallbackAsyncFunc(idpServerInfo, cancellationToken)
                : (_autoGenerateMissedCallback ? Task.Run(() => _requestCallbackFunc(idpServerInfo, cancellationToken)) : null);

        public override bool Equals(object obj)
        {
            if (obj is not RequestCallbackProvider requestCallbackProvider)
            {
                return false;
            }
            return
                object.ReferenceEquals(_requestCallbackFunc, requestCallbackProvider._requestCallbackFunc) &&
                object.ReferenceEquals(_requestCallbackAsyncFunc, requestCallbackProvider._requestCallbackAsyncFunc);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    public sealed class RefreshCallbackProvider : IOidcRefreshCallbackProvider
    {
        private readonly bool _autoGenerateMissedCallback;
        private readonly OidcTokenRefreshCallback _refreshCallbackFunc;
        private readonly OidcTokenRefreshCallbackAsync _refreshCallbackAsyncFunc;

        public RefreshCallbackProvider(OidcTokenRefreshCallback refreshCallbackFunc, OidcTokenRefreshCallbackAsync refreshCallbackAsyncFunc = null, bool autoGenerateMissedCallback = true)
        {
            _autoGenerateMissedCallback = autoGenerateMissedCallback;
            Ensure.That(refreshCallbackFunc != null || refreshCallbackAsyncFunc != null, "At least one refresh callback must be provided.");
            _refreshCallbackFunc = refreshCallbackFunc;
            _refreshCallbackAsyncFunc = refreshCallbackAsyncFunc;
        }

        public BsonDocument GetTokenResult(BsonDocument idpServerInfo, OidcRefreshParameters parameters, CancellationToken cancellationToken) =>
            _refreshCallbackFunc != null
                ? _refreshCallbackFunc(idpServerInfo, parameters, cancellationToken)
                : (_autoGenerateMissedCallback ? _refreshCallbackAsyncFunc(idpServerInfo, parameters, cancellationToken).GetAwaiter().GetResult() : null);

        public Task<BsonDocument> GetTokenResultAsync(BsonDocument idpServerInfo, OidcRefreshParameters parameters, CancellationToken cancellationToken) =>
            _refreshCallbackAsyncFunc != null
                ? _refreshCallbackAsyncFunc(idpServerInfo, parameters, cancellationToken)
                : (_autoGenerateMissedCallback ? Task.Run(() => _refreshCallbackFunc(idpServerInfo, parameters, cancellationToken)) : null);

        public override bool Equals(object obj)
        {
            if (obj is not RefreshCallbackProvider requestCallbackProvider)
            {
                return false;
            }

            return
                object.ReferenceEquals(_refreshCallbackFunc, requestCallbackProvider._refreshCallbackFunc) &&
                object.ReferenceEquals(_refreshCallbackAsyncFunc, requestCallbackProvider._refreshCallbackAsyncFunc);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    public static class OidcTestHelper
    {
        private static readonly string[] __supportedFieldsInServerResponse = new[]
        {
            "clientId",
            "requestScopes",
            "issuer"
        };

        private static readonly string[] __supportedCallbackResponse = new[]
        {
            "accessToken",
            "expiresInSeconds",
            "refreshToken"
        };

        /// <summary>
        /// NOTE: externalAuthenticators must be an instance of <see cref="ExternalCredentialsAuthenticators"/>.
        /// </summary>
        public static void ClearStaticCache(object externalAuthenticators)
        {
            var externalAuthenticator = externalAuthenticators.Should().BeOfType<ExternalCredentialsAuthenticators>().Subject;
            var clock = externalAuthenticator.Oidc._clock();
            externalAuthenticator._oidcProvidersCache(new Lazy<IOidcProvidersCache>(() => new OidcProvidersCache(clock))); // clear cache 
        }

        public static IOidcRequestCallbackProvider CreateRequestCallback(
            bool validateInput = true,
            bool validateToken = true,
            int? expireInSeconds = 600, // 10 mins
            bool invalidResponseDocument = false,
            string accessToken = null,
            Action<BsonDocument, CancellationToken> callbackCalled = null,
            BsonDocument expectedIdpServerInfo = null) =>
            new RequestCallbackProvider((idpServerInfo, ct) =>
            {
                if (expectedIdpServerInfo != null)
                {
                    idpServerInfo.Should().Be(expectedIdpServerInfo);
                }

                accessToken = validateToken ? JwtHelper.GetValidTokenOrThrow(accessToken) : JwtHelper.GetTokenContent(accessToken);

                if (validateInput)
                {
                    idpServerInfo
                        .Elements
                        .Select(c => c.Name)
                        .Should()
                        .OnlyContain(e => __supportedFieldsInServerResponse.Contains(e));
                }

                callbackCalled?.Invoke(idpServerInfo, ct);
                var response = new BsonDocument
                {
                    { "accessToken", accessToken },
                    { "expiresInSeconds", () => expireInSeconds, expireInSeconds.HasValue },
                    { "invalidField", invalidResponseDocument, invalidResponseDocument }
                };
                return response;
            });


        public static IOidcRefreshCallbackProvider CreateRefreshCallback(
            bool validateInput = true,
            bool validateToken = true,
            int? expireInSeconds = 600, // 10 mins
            bool invalidResponseDocument = false,
            string accessToken = null,
            Action<BsonDocument, OidcRefreshParameters, CancellationToken> callbackCalled = null,
            BsonDocument expectedIdpServerInfo = null) =>
            new RefreshCallbackProvider((idpServerInfo, parameters, ct) =>
            {
                if (expectedIdpServerInfo != null)
                {
                    idpServerInfo.Should().Be(expectedIdpServerInfo);
                }

                accessToken = validateToken ? JwtHelper.GetValidTokenOrThrow(accessToken) : JwtHelper.GetTokenContent(accessToken);

                if (validateInput)
                {
                    idpServerInfo
                        .Elements
                        .Select(c => c.Name)
                        .Should()
                        .OnlyContain(e => __supportedFieldsInServerResponse.Contains(e));
                    //parameters
                    //    .Elements
                    //    .Select(c => c.Name)
                    //    .Should()
                    //    .OnlyContain(e => __supportedCallbackResponse.Contains(e));
                }

                callbackCalled?.Invoke(idpServerInfo, parameters, ct);

                var response = new BsonDocument // OIDCRequestTokenResult
                {
                    { "accessToken", accessToken },
                    { "expiresInSeconds", () => expireInSeconds, expireInSeconds.HasValue },
                    { "invalidField", invalidResponseDocument, invalidResponseDocument }
                };
                return response;
            });

        /// <summary>
        /// NOTE: externalAuthenticators must be an instance of <see cref="ExternalCredentialsAuthenticators"/>.
        /// </summary>
        public static TCacheProvider GetOidcProvidersCache<TExternalAuthenticators, TCacheProvider>(TExternalAuthenticators externalAuthenticators)
        {
            var subject = ConvertTo<TExternalAuthenticators, ExternalCredentialsAuthenticators>(externalAuthenticators).Oidc;
            return ConvertTo<IOidcProvidersCache, TCacheProvider>(subject);
        }

        /// <summary>
        /// NOTE: externalAuthenticators must be an instance of <see cref="ExternalCredentialsAuthenticators"/>.
        /// </summary>
        public static Dictionary<TKey, TOidcCredentialsProvider> GetCachedOidcProviders<TExternalAuthenticators, TKey, TOidcCredentialsProvider>(TExternalAuthenticators externalAuthenticators)
        {
            var cache = GetOidcProvidersCache<TExternalAuthenticators, OidcProvidersCache>(externalAuthenticators);
            return cache.CachedProviders().ToDictionary(k => ConvertTo<OidcInputConfiguration, TKey>(k.Key), v => ConvertTo<IOidcExternalAuthenticationCredentialsProvider, TOidcCredentialsProvider>(v.Value.Provider));
        }

        public static void SetAwsForOidcExternalAuthenticationCredentialsProvider<TExternalAuthenticators, TAwsForOidcProvider>(TExternalAuthenticators externalAuthenticators, TAwsForOidcProvider credentialsProvider)
        {
            var subject = ConvertTo<TExternalAuthenticators, ExternalCredentialsAuthenticators>(externalAuthenticators);
            var awsForOidcProvider = ConvertTo<TAwsForOidcProvider, IExternalAuthenticationCredentialsProvider<OidcCredentials>>(credentialsProvider);
            subject._awsForOidcExternalAuthenticationCredentialsProvider(new Lazy<IExternalAuthenticationCredentialsProvider<OidcCredentials>>(() => awsForOidcProvider));
        }

        private static TTo ConvertTo<TFrom, TTo>(TFrom input) =>
            // some types are internal and can't be provided as input arguments in a public method
            input.Should().BeAssignableTo<TTo>().Subject;
    }

    internal static class ExternalCredentialsAuthenticatorsReflector
    {
        internal static Lazy<OidcProvidersCache> _oidcProvidersCache(this ExternalCredentialsAuthenticators obj) =>
            (Lazy<OidcProvidersCache>)Reflector.GetFieldValue(obj, nameof(_oidcProvidersCache));
        internal static void _oidcProvidersCache(this ExternalCredentialsAuthenticators obj, Lazy<IOidcProvidersCache> cacheValue) =>
            Reflector.SetFieldValue(obj, nameof(_oidcProvidersCache), cacheValue);
        public static void _awsForOidcExternalAuthenticationCredentialsProvider(this ExternalCredentialsAuthenticators obj, Lazy<IExternalAuthenticationCredentialsProvider<OidcCredentials>> value) =>
            Reflector.SetFieldValue(obj, nameof(_awsForOidcExternalAuthenticationCredentialsProvider), value);
    }

    internal static class OidcProvidersCacheReflector
    {
        internal static IDictionary<OidcInputConfiguration, OidcCacheValue> CachedProviders(this IOidcProvidersCache obj) =>
            (IDictionary<OidcInputConfiguration, OidcCacheValue>)Reflector.GetFieldValue(obj, "_providersCache");

        internal static IClock _clock(this IOidcProvidersCache obj) => (IClock)Reflector.GetFieldValue(obj, nameof(_clock));
    }
}
