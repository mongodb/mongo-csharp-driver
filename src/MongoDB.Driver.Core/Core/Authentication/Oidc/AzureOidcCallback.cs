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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class AzureOidcCallback : IOidcCallback
    {
        private readonly string _tokenResource;

        public AzureOidcCallback(string tokenResource)
        {
            _tokenResource = tokenResource;
        }

        public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var request = CreateMetadataRequest(parameters);
            using (cancellationToken.Register(() => request.Abort(), useSynchronizationContext: false))
            {
                var response = request.GetResponse();
                return ParseMetadataResponse((HttpWebResponse)response);
            }
        }

        public async Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var request = CreateMetadataRequest(parameters);
            using (cancellationToken.Register(() => request.Abort(), useSynchronizationContext: false))
            {
                var response = await request.GetResponseAsync().ConfigureAwait(false);
                return ParseMetadataResponse((HttpWebResponse)response);
            }
        }

        private HttpWebRequest CreateMetadataRequest(OidcCallbackParameters parameters)
        {
            var metadataUrl = $"http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={_tokenResource}";
            if (!string.IsNullOrEmpty(parameters.UserName))
            {
                metadataUrl += $"&client_id={parameters.UserName}";
            }

            var request = WebRequest.CreateHttp(new Uri(metadataUrl));
            request.Headers["Metadata"] = "true";
            request.Accept = "application/json";
            request.Method = "GET";

            return request;
        }

        private OidcAccessToken ParseMetadataResponse(HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Response status code does not indicate success {response.StatusCode}:{response.StatusDescription}");
            }

            using var responseReader = new StreamReader(response.GetResponseStream());
            using var jsonReader = new JsonReader(responseReader);

            var context = BsonDeserializationContext.CreateRoot(jsonReader);
            var document = BsonDocumentSerializer.Instance.Deserialize(context);

            var accessToken = document.GetValue("access_token");
            return new OidcAccessToken(accessToken.AsString, null);
        }
    }
}
