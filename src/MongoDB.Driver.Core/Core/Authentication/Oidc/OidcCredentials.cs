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
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class OidcCredentials : IExternalCredentials
    {
        #region static
        private const string AccessTokenFieldName = "accessToken";
        public static readonly TimeSpan OverlapWhereExpiredTime = TimeSpan.FromMinutes(5);

        public static OidcCredentials Create(BsonDocument callbackAuthenticationData, BsonDocument saslServerResponse, IClock clock) =>
            new OidcCredentials(Ensure.IsNotNull(callbackAuthenticationData, nameof(callbackAuthenticationData)), Ensure.IsNotNull(saslServerResponse, nameof(saslServerResponse)), Ensure.IsNotNull(clock, nameof(clock)));

        public static OidcCredentials Create(string accessToken) =>
            new OidcCredentials(new BsonDocument(AccessTokenFieldName, Ensure.IsNotNull(accessToken, nameof(accessToken))), serverResponse: null, clock: null);
        #endregion

        private readonly string _accessToken;
        private readonly BsonDocument _callbackAuthenticationData;
        private readonly BsonDocument _serverResponse;
        private readonly IClock _clock;
        private DateTime? _expiration;

        private OidcCredentials(BsonDocument callbackAuthenticationData, BsonDocument serverResponse, IClock clock)
        {
            _callbackAuthenticationData = EnsureAuthenticationDataValid(Ensure.IsNotNull(callbackAuthenticationData, nameof(callbackAuthenticationData)));
            _accessToken = Ensure.IsNotNullOrEmpty(_callbackAuthenticationData.GetValue(AccessTokenFieldName, null)?.ToString(), paramName: AccessTokenFieldName);
            _clock = clock; // can be null
            _expiration = _callbackAuthenticationData.TryGetValue("expiresInSeconds", out var expiresInSeconds) ? _clock.UtcNow.AddSeconds(expiresInSeconds.ToInt32()) : null;
            _serverResponse = serverResponse; // can be null

            static BsonDocument EnsureAuthenticationDataValid(BsonDocument callbackAuthenticationData) =>
                callbackAuthenticationData.Contains(AccessTokenFieldName)
                    ? callbackAuthenticationData
                    : throw new InvalidOperationException("The provided OIDC credentials must contain 'accessToken'.");
        }

        public string AccessToken => _accessToken;
        public BsonDocument CallbackAuthenticationData => _callbackAuthenticationData;
        public DateTime? Expiration => _expiration;
        public BsonDocument ServerResponse => _serverResponse;
        public bool ShouldBeRefreshed => _expiration.HasValue ? (_expiration.Value - _clock.UtcNow) < OverlapWhereExpiredTime : true;

        public void Expire() => _expiration = null;
        public BsonDocument GetKmsCredentials() =>
            // should not be reached
            throw new NotSupportedException("OIDC authentication is not supported with KMS.");
    }
}
