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
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal class OidcAuthenticationCredentialsProviderAdapter<TCredentials>
        : IExternalAuthenticationCredentialsProvider<OidcCredentials>, ICredentialsCache<OidcCredentials>
        where TCredentials : IExternalCredentials
    {
        private readonly IExternalAuthenticationCredentialsProvider<TCredentials> _externalAuthenticationCredentialsProvider;

        public OidcAuthenticationCredentialsProviderAdapter(IExternalAuthenticationCredentialsProvider<TCredentials> externalAuthenticationCredentialsProvider)
        {
            _externalAuthenticationCredentialsProvider = Ensure.IsNotNull(externalAuthenticationCredentialsProvider, nameof(externalAuthenticationCredentialsProvider));
        }

        public OidcCredentials CachedCredentials
        {
            get
            {
                var cachedProvider = _externalAuthenticationCredentialsProvider as ICredentialsCache<TCredentials>;
                if (cachedProvider != null)
                {
                    var cachedCredentials = cachedProvider.CachedCredentials;
                    if (cachedCredentials != null)
                    {
                        return CreateOidcCredentials(cachedCredentials);
                    }
                }
                return null;
            }
        }

        public void Clear() => (_externalAuthenticationCredentialsProvider as ICredentialsCache<TCredentials>)?.Clear();

        public OidcCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken = default)
        {
            var credentials = _externalAuthenticationCredentialsProvider.CreateCredentialsFromExternalSource(cancellationToken);
            return CreateOidcCredentials(credentials);
        }

        public async Task<OidcCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken = default)
        {
            var credentials = await _externalAuthenticationCredentialsProvider.CreateCredentialsFromExternalSourceAsync(cancellationToken).ConfigureAwait(false);
            return CreateOidcCredentials(credentials);
        }

        // private methods
        private OidcCredentials CreateOidcCredentials(IExternalCredentials externalCredentials) => OidcCredentials.Create(externalCredentials.AccessToken);
    }
}
