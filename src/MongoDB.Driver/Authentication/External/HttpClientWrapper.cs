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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.External
{
    internal interface IHttpClientWrapper
    {
        Task<string> GetHttpContentAsync(HttpRequestMessage request, string exceptionMessage, CancellationToken cancellationToken);
    }

    internal sealed class HttpClientWrapper : IHttpClientWrapper
    {
        #region static
        public static HttpClient CreateHttpClient() => new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };
        #endregion

        private readonly HttpClient _httpClient;

        public HttpClientWrapper() : this(CreateHttpClient())
        { }

        internal HttpClientWrapper(HttpClient httpClient)
        {
            _httpClient = Ensure.IsNotNull(httpClient, nameof(httpClient));
        }

        public async Task<string> GetHttpContentAsync(HttpRequestMessage request, string exceptionMessage, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            string content = null;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return content;
            }
            catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
            {
                if (content != null)
                {
                    exceptionMessage = $"{exceptionMessage} Response body: {content}.";
                }
                throw new MongoClientException(exceptionMessage, ex);
            }
        }
    }
}
