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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Authentication.Oidc
{
    internal sealed class AzureOidcCallback : HttpRequestOidcCallback
    {
        private readonly string _tokenResource;

        public AzureOidcCallback(string tokenResource)
        {
            _tokenResource = tokenResource;
        }

        protected override (Uri Uri, (string Key, string Value)[] headers) GetHttpRequestParams(OidcCallbackParameters parameters)
        {
            var metadataUrl = $"http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource={Uri.EscapeDataString(_tokenResource)}";
            if (!string.IsNullOrEmpty(parameters.UserName))
            {
                metadataUrl += $"&client_id={Uri.EscapeDataString(parameters.UserName)}";
            }

            return (new Uri(metadataUrl), new [] { ("Accept", "application/json"), ("Metadata", "true") });
        }

        protected override OidcAccessToken ProcessHttpResponse(Stream responseStream)
        {
            using var responseReader = new StreamReader(responseStream);
            using var jsonReader = new JsonReader(responseReader);

            var context = BsonDeserializationContext.CreateRoot(jsonReader);
            var document = BsonDocumentSerializer.Instance.Deserialize(context);

            var accessToken = document.GetValue("access_token");
            return new OidcAccessToken(accessToken.AsString, null);
        }
    }
}
