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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Authentication;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    public class OidcExternalAuthenticationCredentialsProviderTests
    {
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_response_waiting_should_be_failed_after_timeout(
            [Values(false, true)] bool withRefreshCallback,
            [Values(false, true)] bool async)
        {
            var testTimeout = TimeSpan.FromMinutes(15);
            var clock = FrozenClock.FreezeUtcNow();
            var taskCompletionSource = new TaskCompletionSource<bool>();

            CancellationToken callbackCancellationToken = default;
            using var subject = CreateSubject(
                withRefreshCallback,
                clock,
                (ct) =>
                {
                    ct.IsCancellationRequested.Should().BeFalse();
                    callbackCancellationToken = ct;
                    taskCompletionSource.Task.WaitOrThrow(testTimeout);
                });
            subject._timeout(TimeSpan.FromSeconds(1));
            var exception = async
                ? await Record.ExceptionAsync(() => subject.CreateCredentialsFromExternalSourceAsync(It.IsAny<OidcCredentials>(), new BsonDocument()))
                : Record.Exception(() => subject.CreateCredentialsFromExternalSource(It.IsAny<OidcCredentials>(), new BsonDocument()));

            exception.Should().BeOfType<TimeoutException>();
            callbackCancellationToken.IsCancellationRequested.Should().BeTrue();

            taskCompletionSource.TrySetResult(true); // stop waiting
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Dispose_should_cancel_token_in_callbacks(
            [Values(false, true)] bool withRefreshCallback,
            [Values(false, true)] bool async)
        {
            var testTimeout = TimeSpan.FromMinutes(15);
            var clock = FrozenClock.FreezeUtcNow();
            var taskCompletionSource = new TaskCompletionSource<bool>();

            CancellationToken callbackCancellationToken = default;
            OidcExternalAuthenticationCredentialsProvider subject = null;
            subject = CreateSubject(
                withRefreshCallback,
                clock,
                (ct) =>
                {
                    ct.IsCancellationRequested.Should().BeFalse();
                    callbackCancellationToken = ct;
                    subject.Dispose();
                    taskCompletionSource.Task.WaitOrThrow(testTimeout);
                });
            var exception = async
                ? await Record.ExceptionAsync(() => subject.CreateCredentialsFromExternalSourceAsync(It.IsAny<OidcCredentials>(), new BsonDocument()))
                : Record.Exception(() => subject.CreateCredentialsFromExternalSource(It.IsAny<OidcCredentials>(), new BsonDocument()));
            callbackCancellationToken.IsCancellationRequested.Should().BeTrue();
            exception.Should().BeOfType<OperationCanceledException>();

            taskCompletionSource.TrySetResult(true); // stop waiting
        }

        // private methods
        private OidcExternalAuthenticationCredentialsProvider CreateSubject(bool withRefreshCallback, IClock clock, Action<CancellationToken> callbackAction)
        {
            var endpoint = new DnsEndPoint("localhost", 27017);

            IOidcRequestCallbackProvider requestCallbackProvider = null;
            IOidcRefreshCallbackProvider refreshCallbackProvider = null;
            if (withRefreshCallback)
            {
                requestCallbackProvider = OidcTestHelper.CreateRequestCallback(validateInput: false, validateToken: false, accessToken: "token");
                refreshCallbackProvider = OidcTestHelper.CreateRefreshCallback(callbackCalled: (a, b, ct) => callbackAction(ct), validateInput: false, validateToken: false, accessToken: "token");
            }
            else
            {
                requestCallbackProvider = OidcTestHelper.CreateRequestCallback(callbackCalled: (a, ct) => callbackAction(ct), validateInput: false, validateToken: false, accessToken: "token");
            }
            var oidcInputConfiguration = new OidcInputConfiguration(endpoint, requestCallbackProvider: requestCallbackProvider, refreshCallbackProvider: refreshCallbackProvider);
            var provider = new OidcExternalAuthenticationCredentialsProvider(oidcInputConfiguration, clock);
            if (withRefreshCallback)
            {
                provider._cachedValue(OidcCredentials.Create("token"));
            }
            return provider;
        }
    }

    internal static class OidcExternalAuthenticationCredentialsProviderReflector
    {
        public static void _timeout(this OidcExternalAuthenticationCredentialsProvider provider, TimeSpan value) => Reflector.SetFieldValue(provider, nameof(_timeout), value);
        public static void _cachedValue(this OidcExternalAuthenticationCredentialsProvider provider, OidcCredentials value) => Reflector.SetFieldValue(provider, nameof(_cachedValue), value);
    }
}
