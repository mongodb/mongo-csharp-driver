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

namespace MongoDB.Driver.Core.Authentication.External
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
            _expiration = Ensure.HasValue(expiration, nameof(expiration));
        }

        public string AccessToken => _accessToken;
        public DateTime? Expiration => _expiration;
        public bool IsExpired => _expiration.HasValue ? (_expiration.Value - DateTime.UtcNow) < __overlapWhereExpired : false;

        public BsonDocument GetKmsCredentials()
            => new BsonDocument
            {
                { "accessToken", _accessToken }
            };
    }

    internal class AzureHttpRequestMessageFactory : IExternalCredentialsHttpRequestMessageFactory
    {
        private static readonly Uri __IMDSRequestUri = new Uri(
            baseUri: new Uri("http://169.254.169.254"),
            relativeUri: "metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://vault.azure.net");

        public HttpRequestMessage CreateRequest()
        {
            var credentialsRequest = new HttpRequestMessage
            {
                RequestUri = __IMDSRequestUri,
                Method = HttpMethod.Get
            };
            credentialsRequest.Headers.Add("Metadata", "true");
            credentialsRequest.Headers.Add("Accept", "application/json");

            return credentialsRequest;
        }
    }

    internal class AzureAuthenticationCredentialsProvider : IExternalAuthenticationCredentialsProvider<AzureCredentials>
    {
        private readonly IExternalCredentialsHttpRequestMessageFactory _azureCredentialsHttpRequestMessageFactory;
        private readonly HttpClientHelper _httpClientHelper;

        public AzureAuthenticationCredentialsProvider(HttpClientHelper httpClientHelper)
            : this(httpClientHelper, new AzureHttpRequestMessageFactory())
        {
        }

        public AzureAuthenticationCredentialsProvider(HttpClientHelper httpClientHelper, IExternalCredentialsHttpRequestMessageFactory azureCredentialsHttpRequestMessageFactory)
        {
            _azureCredentialsHttpRequestMessageFactory = Ensure.IsNotNull(azureCredentialsHttpRequestMessageFactory, nameof(azureCredentialsHttpRequestMessageFactory));
            _httpClientHelper = Ensure.IsNotNull(httpClientHelper, nameof(httpClientHelper));
        }

        public AzureCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken) =>
            CreateCredentialsFromExternalSourceAsync(cancellationToken).GetAwaiter().GetResult();

        public async Task<AzureCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken)
        {
            var request = _azureCredentialsHttpRequestMessageFactory.CreateRequest();
            var startTime = DateTime.UtcNow;
            var response = await _httpClientHelper.GetHttpContentAsync(request, "Failed to acquire IMDS access token.", cancellationToken).ConfigureAwait(false);
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
            if (expiresIn == null || !int.TryParse(expiresIn, out var expiresInSeconds))
            {
                var messageDetails = expiresIn?.ToString() ?? "null";
                throw new InvalidOperationException($"Azure IMDS response must contain 'expires_in' integer, but was {messageDetails}.");
            }
            var expirationDateTime = startTime.AddSeconds(expiresInSeconds);

            return new AzureCredentials(accessToken, expirationDateTime);
        }
    }
}
