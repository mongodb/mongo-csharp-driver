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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.External
{
    internal sealed class AzureCredentials : IExternalCredentials
    {
        // credentials are considered expired when: Expiration - now < 1 mins
        private static readonly TimeSpan __overlapWhereExpired = TimeSpan.FromMinutes(1);

        private readonly DateTime? _expiration;
        private readonly string _accessToken;

        public AzureCredentials(string accessToken, DateTime? expiration)
        {
            _accessToken = Ensure.IsNotNull(accessToken, nameof(accessToken));
            _expiration = expiration; // can be null
        }

        public string AccessToken => _accessToken;
        public DateTime? Expiration => _expiration;
        public bool ShouldBeRefreshed => _expiration.HasValue ? (_expiration.Value - DateTime.UtcNow) < __overlapWhereExpired : true;
    }

    internal sealed class AzureAuthenticationCredentialsProvider : IExternalAuthenticationCredentialsProvider<AzureCredentials>
    {
        private readonly AzureHttpClientHelper _azureHttpClientHelper;

        public AzureAuthenticationCredentialsProvider(IHttpClientWrapper httpClientWrapper) => _azureHttpClientHelper = new AzureHttpClientHelper(httpClientWrapper);

        public AzureCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken) =>
            CreateCredentialsFromExternalSourceAsync(cancellationToken).GetAwaiter().GetResult();

        public async Task<AzureCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var response = await _azureHttpClientHelper.GetIMDSesponseAsync(cancellationToken).ConfigureAwait(false);
            return CreateAzureCredentialsFromAzureIMDSResponse(response, startTime);
        }

        private AzureCredentials CreateAzureCredentialsFromAzureIMDSResponse(string azureResponse, DateTime startTime)
        {
            if (!BsonDocument.TryParse(azureResponse, out var parsedResponse))
            {
                throw new InvalidOperationException("Azure IMDS response must be in Json format.");
            }
            var accessToken = parsedResponse.GetValue("access_token", null)?.AsString;
            if (accessToken == null)
            {
                throw new InvalidOperationException("Azure IMDS response must contain access_token.");
            }
            var expiresIn = parsedResponse.GetValue("expires_in", null)?.AsString;
            if (!int.TryParse(expiresIn, out var expiresInSeconds))
            {
                var messageDetails = expiresIn?.ToString() ?? "null";
                throw new InvalidOperationException($"Azure IMDS response must contain 'expires_in' integer, but was {messageDetails}.");
            }
            var expirationDateTime = startTime.AddSeconds(expiresInSeconds);

            return new AzureCredentials(accessToken, expirationDateTime);
        }

        // nested types
        private class AzureHttpClientHelper
        {
            #region static
            private static readonly Uri __IMDSRequestUri = new Uri(
                baseUri: new Uri("http://169.254.169.254"),
                relativeUri: "metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://vault.azure.net");
            #endregion

            private readonly IHttpClientWrapper _httpClientWrapper;

            public AzureHttpClientHelper(IHttpClientWrapper httpClientWrapper) => _httpClientWrapper = Ensure.IsNotNull(httpClientWrapper, nameof(httpClientWrapper));

            public async Task<string> GetIMDSesponseAsync(CancellationToken cancellationToken)
            {
                var credentialsRequest = new HttpRequestMessage
                {
                    RequestUri = __IMDSRequestUri,
                    Method = HttpMethod.Get
                };
                credentialsRequest.Headers.Add("Metadata", "true");
                credentialsRequest.Headers.Add("Accept", "application/json");

                return await _httpClientWrapper.GetHttpContentAsync(credentialsRequest, "Failed to acquire IMDS access token.", cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
