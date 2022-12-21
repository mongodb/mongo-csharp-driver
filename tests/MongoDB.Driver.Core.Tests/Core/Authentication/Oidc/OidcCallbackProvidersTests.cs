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
using MongoDB.Driver.Core.Authentication.Oidc;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    public class OidcCallbackProvidersTests
    {
        [Fact]
        public void RequestCallbackProvider_constructor_should_throw_when_both_sync_and_async_callbacks_null()
        {
            Record.Exception(() => new RequestCallbackProvider(null, null))
                .Should().BeOfType<ArgumentException>().Which.Message
                .Should().Be($"{MongoOidcAuthenticator.RequestCallbackName} must be provided.");
        }

        [Fact]
        public void RefreshCallbackProvider_constructor_should_throw_when_both_sync_and_async_callbacks_null()
        {
            Record.Exception(() => new RefreshCallbackProvider(null, null))
                .Should().BeOfType<ArgumentException>().Which.Message
                .Should().Be($"{MongoOidcAuthenticator.RefreshCallbackName} must be provided.");
        }

        [Fact]
        public async Task RequestCallbackProvider_Equals_should_ignore_auth_generated_callbacks()
        {
            RequestCallback syncFunc = new((a, b, ct) => b);
            RequestCallbackAsync asyncFunc = new((a, b, ct) => Task.FromResult(b));

            var provider1 = await CreateSubject(syncFunc, asyncFunc: null);
            var provider2 = await CreateSubject(syncFunc, asyncFunc: null);
            provider1.Equals(provider2).Should().BeTrue();

            provider1 = await CreateSubject(syncFunc: null, asyncFunc);
            provider2 = await CreateSubject(syncFunc: null, asyncFunc);
            provider1.Equals(provider2).Should().BeTrue();

            provider1 = await CreateSubject(syncFunc, asyncFunc);
            provider2 = await CreateSubject(syncFunc, asyncFunc);
            provider1.Equals(provider2).Should().BeTrue();

            provider1 = await CreateSubject(syncFunc: null, asyncFunc);
            provider2 = await CreateSubject(syncFunc, asyncFunc: null);
            provider1.Equals(provider2).Should().BeFalse();

            provider1 = await CreateSubject(syncFunc, asyncFunc: null);
            provider2 = await CreateSubject(syncFunc: null, asyncFunc);
            provider1.Equals(provider2).Should().BeFalse();

            static async Task<RequestCallbackProvider> CreateSubject(RequestCallback syncFunc, RequestCallbackAsync asyncFunc)
            {
                var data = new BsonDocument("dummy", 1);
                var provider = new RequestCallbackProvider(syncFunc, asyncFunc);
                var principalName = "dummy";
                provider.GetTokenResult(principalName, data, CancellationToken.None).Should().Be(data);
                (await provider.GetTokenResultAsync(principalName, data, CancellationToken.None)).Should().Be(data);
                return provider;
            }
        }

        [Fact]
        public async Task RefreshCallbackProvider_Equals_should_ignore_auth_generated_callbacks()
        {
            RefreshCallback syncFunc = new((a, b, c, ct) => b);
            RefreshCallbackAsync asyncFunc = new((a, b, c, ct) => Task.FromResult(b));

            var provider1 = await CreateSubject(syncFunc, asyncFunc: null);
            var provider2 = await CreateSubject(syncFunc, asyncFunc: null);
            provider1.Equals(provider2).Should().BeTrue();

            provider1 = await CreateSubject(syncFunc: null, asyncFunc);
            provider2 = await CreateSubject(syncFunc: null, asyncFunc);
            provider1.Equals(provider2).Should().BeTrue();

            provider1 = await CreateSubject(syncFunc, asyncFunc);
            provider2 = await CreateSubject(syncFunc, asyncFunc);
            provider1.Equals(provider2).Should().BeTrue();

            provider1 = await CreateSubject(syncFunc: null, asyncFunc);
            provider2 = await CreateSubject(syncFunc, asyncFunc: null);
            provider1.Equals(provider2).Should().BeFalse();

            provider1 = await CreateSubject(syncFunc, asyncFunc: null);
            provider2 = await CreateSubject(syncFunc: null, asyncFunc);
            provider1.Equals(provider2).Should().BeFalse();

            static async Task<RefreshCallbackProvider> CreateSubject(RefreshCallback syncFunc, RefreshCallbackAsync asyncFunc)
            {
                var data = new BsonDocument("dummy", 1);
                var noop = new BsonDocument("noop", 1);
                var provider = new RefreshCallbackProvider(syncFunc, asyncFunc);
                var principalName = "dummy";
                provider.GetTokenResult(principalName, data, noop, CancellationToken.None).Should().Be(data);
                (await provider.GetTokenResultAsync(principalName, data, noop, CancellationToken.None)).Should().Be(data);
                return provider;
            }
        }
    }
}
