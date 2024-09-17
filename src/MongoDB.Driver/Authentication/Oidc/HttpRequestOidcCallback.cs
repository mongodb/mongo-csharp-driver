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

namespace MongoDB.Driver.Authentication.Oidc
{
    internal abstract class HttpRequestOidcCallback : IOidcCallback
    {
        public OidcAccessToken GetOidcAccessToken(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var request = CreateRequest(parameters);
            using (cancellationToken.Register(() => request.Abort(), useSynchronizationContext: false))
            {
                using var response = request.GetResponse();
                return ProcessHttpResponse((HttpWebResponse)response);
            }
        }

        public async Task<OidcAccessToken> GetOidcAccessTokenAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var request = CreateRequest(parameters);
            using (cancellationToken.Register(() => request.Abort(), useSynchronizationContext: false))
            {
                using var response = await request.GetResponseAsync().ConfigureAwait(false);
                return ProcessHttpResponse((HttpWebResponse)response);
            }
        }

        protected abstract (Uri Uri, (string Key, string Value)[] headers) GetHttpRequestParams(OidcCallbackParameters parameters);
        protected abstract OidcAccessToken ProcessHttpResponse(Stream responseStream);

        private HttpWebRequest CreateRequest(OidcCallbackParameters parameters)
        {
            var metadataInfo = GetHttpRequestParams(parameters);

#pragma warning disable SYSLIB0014 // Type or member is obsolete
            var request = WebRequest.CreateHttp(metadataInfo.Uri);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            request.Method = "GET";
            foreach (var header in metadataInfo.headers)
            {
                if (string.Equals(header.Key, "Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Accept = header.Value;
                }
                else
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            return request;
        }

        private OidcAccessToken ProcessHttpResponse(HttpWebResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Response status code does not indicate success {response.StatusCode}:{response.StatusDescription}");
            }

            using var responseStream = response.GetResponseStream();
            return ProcessHttpResponse(responseStream);
        }
    }
}
