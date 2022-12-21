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
using System.Collections.Concurrent;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.Oidc
{
    internal sealed class OidcCacheKey
    {
        #region static
        public static readonly TimeSpan CacheExpiredTime = TimeSpan.FromHours(5);
        public static void RemoveInvalidRecords<TValue>(ConcurrentDictionary<OidcCacheKey, TValue> dictionary)
        {
            var removeRecordsMarker = new OidcCacheKey(removeInvalidRecordsRequested: true);
            while (dictionary.TryRemove(removeRecordsMarker, out var provider))
            {
                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        #endregion

        private readonly IClock _clock;
        private readonly bool _removeInvalidRecordsRequested;
        private DateTime _modifiedAt;
        private readonly OidcInputConfiguration _oidcInputConfiguration;

        public OidcCacheKey(OidcInputConfiguration oidcInputConfiguration, IClock clock) : this(removeInvalidRecordsRequested: false)
        {
            _clock = Ensure.IsNotNull(clock, nameof(clock));
            _oidcInputConfiguration = Ensure.IsNotNull(oidcInputConfiguration, nameof(oidcInputConfiguration));
            _modifiedAt = clock.UtcNow;
        }

        private OidcCacheKey(bool removeInvalidRecordsRequested)
        {
            _removeInvalidRecordsRequested = removeInvalidRecordsRequested;
        }

        public OidcInputConfiguration OidcInputConfiguration => _oidcInputConfiguration;
        public DateTime ModifiedAt => _modifiedAt;
        public bool InvalidRecord => _removeInvalidRecordsRequested || (_clock.UtcNow - _modifiedAt) > CacheExpiredTime;

        public void TrackUsage() => _modifiedAt = _clock.UtcNow;

        public override bool Equals(object obj)
        {
            if (obj is not OidcCacheKey cacheKey)
            {
                return false;
            }

            if (_removeInvalidRecordsRequested || cacheKey._removeInvalidRecordsRequested)
            {
                // consider all invalid records as equal when removing
                return InvalidRecord == cacheKey.InvalidRecord;
            }

            return _oidcInputConfiguration.Equals(cacheKey._oidcInputConfiguration);
        }

        public override int GetHashCode() => 1;
    }
}
