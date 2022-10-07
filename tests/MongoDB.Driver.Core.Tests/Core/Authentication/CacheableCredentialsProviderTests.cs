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
using MongoDB.Bson.TestHelpers.XunitExtensions;
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

            var creds1 = await CreateCredentials();
            var creds2 = await CreateCredentials();
            creds1.Should().BeSameAs(creds2);

            isExpired = true;
            var creds3 = await CreateCredentials();
            creds2.Should().NotBeSameAs(creds3);
            var creds4 = await CreateCredentials();
            creds3.Should().NotBeSameAs(creds4);

            isExpired = false;
            var creds5 = await CreateCredentials();
            creds4.Should().BeSameAs(creds5);
            var creds6 = await CreateCredentials();
            creds5.Should().BeSameAs(creds6);

            async Task<DummyCredentials> CreateCredentials() => async ? (await subject.CreateCredentialsFromExternalSourceAsync()) : subject.CreateCredentialsFromExternalSource();
        }

        private CacheableCredentialsProvider<DummyCredentials> CreateSubject(Func<bool> isExpiredFunc)
        {
            var externalCredentialsProviderMock = new Mock<IExternalAuthenticationCredentialsProvider<DummyCredentials>>();
            var setupSequence = externalCredentialsProviderMock
                .Setup(c => c.CreateCredentialsFromExternalSource(It.IsAny<CancellationToken>()))
                .Returns(() => new DummyCredentials(isExpiredFunc));
            var setupSequenceAsync = externalCredentialsProviderMock
                .Setup(c => c.CreateCredentialsFromExternalSourceAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new DummyCredentials(isExpiredFunc));

            return new CacheableCredentialsProvider<DummyCredentials>(externalCredentialsProviderMock.Object);
        }
    }

    public class DummyCredentials : IExternalCredentials
    {
        private readonly Guid _id;
        private readonly Func<bool> _isExpiredFunc;

        public DummyCredentials(Func<bool> isExpiredFunc)
        {
            _id = Guid.NewGuid();
            _isExpiredFunc = Ensure.IsNotNull(isExpiredFunc, nameof(isExpiredFunc));
        }

        public DateTime? Expiration { get; }

        public bool IsExpired => _isExpiredFunc();

        public Guid Id => _id;

        public BsonDocument GetKmsCredentials() => new BsonDocument("dummy", 1);
    }
}
