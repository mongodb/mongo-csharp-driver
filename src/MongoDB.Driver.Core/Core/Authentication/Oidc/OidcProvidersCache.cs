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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal interface IOidcProvidersCache
    {
        IOidcExternalAuthenticationCredentialsProvider GetProvider(OidcInputConfiguration inputConfiguration);
    }

    internal sealed class OidcCacheValue
    {
        #region static
        public static readonly TimeSpan CacheExpirationWindow = TimeSpan.FromHours(5);
        #endregion

        private readonly IClock _clock;
        private DateTime _lastUsage;
        private readonly IOidcExternalAuthenticationCredentialsProvider _provider;

        public OidcCacheValue(IOidcExternalAuthenticationCredentialsProvider provider, IClock clock)
        {
            _clock = Ensure.IsNotNull(clock, nameof(clock));
            _lastUsage = clock.UtcNow;
            _provider = Ensure.IsNotNull(provider, nameof(provider));
        }

        public bool IsInvalid => (_clock.UtcNow - _lastUsage) > CacheExpirationWindow;
        public DateTime LastUsage => _lastUsage;
        public IOidcExternalAuthenticationCredentialsProvider Provider => _provider;

        public void Touch() => _lastUsage = _clock.UtcNow;
    }

    internal class OidcProvidersCache : IOidcProvidersCache
    {
        private readonly IClock _clock;
        private readonly Dictionary<OidcInputConfiguration, OidcCacheValue> _providersCache;
        private readonly object _lock = new();

        public OidcProvidersCache(IClock clock)
        {
            _clock = clock;
            _providersCache = new();
        }

        public IOidcExternalAuthenticationCredentialsProvider GetProvider(OidcInputConfiguration inputConfiguration)
        {
            var toRemove = new List<IOidcExternalAuthenticationCredentialsProvider>();
            lock (_lock)
            {
                for (int i = 0; i < _providersCache.Count; i++)
                {
                    var element = _providersCache.ElementAt(i);
                    if (element.Value.IsInvalid)
                    {
                        toRemove.Add(element.Value.Provider);
                        _providersCache.Remove(element.Key);
                    }
                }
            }

            foreach (var element in toRemove.OfType<IDisposable>())
            {
                element.Dispose();
            }

            lock (_lock)
            {
                var cacheValue = _providersCache.GetOrAdd(
                    inputConfiguration,
                    () => new OidcCacheValue(new OidcExternalAuthenticationCredentialsProvider(inputConfiguration, _clock), _clock),
                    (value) =>
                    {
                        value.Touch();
                        return value;
                    });
                return cacheValue.Provider;
            }
        }
    }
}
