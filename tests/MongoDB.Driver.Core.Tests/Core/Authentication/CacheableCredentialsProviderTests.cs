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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Misc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication
{
    public class CacheableCredentialsProviderTests
    {
        [Theory]
        [ParameterAttributeData]
        public async Task CreateCredentialsFromExternalSource_should_return_the_same_result_until_cache_valid([Values(false, true)] bool async)
        {
            bool isExpired = false;
            Func<bool> isExpiredFunc = () => isExpired;
            var subject = CreateSubject(isExpiredFunc);

            var creds1 = await CreateCredentials(subject, async);
            var creds2 = await CreateCredentials(subject, async);
            creds1.Should().BeSameAs(creds2);

            isExpired = true;
            var creds3 = await CreateCredentials(subject, async);
            creds2.Should().NotBeSameAs(creds3);
            var creds4 = await CreateCredentials(subject, async);
            creds3.Should().NotBeSameAs(creds4);

            isExpired = false;
            var creds5 = await CreateCredentials(subject, async);
            creds4.Should().BeSameAs(creds5);
            var creds6 = await CreateCredentials(subject, async);
            creds5.Should().BeSameAs(creds6);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Cache_should_not_be_in_play_when_expiration_date_is_null([Values(false, true)] bool async)
        {
            DateTime? expiredDate = null;
            Func<DateTime?> expiredDateFunc = () => expiredDate;
            var subject = CreateSubject(expirationDateFunc: expiredDateFunc);
            await CreateCredentials(subject, async);
            subject.Credentials.Should().BeNull();
        }

        private async Task<DummyCredentials> CreateCredentials(CacheableCredentialsProvider<DummyCredentials> subject, bool async) =>
            async ? (await subject.CreateCredentialsFromExternalSourceAsync()) : subject.CreateCredentialsFromExternalSource();

        private CacheableCredentialsProvider<DummyCredentials> CreateSubject(Func<bool> isExpiredFunc = null, Func<DateTime?> expirationDateFunc = null)
        {
            isExpiredFunc = isExpiredFunc ?? (() => false);
            expirationDateFunc = expirationDateFunc ?? (() => DateTime.UtcNow.AddDays(1)); // dummy value if not provided

            var externalCredentialsProviderMock = new Mock<IExternalAuthenticationCredentialsProvider<DummyCredentials>>();
            var setupSequence = externalCredentialsProviderMock
                .Setup(c => c.CreateCredentialsFromExternalSource(It.IsAny<CancellationToken>()))
                .Returns(() => new DummyCredentials(isExpiredFunc, expirationDateFunc));
            var setupSequenceAsync = externalCredentialsProviderMock
                .Setup(c => c.CreateCredentialsFromExternalSourceAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new DummyCredentials(isExpiredFunc, expirationDateFunc));

            return new CacheableCredentialsProvider<DummyCredentials>(externalCredentialsProviderMock.Object);
        }
    }

    public class DummyCredentials : IExternalCredentials
    {
        private readonly Func<DateTime?> _expirationDateFunc;
        private readonly Guid _id;
        private readonly Func<bool> _isExpiredFunc;

        public DummyCredentials(Func<bool> isExpiredFunc, Func<DateTime?> expirationDateFunc)
        {
            _expirationDateFunc = expirationDateFunc; // can be null
            _id = Guid.NewGuid();
            _isExpiredFunc = Ensure.IsNotNull(isExpiredFunc, nameof(isExpiredFunc));
        }

        public DateTime? Expiration => _expirationDateFunc();

        public bool ShouldBeRefreshed => _isExpiredFunc();

        public Guid Id => _id;

        public BsonDocument GetKmsCredentials() => new BsonDocument("dummy", 1);
    }
}
