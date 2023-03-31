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
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.External
{
    internal interface IExternalCredentialsAuthenticators
    {
        IExternalAuthenticationCredentialsProvider<AwsCredentials> Aws { get; }
        IExternalAuthenticationCredentialsProvider<OidcCredentials> AwsForOidc { get; }
        IExternalAuthenticationCredentialsProvider<AzureCredentials> Azure { get; }
        IExternalAuthenticationCredentialsProvider<GcpCredentials> Gcp { get; }
        IOidcProvidersCache Oidc { get; }
    }

    internal sealed class ExternalCredentialsAuthenticators : IExternalCredentialsAuthenticators
    {
        #region static
        private static Lazy<ExternalCredentialsAuthenticators> __instance = new Lazy<ExternalCredentialsAuthenticators>(() => new ExternalCredentialsAuthenticators(), isThreadSafe: true);
        // public static
        public static ExternalCredentialsAuthenticators Instance => __instance.Value;
        #endregion

        private readonly IHttpClientWrapper _httpClientWrapper;

        private readonly Lazy<IExternalAuthenticationCredentialsProvider<AwsCredentials>> _awsExternalAuthenticationCredentialsProvider;
        private readonly Lazy<IExternalAuthenticationCredentialsProvider<OidcCredentials>> _awsForOidcExternalAuthenticationCredentialsProvider;
        private readonly Lazy<IExternalAuthenticationCredentialsProvider<AzureCredentials>> _azureExternalAuthenticationCredentialsProvider;
        private readonly Lazy<IExternalAuthenticationCredentialsProvider<GcpCredentials>> _gcpExternalAuthenticationCredentialsProvider;
        private readonly Lazy<IOidcProvidersCache> _oidcProvidersCache;

        internal ExternalCredentialsAuthenticators() : this(new HttpClientWrapper(), SystemClock.Instance, EnvironmentVariableProvider.Instance)
        {
        }

        internal ExternalCredentialsAuthenticators(IHttpClientWrapper httpClientWrapper, IClock clock, IEnvironmentVariableProvider environmentVariableProvider)
        {
            Ensure.IsNotNull(clock, nameof(clock));
            _httpClientWrapper = Ensure.IsNotNull(httpClientWrapper, nameof(httpClientWrapper));
            _awsExternalAuthenticationCredentialsProvider = new Lazy<IExternalAuthenticationCredentialsProvider<AwsCredentials>>(() => new AwsAuthenticationCredentialsProvider(), isThreadSafe: true);
            _awsForOidcExternalAuthenticationCredentialsProvider = new Lazy<IExternalAuthenticationCredentialsProvider<OidcCredentials>>(() => FileOidcExternalAuthenticationCredentialsProvider.CreateProviderFromPathInEnvironmentVariableIfConfigured("AWS_WEB_IDENTITY_TOKEN_FILE", environmentVariableProvider), isThreadSafe: true);
            _azureExternalAuthenticationCredentialsProvider = new Lazy<IExternalAuthenticationCredentialsProvider<AzureCredentials>>(() => new CacheableCredentialsProvider<AzureCredentials>(new AzureAuthenticationCredentialsProvider(_httpClientWrapper)), isThreadSafe: true);
            _gcpExternalAuthenticationCredentialsProvider = new Lazy<IExternalAuthenticationCredentialsProvider<GcpCredentials>>(() => new GcpAuthenticationCredentialsProvider(_httpClientWrapper), isThreadSafe: true);
            _oidcProvidersCache = new Lazy<IOidcProvidersCache>(() => new OidcProvidersCache(clock), isThreadSafe: true);
        }

        // public properties
        public IExternalAuthenticationCredentialsProvider<AwsCredentials> Aws => _awsExternalAuthenticationCredentialsProvider.Value;
        public IExternalAuthenticationCredentialsProvider<OidcCredentials> AwsForOidc => _awsForOidcExternalAuthenticationCredentialsProvider.Value;
        public IExternalAuthenticationCredentialsProvider<AzureCredentials> Azure => _azureExternalAuthenticationCredentialsProvider.Value;
        public IExternalAuthenticationCredentialsProvider<GcpCredentials> Gcp => _gcpExternalAuthenticationCredentialsProvider.Value;
        public IOidcProvidersCache Oidc => _oidcProvidersCache.Value;

        // internal properties
        internal IHttpClientWrapper HttpClientWrapper => _httpClientWrapper;
    }
}
