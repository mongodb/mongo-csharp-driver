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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.External
{
    internal sealed class ExternalCredentialsAuthenticators
    {
        #region static
        private static Lazy<ExternalCredentialsAuthenticators> __instance = new Lazy<ExternalCredentialsAuthenticators>(() => new ExternalCredentialsAuthenticators(), isThreadSafe: true);
        // public static
        public static ExternalCredentialsAuthenticators Instance => __instance.Value;
        #endregion

        private readonly IHttpClientWrapper _httpClientWrapper;
        private readonly Lazy<IExternalAuthenticationCredentialsProvider<AzureCredentials>> _azureExternalAuthenticationCredentialsProvider;
        private readonly Lazy<IExternalAuthenticationCredentialsProvider<GcpCredentials>> _gcpExternalAuthenticationCredentialsProvider;

        internal ExternalCredentialsAuthenticators() : this(new HttpClientWrapper())
        {
        }

        internal ExternalCredentialsAuthenticators(IHttpClientWrapper httpClientWrapper)
        {
            _httpClientWrapper = Ensure.IsNotNull(httpClientWrapper, nameof(httpClientWrapper));
            _azureExternalAuthenticationCredentialsProvider = new Lazy<IExternalAuthenticationCredentialsProvider<AzureCredentials>>(() => new CacheableCredentialsProvider<AzureCredentials>(new AzureAuthenticationCredentialsProvider(_httpClientWrapper)), isThreadSafe: true);
            _gcpExternalAuthenticationCredentialsProvider = new Lazy<IExternalAuthenticationCredentialsProvider<GcpCredentials>>(() => new GcpAuthenticationCredentialsProvider(_httpClientWrapper), isThreadSafe: true);
        }

        public IExternalAuthenticationCredentialsProvider<AzureCredentials> Azure => _azureExternalAuthenticationCredentialsProvider.Value;
        public IExternalAuthenticationCredentialsProvider<GcpCredentials> Gcp => _gcpExternalAuthenticationCredentialsProvider.Value;

        internal IHttpClientWrapper HttpClientWrapper => _httpClientWrapper;
    }
}
