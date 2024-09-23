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
    internal sealed class GcpCredentials : IExternalCredentials
    {
        private readonly string _accessToken;

        public GcpCredentials(string accessToken) => _accessToken = accessToken;

        public string AccessToken => _accessToken;

        public DateTime? Expiration => null;

        public bool ShouldBeRefreshed => true;
    }

    internal sealed class GcpAuthenticationCredentialsProvider : IExternalAuthenticationCredentialsProvider<GcpCredentials>
    {
        private readonly GcpHttpClientHelper _gcpHttpClientHelper;

        public GcpAuthenticationCredentialsProvider(IHttpClientWrapper httpClientWrapper) => _gcpHttpClientHelper = new GcpHttpClientHelper(httpClientWrapper);

        public GcpCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken) =>
            CreateCredentialsFromExternalSourceAsync(cancellationToken).GetAwaiter().GetResult();

        public async Task<GcpCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken)
        {
            var accessToken = await _gcpHttpClientHelper.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            return new GcpCredentials(accessToken);
        }

        // nested types
        private class GcpHttpClientHelper
        {
            // private static
            private static readonly string __defaultGceMetadataHost = "metadata.google.internal";
            private readonly IHttpClientWrapper _httpClientWrapper;

            public GcpHttpClientHelper(IHttpClientWrapper httpClientWrapper) => _httpClientWrapper = Ensure.IsNotNull(httpClientWrapper, nameof(httpClientWrapper));

            public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
            {
                var host = Environment.GetEnvironmentVariable("GCE_METADATA_HOST") ?? __defaultGceMetadataHost;

                var tokenRequest = new HttpRequestMessage
                {
                    RequestUri = new Uri($"http://{host}/computeMetadata/v1/instance/service-accounts/default/token"),
                    Method = HttpMethod.Get
                };
                tokenRequest.Headers.Add("Metadata-Flavor", "Google");

                var response = await _httpClientWrapper.GetHttpContentAsync(tokenRequest, "Failed to acquire gce metadata credentials.", cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrEmpty(response))
                {
                    throw new MongoClientException($"The metadata host response is empty.");
                }
                var parsedResponse = BsonDocument.Parse(response);
                return parsedResponse.GetValue("access_token", defaultValue: null)?.AsString ?? throw new MongoClientException($"The metadata host response {response} doesn't contain access_token.");
            }
        }
    }
}
