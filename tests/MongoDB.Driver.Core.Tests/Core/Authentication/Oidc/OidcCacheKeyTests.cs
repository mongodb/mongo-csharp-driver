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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    public class OidcCacheKeyTests
    {
        private readonly static EndPoint __endpoint = new DnsEndPoint("localhost", 27017);

        [Fact]
        public void RemoveInvalidRecords_should_remove_only_invalid_keys()
        {
            var startingTime = DateTime.UtcNow.Subtract(OidcCacheKey.CacheExpiredTime.Add(TimeSpan.FromMilliseconds(1)));
            var invalidClock = new FrozenClock(startingTime); // will be invalid later
            var validClockKey2 = new FrozenClock(startingTime);
            var validClockKey4 = new FrozenClock(startingTime);
            var invalidKey1 = new OidcCacheKey(new OidcInputConfiguration(__endpoint, providerName: "device1"), invalidClock);
            var validKey2 = new OidcCacheKey(new OidcInputConfiguration(__endpoint, providerName: "device2"), validClockKey2);
            var invalidKey3 = new OidcCacheKey(new OidcInputConfiguration(__endpoint, providerName: "device3"), invalidClock);
            var validKey4 = new OidcCacheKey(new OidcInputConfiguration(__endpoint, providerName: "device4"), validClockKey4);

            var subject = CreateSubject(invalidKey1, validKey2, invalidKey3, validKey4);

            subject.Count.Should().Be(4);
            ValidateRecords(subject, 1, 2, 3, 4);

            OidcCacheKey.RemoveInvalidRecords(subject);

            subject.Count.Should().Be(4);
            ValidateRecords(subject, 1, 2, 3, 4);

            invalidClock.UtcNow += OidcCacheKey.CacheExpiredTime + TimeSpan.FromMilliseconds(1); // ensure "invalid" records are changed to invalid
            ValidateRecords(subject, 1, 2, 3, 4);

            OidcCacheKey.RemoveInvalidRecords(subject);

            subject.Count.Should().Be(2);
            ValidateRecords(subject, 2, 4);

            validClockKey2.UtcNow += OidcCacheKey.CacheExpiredTime + TimeSpan.FromMilliseconds(1); // making record 2 invalid
            validKey2.TrackUsage(); // protect record 2 from removing
            OidcCacheKey.RemoveInvalidRecords(subject);

            subject.Count.Should().Be(2);
            ValidateRecords(subject, 2, 4);

            validClockKey2.UtcNow += OidcCacheKey.CacheExpiredTime + TimeSpan.FromMilliseconds(1);
            validClockKey4.UtcNow += OidcCacheKey.CacheExpiredTime + TimeSpan.FromMilliseconds(1);
            OidcCacheKey.RemoveInvalidRecords(subject);

            subject.Count.Should().Be(0);
            ValidateRecords(subject, expectedIndexes: null);

            OidcCacheKey.RemoveInvalidRecords(subject);

            ValidateRecords(subject, expectedIndexes: null);

            void ValidateRecords(ConcurrentDictionary<OidcCacheKey, int> dictionary, params int[] expectedIndexes)
            {
                if (expectedIndexes == null)
                {
                    dictionary.Count.Should().Be(0);
                    return;
                }

                int index = 0;
                foreach (var item in dictionary.ToList().OrderBy(v => v.Value))
                {
                    item.Value.Should().Be(expectedIndexes[index]);
                    index++;
                }
            }
        }

        [Fact]
        public void RemoveInvalidRecords_should_ingore_empty_dictionary()
        {
            var subject = CreateSubject();

            subject.Count.Should().Be(0);

            OidcCacheKey.RemoveInvalidRecords(subject);

            subject.Count.Should().Be(0);
        }

        // private methods
        private ConcurrentDictionary<OidcCacheKey, int> CreateSubject(params OidcCacheKey[] keys) =>
            new ConcurrentDictionary<OidcCacheKey, int>(keys.Select((k, index) => new KeyValuePair<OidcCacheKey, int>(k, index + 1)));
    }
}
