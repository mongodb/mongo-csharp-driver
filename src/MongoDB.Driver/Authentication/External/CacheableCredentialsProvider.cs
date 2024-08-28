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

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Authentication.External
{
    internal interface ICredentialsCache<TCredentials> where TCredentials : IExternalCredentials
    {
        void Clear();
    }

    internal sealed class CacheableCredentialsProvider<TCredentials> : IExternalAuthenticationCredentialsProvider<TCredentials>, ICredentialsCache<TCredentials>
        where TCredentials : IExternalCredentials
    {
        private TCredentials _cachedCredentials;
        private readonly IExternalAuthenticationCredentialsProvider<TCredentials> _provider;

        public CacheableCredentialsProvider(IExternalAuthenticationCredentialsProvider<TCredentials> provider)
        {
            _provider = Ensure.IsNotNull(provider, nameof(provider));
        }

        public TCredentials Credentials => _cachedCredentials;

        public TCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken = default)
        {
            var cachedCredentials = _cachedCredentials;
            if (IsValidCache(cachedCredentials))
            {
                return cachedCredentials;
            }
            else
            {
                Clear();
                try
                {
                    cachedCredentials = _provider.CreateCredentialsFromExternalSource(cancellationToken);
                    if (cachedCredentials.Expiration.HasValue) // allows caching
                    {
                        _cachedCredentials = cachedCredentials;
                    }
                    return cachedCredentials;
                }
                catch
                {
                    Clear();
                    throw;
                }
            }
        }

        public async Task<TCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken = default)
        {
            var cachedCredentials = _cachedCredentials;
            if (IsValidCache(cachedCredentials))
            {
                return cachedCredentials;
            }
            else
            {
                Clear();
                try
                {
                    cachedCredentials = await _provider.CreateCredentialsFromExternalSourceAsync(cancellationToken).ConfigureAwait(false);
                    if (cachedCredentials.Expiration.HasValue) // allows caching
                    {
                        _cachedCredentials = cachedCredentials;
                    }
                    return cachedCredentials;
                }
                catch
                {
                    Clear();
                    throw;
                }
            }
        }

        // private methods
        private bool IsValidCache(TCredentials credentials) => credentials != null && !credentials.ShouldBeRefreshed;
        public void Clear() => _cachedCredentials = default;
    }
}
