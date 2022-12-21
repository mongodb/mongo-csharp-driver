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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Oidc;
using Reflector = MongoDB.Bson.TestHelpers.Reflector;

namespace MongoDB.Driver.Core.TestHelpers.Authentication
{
    public static class OidcTestHelper
    {
        private static readonly string[] __supportedFieldsInServerResponse = new[]
        {
            "authorizationEndpoint",
            "tokenEndpoint",
            "deviceAuthorizationEndpoint",
            "clientId",
            "clientSecret",
            "requestScopes"
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
            externalAuthenticator._oidcAuthenticationCredentialsProviderCache(
                new Lazy<ConcurrentDictionary<OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>>(
                    () => new ConcurrentDictionary<OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>())); // clear cache
        }

        public static RequestCallback CreateRequestCallback(
            string expectedPrincipalName = null,
            bool validateInput = true,
            bool validateToken = true,
            int? expireInSeconds = 600, // 10 mins
            bool invalidResponseDocument = false,
            string accessToken = null,
            Action<string, BsonDocument, CancellationToken> callbackCalled = null,
            BsonDocument expectedSaslResponseDocument = null) =>
            (principalName, serverResponse, ct) =>
            {
                if (expectedPrincipalName != null)
                {
                    principalName.Should().Be(expectedPrincipalName);
                }

                if (expectedSaslResponseDocument != null)
                {
                    serverResponse.Should().Be(expectedSaslResponseDocument);
                }

                accessToken = validateToken ? JwtHelper.GetValidTokenOrThrow(accessToken) : JwtHelper.GetTokenContent(accessToken);

                if (validateInput)
                {
                    serverResponse
                        .Elements
                        .Select(c => c.Name)
                        .Should()
                        .OnlyContain(e => __supportedFieldsInServerResponse.Contains(e));
                }

                callbackCalled?.Invoke(principalName, serverResponse, ct);
                var response = new BsonDocument
                {
                    { "accessToken", accessToken },
                    { "expiresInSeconds", () => expireInSeconds, expireInSeconds.HasValue },
                    { "invalidField", invalidResponseDocument, invalidResponseDocument }
                };
                return response;
            };


        public static RefreshCallback CreateRefreshCallback(
            string expectedPrincipalName = null,
            bool validateInput = true,
            bool validateToken = true,
            int? expireInSeconds = 600, // 10 mins
            bool invalidResponseDocument = false,
            string accessToken = null,
            Action<string, BsonDocument, BsonDocument, CancellationToken> callbackCalled = null,
            BsonDocument expectedSaslResponseDocument = null) =>
            (principalName, serverResponse, previousCache, ct) =>
            {
                if (expectedPrincipalName != null)
                {
                    principalName.Should().Be(expectedPrincipalName);
                }

                if (expectedSaslResponseDocument != null)
                {
                    serverResponse.Should().Be(expectedSaslResponseDocument);
                }

                accessToken = validateToken ? JwtHelper.GetValidTokenOrThrow(accessToken) : JwtHelper.GetTokenContent(accessToken);

                if (validateInput)
                {
                    serverResponse
                        .Elements
                        .Select(c => c.Name)
                        .Should()
                        .OnlyContain(e => __supportedFieldsInServerResponse.Contains(e));
                    previousCache
                        .Elements
                        .Select(c => c.Name)
                        .Should()
                        .OnlyContain(e => __supportedCallbackResponse.Contains(e));
                }

                callbackCalled?.Invoke(principalName, serverResponse, previousCache, ct);

                var response = new BsonDocument // OIDCRequestTokenResult
                {
                    { "accessToken", accessToken },
                    { "expiresInSeconds", () => expireInSeconds, expireInSeconds.HasValue },
                    { "invalidField", invalidResponseDocument, invalidResponseDocument }
                };
                return response;
            };

        /// <summary>
        /// NOTE: externalAuthenticators must be an instance of <see cref="ExternalCredentialsAuthenticators"/>.
        /// </summary>
        public static Lazy<ConcurrentDictionary<TKey, TOidcCredentialsProvider>> GetOidcProvidersCache<TExternalAuthenticators, TKey, TOidcCredentialsProvider>(TExternalAuthenticators externalAuthenticators)
        {
            var subject = ConvertTo<TExternalAuthenticators, ExternalCredentialsAuthenticators>(externalAuthenticators);
            return ConvertTo<Lazy<ConcurrentDictionary<OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>>, Lazy<ConcurrentDictionary<TKey, TOidcCredentialsProvider>>>(subject._oidcAuthenticationCredentialsProviderCache());
        }

        /// <summary>
        /// NOTE: externalAuthenticators must be an instance of <see cref="ExternalCredentialsAuthenticators"/>.
        /// </summary>
        public static TOidcCredentialsProvider GetOidcProvider<TExternalAuthenticators, TOidcCredentialsProvider>(TExternalAuthenticators externalAuthenticators)
        {
            var subject = ConvertTo<TExternalAuthenticators, ExternalCredentialsAuthenticators>(externalAuthenticators);
            return ConvertTo<IOidcExternalAuthenticationCredentialsProvider, TOidcCredentialsProvider>(subject._oidcAuthenticationCredentialsProviderCache().Value.Single().Value);
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
        public static Lazy<ConcurrentDictionary<OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>> _oidcAuthenticationCredentialsProviderCache(this ExternalCredentialsAuthenticators obj) =>
            (Lazy<ConcurrentDictionary<OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>>)Reflector.GetFieldValue(obj, nameof(_oidcAuthenticationCredentialsProviderCache));
        public static void _oidcAuthenticationCredentialsProviderCache(this ExternalCredentialsAuthenticators obj, Lazy<ConcurrentDictionary<OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>> value) =>
            Reflector.SetFieldValue(obj, nameof(_oidcAuthenticationCredentialsProviderCache), value);
        public static void _awsForOidcExternalAuthenticationCredentialsProvider(this ExternalCredentialsAuthenticators obj, Lazy<IExternalAuthenticationCredentialsProvider<OidcCredentials>> value) =>
            Reflector.SetFieldValue(obj, nameof(_awsForOidcExternalAuthenticationCredentialsProvider), value);
    }
}
