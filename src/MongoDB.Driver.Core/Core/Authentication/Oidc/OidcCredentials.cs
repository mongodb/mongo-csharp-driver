﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class OidcCredentials : IExternalCredentials
    {
        #region static
        private const string AccessTokenFieldName = "accessToken";
        private const string RefreshTokenFieldName = "refreshToken";
        public static readonly TimeSpan ExpirationWindow = TimeSpan.FromMinutes(5);

        public static OidcCredentials Create(BsonDocument callbackAuthenticationData, BsonDocument idpServerInfo, IClock clock) =>
            new OidcCredentials(
                Ensure.IsNotNull(callbackAuthenticationData, nameof(callbackAuthenticationData)),
                Ensure.IsNotNull(idpServerInfo, nameof(idpServerInfo)),
                Ensure.IsNotNull(clock, nameof(clock)));

        public static OidcCredentials Create(string accessToken) =>
            new OidcCredentials(new BsonDocument(AccessTokenFieldName, Ensure.IsNotNull(accessToken, nameof(accessToken))), idpServerInfo: null, clock: null);
        #endregion

        private readonly string _accessToken;
        private readonly BsonDocument _callbackAuthenticationData;
        private readonly IClock _clock;
        private DateTime? _expiration;
        private readonly BsonDocument _idpServerInfo;
        private readonly string _refreshToken;

        private OidcCredentials(
            BsonDocument callbackAuthenticationData,
            BsonDocument idpServerInfo,
            IClock clock)
        {
            _callbackAuthenticationData = EnsureAuthenticationDataValid(callbackAuthenticationData);
            _accessToken = _callbackAuthenticationData?.GetValue(AccessTokenFieldName, null)?.ToString();
            _refreshToken = _callbackAuthenticationData?.GetValue(RefreshTokenFieldName, null)?.ToString();
            _clock = clock; // can be null
            _expiration = _callbackAuthenticationData != null && _callbackAuthenticationData.TryGetValue("expiresInSeconds", out var expiresInSeconds) ? _clock.UtcNow.AddSeconds(expiresInSeconds.ToInt32()) : null;
            _idpServerInfo = idpServerInfo; // can be null

            static BsonDocument EnsureAuthenticationDataValid(BsonDocument callbackAuthenticationData)
            {
                if (callbackAuthenticationData == null) return null;

                bool withAccessToken = false;
                foreach (var item in callbackAuthenticationData)
                {
                    switch (item.Name)
                    {
                        case AccessTokenFieldName: withAccessToken = true; break;
                        case "expiresInSeconds":
                        case RefreshTokenFieldName: /*optional fields, do nothing*/ break;
                        default: throw new InvalidOperationException($"The provided OIDC credentials contain unsupported key: '{item.Name}'.");
                    }
                }
                return withAccessToken ? callbackAuthenticationData : throw new InvalidOperationException($"The provided OIDC credentials must contain '{AccessTokenFieldName}'.");
            }
        }

        public string AccessToken => _accessToken;
        public BsonDocument CallbackAuthenticationData => _callbackAuthenticationData;
        public DateTime? Expiration => _expiration;
        public BsonDocument IdpServerInfo => _idpServerInfo;
        public string RefreshToken => _refreshToken;
        public bool ShouldBeRefreshed =>
            _callbackAuthenticationData == null || // no credentials yet
            _expiration.HasValue ? (_expiration.Value - _clock.UtcNow) < ExpirationWindow : true; // expired by time
        public void Expire() => _expiration = null;
        public BsonDocument GetKmsCredentials() =>
            // should not be reached
            throw new NotSupportedException("OIDC authentication is not supported with KMS.");
    }
}
