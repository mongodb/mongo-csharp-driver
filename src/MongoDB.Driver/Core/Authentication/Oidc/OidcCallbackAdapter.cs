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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal interface IOidcCallbackAdapter
    {
        OidcCredentials CachedCredentials { get; }

        void InvalidateCachedCredentials(OidcCredentials credentials);

        OidcCredentials GetCredentials(OidcCallbackParameters parameters, CancellationToken cancellationToken);

        ValueTask<OidcCredentials> GetCredentialsAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken);
    }

    internal sealed class OidcCallbackAdapter : IOidcCallbackAdapter, IDisposable
    {
        private readonly IClock _clock;
        private readonly IOidcCallback _wrappedCallback;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private OidcCredentials _cachedCredentials;

        public OidcCallbackAdapter(IOidcCallback wrappedCallback, IClock clock)
        {
            _wrappedCallback = wrappedCallback;
            _clock = clock;
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public OidcCredentials CachedCredentials => _cachedCredentials;

        public void InvalidateCachedCredentials(OidcCredentials credentials)
        {
            if (credentials == null)
            {
                return;
            }

            _lock.Wait();
            try
            {
                if (_cachedCredentials?.AccessToken == credentials.AccessToken)
                {
                    _cachedCredentials = null;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public OidcCredentials GetCredentials(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var credentials = _cachedCredentials;
            if (credentials?.IsExpired == false)
            {
                return credentials;
            }

            _lock.Wait(cancellationToken);

            try
            {
                if (_cachedCredentials?.IsExpired == false)
                {
                    return _cachedCredentials;
                }

                var response = _wrappedCallback.GetOidcAccessToken(parameters, cancellationToken);
                if (response == null)
                {
                    throw new InvalidOperationException("Unexpected response from OIDC Callback.");
                }

                _cachedCredentials = new OidcCredentials(response.AccessToken, response.ExpiresIn, _clock);
                return _cachedCredentials;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask<OidcCredentials> GetCredentialsAsync(OidcCallbackParameters parameters, CancellationToken cancellationToken)
        {
            var credentials = _cachedCredentials;
            if (credentials?.IsExpired == false)
            {
                return credentials;
            }

            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_cachedCredentials?.IsExpired == false)
                {
                    return _cachedCredentials;
                }

                var response = await _wrappedCallback.GetOidcAccessTokenAsync(parameters, cancellationToken).ConfigureAwait(false);
                if (response == null)
                {
                    throw new InvalidOperationException("Unexpected response from OIDC Callback.");
                }

                _cachedCredentials = new OidcCredentials(response.AccessToken, response.ExpiresIn, _clock);
                return _cachedCredentials;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
