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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Authentication;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication.Oidc
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "OidcMechanism")]
    public class OidcAuthenticatorMockedTests : IDisposable
    {
        #region static
        const string PrincipalName = "PrincipalNameTest";
        const string PrincipalName2 = "PrincipalNameTest2";
        const string RequestAccessToken = "requestAccessTokenValue";
        const string RefreshAccessToken = "refreshAccessTokenValue";

        // private static fields
        private static readonly OidcCredentials __awsForOidcCredentials;
        private static readonly AzureCredentials __azureCredentials;
        private static readonly CollectionNamespace __collectionNamespace;
        private static readonly ConnectionDescription __descriptionCommandWireProtocol;
        private static readonly EndPoint __endpoint2;
        private static readonly GcpCredentials __gcpCredentials;
        private static readonly BsonDocument __initialSaslStartResponseForCallbackWorkflow;
        private static readonly OidcCredentials __oidcCredentials;
        private static readonly ServerId __serverId;

        // static constructor
        static OidcAuthenticatorMockedTests()
        {
            __awsForOidcCredentials = OidcCredentials.Create("awsToken");
            __azureCredentials = new AzureCredentials("azureToken", expiration: null);
            __collectionNamespace = CollectionNamespace.FromFullName("db.coll");
            __gcpCredentials = new GcpCredentials("gcpToken");
            __oidcCredentials = OidcCredentials.Create(new BsonDocument("accessToken", 1), new BsonDocument(), Mock.Of<IClock>(), OidcTimeSynchronizer.Instance);
            __endpoint2 = new DnsEndPoint("localhost", 27018);

            __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));

            __descriptionCommandWireProtocol = new ConnectionDescription(
                new ConnectionId(__serverId),
                new HelloResult(
                    new BsonDocument("ok", 1)
                    .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                    .Add("maxWireVersion", WireVersion.Server70)));

            __initialSaslStartResponseForCallbackWorkflow = new BsonDocument
            {
                { "clientId", "clientIdValue" },
                { "requestScopes", "requestScopes" },
                { "issuer", "issuer" }
            };

            // aws device workflow relies on file reading to get oidc token
            File.WriteAllText(path: "awsToken", contents: "awsToken");
        }

        public OidcAuthenticatorMockedTests()
        {
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);
        }
        #endregion

        [Fact]
        public void Constructor_should_throw_when_endpoint_is_null()
        {
            var exception = Record.Exception(() => MongoOidcAuthenticator.CreateAuthenticator("$external", principalName: "name", new KeyValuePair<string, string>[0], endPoint: null, serverApi: null));

            exception.Should().BeOfType<ArgumentNullException>().Which.Message.Should().Contain("endpoint");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_with_callbacks_should_not_call_provider_workflow_and_use_cache(
            [Values(false, true)] bool withExpiredOidcCredentials,
            [Values(false, true)] bool async)
        {
            int requestCallbackCalled = 0;
            int refreshCallbackCalled = 0;

            var properties = CreateAuthorizationProperties(
                withRequestCallback: true,
                withRefreshCallback: true,
                providerName: null,
                requestCallbackCalled: (a, b, ct) => requestCallbackCalled++,
                refreshCallbackCalled: (a, b, c, ct) => refreshCallbackCalled++);

            using var mockConnection = CreateConnection();

            var clock = FrozenClock.FreezeUtcNow();

            var externalAuthenticatorsMock = CreateExternalCredentialsAuthenticators(
                provider: null,
                clock,
                async,
                out var verifyExpectedDeviceCalls,
                out _,
                useActualOidcProvider: true);
            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                PrincipalName,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                externalAuthenticatorsMock.Object);

            // attempt 1
            var exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: false);

            verifyExpectedDeviceCalls(0);
            AssertCallbacksCall(requestCount: 1, refreshCount: 0);

            exception.Should().BeNull();

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("n", PrincipalName),
                expectedContinueDoument: new BsonDocument("jwt", RequestAccessToken));

            // attempt 2
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);

            exception.Should().BeNull();
            verifyExpectedDeviceCalls(0);
            AssertCallbacksCall(requestCount: 1, refreshCount: 0);

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("jwt", RequestAccessToken), // cached value
                expectedContinueDoument: null);

            if (withExpiredOidcCredentials)
            {
                clock.UtcNow += OidcCredentials.ExpirationWindow + TimeSpan.FromMilliseconds(1);
            }

            // attempt 3
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                // cache presents: withExpiredOidcCredentials determines whether expired or no
                onlySaslStart: true);

            exception.Should().BeNull();
            verifyExpectedDeviceCalls(0);
            AssertCallbacksCall(requestCount: 1, refreshCount: withExpiredOidcCredentials ? 1 : 0);

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("jwt", withExpiredOidcCredentials ? RefreshAccessToken : RequestAccessToken),
                expectedContinueDoument: null);

            void AssertCallbacksCall(int requestCount, int refreshCount)
            {
                requestCallbackCalled.Should().Be(requestCount);
                refreshCallbackCalled.Should().Be(refreshCount);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_with_callbacks_should_clear_cache_when_failure([Values(false, true)] bool async)
        {
            var requestException = new Exception("Request callback failure.");
            var properties = CreateAuthorizationProperties(
                withRequestCallback: true,  // no-op because mocking
                withRefreshCallback: true, // no-op because mocking
                providerName: null,
                requestCallbackCalled: null, // no-op because mocking
                refreshCallbackCalled: null); // no-op because mocking

            using var mockConnection = CreateConnection();

            var clock = FrozenClock.FreezeUtcNow();

            var externalAuthenticatorsMock = CreateExternalCredentialsAuthenticators(
                provider: null,
                clock,
                async,
                out _,
                out var verifyClearCalls,
                clearException: requestException);
            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                PrincipalName,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                externalAuthenticatorsMock.Object);

            // attempt 1
            var exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: false);

            exception.Should().BeNull();

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("n", PrincipalName),
                expectedContinueDoument: new BsonDocument("jwt", __oidcCredentials.AccessToken));

            verifyClearCalls(0);

            // attempt 2
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: false); // no actual cached value configured in mock, so still need 2 server calls

            exception.Should().Be(requestException);
            verifyClearCalls(1);

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("n", PrincipalName),
                expectedContinueDoument: null); // no sending
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_with_callbacks_and_different_cacheKey_should_create_different_oidc_creds(
            [Values(false, true)] bool isPrincipalNameDifferent, // isPrincipalNameDifferent=false, the cache key difference is in endpoint
            [Values(false, true)] bool async)
        {
            int requestCallbackCalled = 0;
            int refreshCallbackCalled = 0;

            var properties = CreateAuthorizationProperties(
                withRequestCallback: true,
                withRefreshCallback: true,
                providerName: null,
                requestCallbackCalled: (a, b, ct) => requestCallbackCalled++,
                refreshCallbackCalled: (a, b, c, ct) => refreshCallbackCalled++);

            using var mockConnection = CreateConnection();
            var authenticators = ExternalCredentialsAuthenticators.Instance;
            var clock = FrozenClock.FreezeUtcNow();


            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                PrincipalName,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                authenticators);  // no mocking

            var lazyCache = GetCacheDictionary();
            lazyCache.ToList()
                .Should().ContainSingle()
                .Which.Key.PrincipalName
                .Should().Be(PrincipalName);

            var exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: false);
            exception.Should().BeNull();
            AssertCallbacksCall(requestCount: 1, refreshCount: 0);

            // create the same authenticator again => expect using cache and same requestCount
            authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                PrincipalName,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                authenticators);  // no mocking

            lazyCache = GetCacheDictionary();
            lazyCache
                .ToList()
                .Should().ContainSingle().Which.Key.PrincipalName
                .Should().Be(PrincipalName);

            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);
            exception.Should().BeNull();
            AssertCallbacksCall(requestCount: 1, refreshCount: 0);

            // create a similar authenticator again but with different cache key => expect requestCount++
            properties = CreateAuthorizationProperties(
                withRequestCallback: true,
                withRefreshCallback: true,
                providerName: null,
                requestCallbackCalled: (a, b, ct) => requestCallbackCalled++,
                refreshCallbackCalled: (a, b, c, ct) => refreshCallbackCalled++);
            authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                isPrincipalNameDifferent ? PrincipalName2 : PrincipalName,
                properties,
                isPrincipalNameDifferent ? __serverId.EndPoint : __endpoint2,
                serverApi: null,
                authenticators); // no mocking

            lazyCache = GetCacheDictionary();
            var expectedCachedRecords = lazyCache.ToList().Should().HaveCount(2).And.Subject;
            expectedCachedRecords.Select(r => r.Key.PrincipalName).Should().Contain(new[] { PrincipalName, isPrincipalNameDifferent ? PrincipalName2 : PrincipalName });
            expectedCachedRecords.Select(r => r.Key.EndPoint).Should().Contain(new[] { __serverId.EndPoint, isPrincipalNameDifferent ? __serverId.EndPoint : __endpoint2 });

            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: false);
            exception.Should().BeNull();
            AssertCallbacksCall(requestCount: 2, refreshCount: 0);

            void AssertCallbacksCall(int requestCount, int refreshCount)
            {
                requestCallbackCalled.Should().Be(requestCount);
                refreshCallbackCalled.Should().Be(refreshCount);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_with_provider_workflow_should_clear_cache_when_failure(
            [Values("azure", "gcp")] string provider, // aws doesn't use cache
            [Values(false, true)] bool async)
        {
            var requestException = new Exception("Device workflow failure.");
            var properties = CreateAuthorizationProperties(
                withRequestCallback: false,  // no-op because mocking
                withRefreshCallback: false, // no-op because mocking
                provider);

            using var mockConnection = CreateConnection();

            var clock = FrozenClock.FreezeUtcNow();

            var externalAuthenticatorsMock = CreateExternalCredentialsAuthenticators(
                provider: provider,
                clock,
                async,
                out _,
                out var verifyClearCalls,
                clearException: requestException);

            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                principalName: null,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                externalAuthenticatorsMock.Object);

            verifyClearCalls(0);

            // attempt 1
            var exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);

            exception.Should().Be(requestException);
            verifyClearCalls(1);

            AssertMessage(mockConnection); // no sending
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_without_callbacks_should_use_provider_workflow(
            [Values("aws", "azure", "gcp")] string provider,
            [Values(false, true)] bool async)
        {
            var properties = CreateAuthorizationProperties(
                withRequestCallback: false,
                withRefreshCallback: false,
                provider);

            using var mockConnection = CreateConnection();
            var environmentVariableProviderMock = new Mock<IEnvironmentVariableProvider>();
            environmentVariableProviderMock.Setup(c => c.GetEnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE")).Returns($"{provider}Token");

            var clock = FrozenClock.FreezeUtcNow();

            var externalAuthenticatorsMock = CreateExternalCredentialsAuthenticators(
                provider,
                clock,
                async,
                out var verifyExpectedDeviceCalls,
                out var verifyClearCalls);
            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                principalName: null,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                externalAuthenticatorsMock.Object);

            // attempt 1
            var exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true); // device workflow needs only saslStart

            verifyExpectedDeviceCalls?.Invoke(1); // null for providerName: aws
            verifyClearCalls?.Invoke(0);
            exception.Should().BeNull();

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("jwt", $"{provider}Token"),
                expectedContinueDoument: null);

            // attempt 2
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);

            exception.Should().BeNull();
            verifyExpectedDeviceCalls?.Invoke(2);
            verifyClearCalls?.Invoke(0);

            AssertMessage(
                mockConnection,
                expectedStartDocument: new BsonDocument("jwt", $"{provider}Token"),
                expectedContinueDoument: null);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_correctly_call_reauhentication_when_requested(
            [Values(null /* callbacks */, "aws", "azure", "gcp")] string provider,
            [Values(false, true)] bool failedReauth,
            [Values(false, true)] bool storeCache,
            [Values(false, true)] bool async)
        {
            var callbackWorkflow = provider == null;
            var reauthenticationException = CoreExceptionHelper.CreateMongoCommandException((int)ServerErrorCode.ReauthenticationRequired);
            var properties = CreateAuthorizationProperties(
                withRequestCallback: callbackWorkflow,
                withRefreshCallback: false,
                provider);

            using var mockConnection = CreateConnection();

            var clock = FrozenClock.FreezeUtcNow();

            var externalAuthenticatorsMock = CreateExternalCredentialsAuthenticators(
                provider: provider,
                clock,
                async,
                out _,
                out var verifyClearCalls,
                storeCredentialsInCache: storeCache);

            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                principalName: null,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                externalAuthenticatorsMock.Object);

            verifyClearCalls(0);

            var exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: !callbackWorkflow); // auth complited
            exception.Should().BeNull();
            mockConnection.IsInitialized = true;

            AssertMessage(
                mockConnection,
                expectedStartDocument: callbackWorkflow ? new BsonDocument() : new BsonDocument("jwt", $"{provider}Token"),
                expectedContinueDoument: callbackWorkflow ? new BsonDocument("jwt", __oidcCredentials.AccessToken) : null);

            verifyClearCalls(0);

            // operation failure
            mockConnection.EnqueueCommandResponseMessage(reauthenticationException);

            mockConnection.EnqueueCommandResponseMessage(
                async () =>
                {
                    var saslException = await Authenticate(
                        authenticator,
                        mockConnection,
                        async,
                        failedSaslException: failedReauth ? reauthenticationException : null,
                        onlySaslStart: (storeCache && !failedReauth) || !callbackWorkflow,
                        mockedResponsesAfterAuthentication: $@"
                        {{
                            ""cursor"" :
                            {{
                                ""firstBatch"" : [ ],
                                ""id"" : NumberLong(0),
                                ""ns"" : ""{__collectionNamespace}""
                            }},
                            ""ok"" : 1
                        }}");

                    if (saslException != null)
                    {
                        throw saslException;
                    }

                    return null; // no actual message to save
                });

            var operationException = await Record.ExceptionAsync(() => RunFindOperation(mockConnection, async));
            DequeueSentMessage(mockConnection).Should().Contain("find"); // failed initial attempt

            if (failedReauth && storeCache)
            {
                verifyClearCalls(1);
                if (callbackWorkflow)
                {
                    operationException.Should().BeNull();

                    DequeueSentMessage(mockConnection).Should().Contain("saslStart"); // failed reauth sasl command

                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument(),
                        expectedContinueDoument: new BsonDocument("jwt", __oidcCredentials.AccessToken),
                        ignoreNonSaslMessages: true);

                    DequeueSentMessage(mockConnection).Should().Contain("find"); // succeeded attempt
                }
                else
                {
                    operationException.Should().BeOfType<MongoAuthenticationException>().Which.InnerException.Should().Be(reauthenticationException);

                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument("jwt", $"{provider}Token"),
                        expectedContinueDoument: null,
                        ignoreNonSaslMessages: false);
                }
                mockConnection.GetSentMessages().Should().HaveCount(0);
            }
            else if (failedReauth && !storeCache)
            {
                verifyClearCalls(1);
                operationException.Should().BeOfType<MongoAuthenticationException>().Which.InnerException.Should().Be(reauthenticationException);
                if (callbackWorkflow)
                {
                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument(),
                        expectedContinueDoument: null, // no saslConinue because saslStart is failed
                        ignoreNonSaslMessages: false);
                }
                else
                {
                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument("jwt", $"{provider}Token"),
                        expectedContinueDoument: null,
                        ignoreNonSaslMessages: false);
                }
                mockConnection.GetSentMessages().Should().HaveCount(0);
            }
            else if (!failedReauth && storeCache)
            {
                verifyClearCalls(0);
                operationException.Should().BeNull();
                if (callbackWorkflow)
                {
                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument("jwt", __oidcCredentials.AccessToken),
                        expectedContinueDoument: null,
                        ignoreNonSaslMessages: true);
                }
                else
                {
                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument("jwt", $"{provider}Token"),
                        expectedContinueDoument: null,
                        ignoreNonSaslMessages: true);
                }
                DequeueSentMessage(mockConnection).Should().Contain("find"); // succeeded attempt
                mockConnection.GetSentMessages().Should().HaveCount(0);
            }
            else if (!failedReauth && !storeCache)
            {
                verifyClearCalls(0);
                operationException.Should().BeNull();
                if (callbackWorkflow)
                {
                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument(),
                        expectedContinueDoument: new BsonDocument("jwt", __oidcCredentials.AccessToken),
                        ignoreNonSaslMessages: true);
                }
                else
                {
                    AssertMessage(
                        mockConnection,
                        expectedStartDocument: new BsonDocument("jwt", $"{provider}Token"),
                        expectedContinueDoument: null,
                        ignoreNonSaslMessages: true);
                }
                DequeueSentMessage(mockConnection).Should().Contain("find"); // succeeded attempt
                mockConnection.GetSentMessages().Should().HaveCount(0);
            }
            else
            {
                throw new Exception("Unexpected test configuration.");
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Reauthenticate_workflow_should_expire_credentials_only_once([Values(false, true)] bool async)
        {
            int requestCallbackCalled = 0;
            int refreshCallbackCalled = 0;

            var timeout = TimeSpan.FromMinutes(1);
            bool ensureNoCallbackCalls = false;
            var properties = CreateAuthorizationProperties(
                withRequestCallback: true,
                withRefreshCallback: true,
                providerName: null,
                requestCallbackCalled:
                    (a, b, ct) =>
                    {
                        if (ensureNoCallbackCalls)
                        {
                            throw new Exception("Should not be reached.");
                        }
                        requestCallbackCalled++;
                    },
                refreshCallbackCalled: (a, b, c, ct) =>
                {
                    if (ensureNoCallbackCalls)
                    {
                        throw new Exception("Should not be reached.");
                    }
                    refreshCallbackCalled++;
                });

            using var mockConnection = CreateConnection();
            mockConnection.IsInitialized = false; // regular workflow

            var clock = FrozenClock.FreezeUtcNow();

            var prepareAuthenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                PrincipalName,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                ExternalCredentialsAuthenticators.Instance);

            // attempt 0. Cache is saved
            var exception = await Authenticate(
                prepareAuthenticator,
                mockConnection,
                async,
                onlySaslStart: false);
            var cache = GetCacheDictionary();

            cache.Single().Value.CachedCredentials.ShouldBeRefreshed.Should().BeFalse(); // there is a cache before reauth logic

            AssertCallbacksCall(requestCount: 1, refreshCount: 0);

            var authenticator = MongoOidcAuthenticator.CreateAuthenticator(
                source: "$external",
                PrincipalName,
                properties,
                __serverId.EndPoint,
                serverApi: null,
                ExternalCredentialsAuthenticators.Instance);
            mockConnection.IsInitialized = true; // reautentication workflow is enabled

            // attempt 1
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);
            exception.Should().BeNull();

            AssertCallbacksCall(requestCount: 1, refreshCount: 1);
            ensureNoCallbackCalls = true; // the cache won't be touched anymore, otherwise the callback will trigger exception

            // attempt 2
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);
            exception.Should().BeNull();

            AssertCallbacksCall(requestCount: 1, refreshCount: 1);

            // attempt 3
            exception = await Authenticate(
                authenticator,
                mockConnection,
                async,
                onlySaslStart: true);
            exception.Should().BeNull();

            AssertCallbacksCall(requestCount: 1, refreshCount: 1);

            void AssertCallbacksCall(int requestCount, int refreshCount)
            {
                requestCallbackCalled.Should().Be(requestCount);
                refreshCallbackCalled.Should().Be(refreshCount);
            }
        }

        public void Dispose() => OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

        // private methods
        private void AssertMessage(
            MockConnection mockConnection,
            BsonDocument expectedStartDocument = null,
            BsonDocument expectedContinueDoument = null,
            bool ignoreNonSaslMessages = false)
        {
            if (expectedStartDocument != null)
            {
                var message = DequeueSentMessage(mockConnection);
                message["saslStart"].ToInt32().Should().Be(1);
                var messageStartPayloadBytes = message["payload"].AsByteArray;
                var messageStartPayloadDocument = BsonSerializer.Deserialize<BsonDocument>(messageStartPayloadBytes);
                messageStartPayloadDocument.Should().Be(expectedStartDocument);
            }
            else
            {
                if (!TryAssertNonSaslOrReturn())
                {
                    return;
                }
            }

            if (expectedContinueDoument != null)
            {
                var message = DequeueSentMessage(mockConnection);
                message["saslContinue"].ToInt32().Should().Be(1);
                var messageContinuePayloadBytes = message["payload"].AsByteArray;
                var messageContinuePayloadDocument = BsonSerializer.Deserialize<BsonDocument>(messageContinuePayloadBytes);
                messageContinuePayloadDocument.Should().Be(expectedContinueDoument);
            }
            else
            {
                if (!TryAssertNonSaslOrReturn())
                {
                    return;
                }
            }

            bool TryAssertNonSaslOrReturn()
            {
                if (ignoreNonSaslMessages)
                {
                    mockConnection.GetSentMessages().Count.Should().NotBe(0);
                    return false;
                }
                else
                {
                    mockConnection.GetSentMessages().Count.Should().Be(0);
                    return true;
                }
            }
        }

        private async Task<Exception> Authenticate(
            MongoOidcAuthenticator authenticator,
            MockConnection mockConnection,
            bool async,
            bool? onlySaslStart = null,
            Exception failedSaslException = null,
            CancellationToken cancellationToken = default,
            params string[] mockedResponsesAfterAuthentication)
        {
            List<string> saslResponses = new ();
            if (onlySaslStart.HasValue)
            {
                if (!onlySaslStart.Value)
                {
                    saslResponses.AddRange(new[]
                    {
                        // step 1: { "saslStart" : 1, "mechanism" : "MONGODB-OIDC", "payload" : #{ "n" : "PrincipalNameTest" }# } }
                        // step 2: saslStart response:
                        @$"
                            {{
                                conversationId : 1,
                                done : false,
                                payload : BinData(0, ""{Convert.ToBase64String(__initialSaslStartResponseForCallbackWorkflow.ToBson())}""),
                                ok : 1
                            }}",

                        // step 3: { "saslContinue" : 1, "conversationId" : 1, "payload" : #{ "jwt" : "requestAccessToken" }# } 
                        // step 4: saslContinue response:
                        @$"
                            {{
                                conversationId : 1,
                                done : true,
                                payload : BinData(0,""""),
                                ok : 1
                            }}"
                    });
                }
                else
                {
                    saslResponses.AddRange(new[]
                    {
                        // step 1: { "saslStart" : 1, "mechanism" : "MONGODB-OIDC", "payload" : #{ "jws" : "providerNameToken" }# } }
                        // step 2: saslStart response:
                        @$"
                        {{
                            conversationId : 1,
                            done : true,
                            payload : BinData(0,""""),
                            ok : 1
                        }}"
                    });
                }
            }

            if (failedSaslException != null)
            {
                mockConnection.EnqueueCommandResponseMessage(failedSaslException);
            }

            foreach (var mockedResponse in Enumerable.Concat(saslResponses, mockedResponsesAfterAuthentication))
            {
                mockConnection.EnqueueCommandResponseMessage(mockedResponse);
            }

            return async
                ? await Record.ExceptionAsync(() => authenticator.AuthenticateAsync(mockConnection, __descriptionCommandWireProtocol, cancellationToken))
                : Record.Exception(() => authenticator.Authenticate(mockConnection, __descriptionCommandWireProtocol, cancellationToken));
        }

        private Dictionary<string, object> CreateAuthorizationProperties(
            bool withRequestCallback,
            bool withRefreshCallback,
            string providerName,
            Action<string, BsonDocument, CancellationToken> requestCallbackCalled = null,
            Action<string, BsonDocument, BsonDocument, CancellationToken> refreshCallbackCalled = null)
        {
            Dictionary<string, object> properties = new();
            if (withRequestCallback)
            {
                properties.Add(
                    MongoOidcAuthenticator.RequestCallbackMechanismProperyName,
                    OidcTestHelper.CreateRequestCallback(
                        validateToken: false,
                        accessToken: RequestAccessToken,
                        expectedSaslResponseDocument: __initialSaslStartResponseForCallbackWorkflow,
                        callbackCalled: requestCallbackCalled));
            }
            if (withRefreshCallback)
            {
                properties.Add(
                    MongoOidcAuthenticator.RefreshCallbackMechanismProperyName,
                    OidcTestHelper.CreateRefreshCallback(
                        validateToken: false,
                        accessToken: RefreshAccessToken,
                        expectedSaslResponseDocument: __initialSaslStartResponseForCallbackWorkflow,
                        callbackCalled: refreshCallbackCalled));
            };
            if (providerName != null)
            {
                properties.Add(MongoOidcAuthenticator.ProviderMechanismProperyName, providerName);
            }

            return properties;
        }

        private MockConnection CreateConnection()
        {
            var mockConnection = new MockConnection(__serverId, new ConnectionSettings(), new EventCapturer());
            mockConnection.Description = __descriptionCommandWireProtocol;
            mockConnection.GetSentMessages().Count.Should().Be(0);
            return mockConnection;
        }

        private Mock<IExternalCredentialsAuthenticators> CreateExternalCredentialsAuthenticators(
            string provider,
            IClock clock,
            bool async,
            out Action<int> verifyDeviceProviderCalls,
            out Action<int> verifyClearCalls,
            Exception clearException = null,
            bool storeCredentialsInCache = false,
            bool useActualOidcProvider = false)
        {
            var externalCredentialsAuthenticatorsMock = new Mock<IExternalCredentialsAuthenticators>();
            // aws
            var awsMockedProvider = GetMockedProvider(__awsForOidcCredentials, storeCredentialsInCache, out var verifyAwsClearCalls, supportClear: false);
            externalCredentialsAuthenticatorsMock.Setup(a => a.AwsForOidc).Returns(awsMockedProvider.Object);
            // azure
            var azureMockedProvider = GetMockedProvider(__azureCredentials, storeCredentialsInCache, out var verifyAzureClearCalls);
            externalCredentialsAuthenticatorsMock.Setup(a => a.Azure).Returns(azureMockedProvider.Object);
            // gcp
            var gcpMockedProvider = GetMockedProvider(__gcpCredentials, storeCredentialsInCache, out var verifyGcpClearCalls);
            externalCredentialsAuthenticatorsMock.Setup(a => a.Gcp).Returns(gcpMockedProvider.Object);

            switch (provider)
            {
                case "aws":
                    {
                        // oidc aws doesn't support clear
                        verifyDeviceProviderCalls = (expectedCount) => ValidateMockProvider(awsMockedProvider, expectedCount, async);
                        verifyClearCalls = verifyAwsClearCalls;
                    }
                    break;
                case "azure":
                    {
                        if (clearException != null)
                        {
                            azureMockedProvider
                                .Setup(p => p.CreateCredentialsFromExternalSource(It.IsAny<CancellationToken>()))
                                .Throws(clearException);
                            azureMockedProvider
                                .Setup(p => p.CreateCredentialsFromExternalSourceAsync(It.IsAny<CancellationToken>()))
                                .Throws(clearException);
                        }

                        verifyDeviceProviderCalls = (expectedCount) => ValidateMockProvider(azureMockedProvider, expectedCount, async);
                        verifyClearCalls = verifyAzureClearCalls;
                    }
                    break;
                case "gcp":
                    {
                        if (clearException != null)
                        {
                            gcpMockedProvider
                                .Setup(p => p.CreateCredentialsFromExternalSource(It.IsAny<CancellationToken>()))
                                .Throws(clearException);
                            gcpMockedProvider
                                .Setup(p => p.CreateCredentialsFromExternalSourceAsync(It.IsAny<CancellationToken>()))
                                .Throws(clearException);
                        }

                        verifyDeviceProviderCalls = (expectedCount) => ValidateMockProvider(gcpMockedProvider, expectedCount, async);
                        verifyClearCalls = verifyGcpClearCalls;
                    }
                    break;
                case null: // oidc
                    {
                        Mock<IOidcExternalAuthenticationCredentialsProvider> oidcMockedProvider = null;
                        if (!useActualOidcProvider)
                        {
                            oidcMockedProvider = new();

                            if (storeCredentialsInCache)
                            {
                                var oidcCachedProvider = oidcMockedProvider.As<ICredentialsCache<OidcCredentials>>();
                                oidcCachedProvider
                                    .SetupGet(p => p.CachedCredentials)
                                    .Returns(() => GetCachedCredentials(oidcCachedProvider, __oidcCredentials, storeCredentialsInCache));
                            }
                            if (clearException != null)
                            {
                                oidcMockedProvider
                                    .SetupSequence(p => p.CreateCredentialsFromExternalSource(It.IsAny<BsonDocument>(), It.IsAny<CancellationToken>()))
                                    .Returns(__oidcCredentials)
                                    .Throws(clearException);
                                oidcMockedProvider
                                    .SetupSequence(p => p.CreateCredentialsFromExternalSourceAsync(It.IsAny<BsonDocument>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(__oidcCredentials)
                                    .Throws(clearException);
                            }
                            else
                            {
                                oidcMockedProvider
                                    .Setup(p => p.CreateCredentialsFromExternalSource(It.IsAny<BsonDocument>(), It.IsAny<CancellationToken>()))
                                    .Returns(__oidcCredentials);
                                oidcMockedProvider
                                    .Setup(p => p.CreateCredentialsFromExternalSourceAsync(It.IsAny<BsonDocument>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(__oidcCredentials);
                            }
                        }

                        externalCredentialsAuthenticatorsMock
                            .SetupGet(c => c.Oidc)
                            .Returns(() =>
                            {
                                var mockedCache = new Mock<IOidcProvidersCache>();
                                mockedCache
                                    .Setup(c => c.GetProvider(It.IsAny<OidcInputConfiguration>()))
                                    .Returns((OidcInputConfiguration ic) =>
                                    {
                                        // use a mocked provider or create a real one
                                        return oidcMockedProvider?.Object ?? new OidcExternalAuthenticationCredentialsProvider(ic, clock, OidcTimeSynchronizer.Instance);
                                    });
                                mockedCache.SetupGet(c => c.TimeSynchronizer).Returns(OidcTimeSynchronizer.Instance);
                                return mockedCache.Object;
                            });

                        verifyDeviceProviderCalls = (expectedCount) =>
                        {
                            ValidateMockProvider(awsMockedProvider, expectedCount, async);
                            ValidateMockProvider(azureMockedProvider, expectedCount, async);
                            ValidateMockProvider(gcpMockedProvider, expectedCount, async);
                        };
                        verifyClearCalls = oidcMockedProvider != null ?
                            (expectedCount) => oidcMockedProvider.Verify(c => c.Clear(), Times.Exactly(expectedCount)) :
                            null;
                    }
                    break;
                default:
                    {
                        throw new Exception($"Not supported device name: {provider}.");
                    }
            }

            return externalCredentialsAuthenticatorsMock;

            static TDevice GetCachedCredentials<TDevice>(Mock<ICredentialsCache<TDevice>> mockedCredentialsCache, TDevice value, bool storeCredentialsInCache) where TDevice : IExternalCredentials
            {
                var invocations = mockedCredentialsCache.Invocations;
                return
                    invocations.Any(i =>
                        // use cache after first fetching credentials
                        i.Method.Name.Contains(nameof(IOidcExternalAuthenticationCredentialsProvider.CreateCredentialsFromExternalSource)) ||
                        i.Method.Name.Contains(nameof(IExternalAuthenticationCredentialsProvider<TDevice>.CreateCredentialsFromExternalSource))) &&
                    invocations.All(i =>
                        // ignore cache after first clear
                        !i.Method.Name.Contains(nameof(ICredentialsCache<TDevice>.Clear))) &&
                    storeCredentialsInCache
                    ? value
                    : default;
            }

            static Mock<IExternalAuthenticationCredentialsProvider<TCredentials>> GetMockedProvider<TCredentials>(
                TCredentials deviceCredentials,
                bool storeCredentialsInCache,
                out Action<int> verifyClearCalls,
                bool supportClear = true) where TCredentials : IExternalCredentials
            {
                var mockedDeviceProvider = new Mock<IExternalAuthenticationCredentialsProvider<TCredentials>>();
                mockedDeviceProvider
                    .Setup(p => p.CreateCredentialsFromExternalSource(CancellationToken.None))
                    .Returns(deviceCredentials);
                mockedDeviceProvider
                    .Setup(p => p.CreateCredentialsFromExternalSourceAsync(CancellationToken.None))
                    .ReturnsAsync(deviceCredentials);

                // mock ICredentialsCache logic
                var cachedProviderMock = mockedDeviceProvider.As<ICredentialsCache<TCredentials>>();
                if (supportClear)
                {
                    verifyClearCalls = (clearCalls) => cachedProviderMock.Verify(p => p.Clear(), Times.Exactly(clearCalls));
                }
                else
                {
                    verifyClearCalls = (clearCalls) => { /* ignore it */ };
                }

                cachedProviderMock
                    .SetupGet(p => p.CachedCredentials)
                    .Returns(() => GetCachedCredentials(cachedProviderMock, deviceCredentials, storeCredentialsInCache));

                return mockedDeviceProvider;
            }

            static void ValidateMockProvider<TCredentials>(Mock<IExternalAuthenticationCredentialsProvider<TCredentials>> provider, int expectedCount, bool async) where TCredentials: IExternalCredentials
            {
                if (async)
                {
                    provider.Verify(a => a.CreateCredentialsFromExternalSourceAsync(It.IsAny<CancellationToken>()), Times.Exactly(expectedCount));
                }
                else
                {
                    provider.Verify(a => a.CreateCredentialsFromExternalSource(It.IsAny<CancellationToken>()), Times.Exactly(expectedCount));
                }
            }
        }

        private BsonDocument DequeueSentMessage(MockConnection mockConnection)
        {
            var sentMessage = mockConnection.GetSentMessages();
            var result = (CommandRequestMessage)sentMessage[0];
            sentMessage.RemoveAt(0);
            return result
                .WrappedMessage
                .Sections
                .Single()
                .Should()
                .BeOfType<Type0CommandMessageSection<BsonDocument>>()
                .Which
                .Document;
        }

        private Dictionary<OidcInputConfiguration, OidcExternalAuthenticationCredentialsProvider> GetCacheDictionary() =>
            OidcTestHelper
                .GetCachedOidcProviders<ExternalCredentialsAuthenticators, OidcInputConfiguration, OidcExternalAuthenticationCredentialsProvider>(ExternalCredentialsAuthenticators.Instance);

        private async Task RunFindOperation(IConnectionHandle connection, bool async)
        {
            var operation = new FindOperation<BsonDocument>(__collectionNamespace, BsonDocumentSerializer.Instance, CoreTestConfiguration.MessageEncoderSettings);
            using var server = GetMockedServer(connection);
            server.Initialize();
            using var binding = new SingleServerReadWriteBinding(server, NoCoreSession.NewHandle());
            using var bindingHandle = new ReadWriteBindingHandle(binding);
            var cursor = async
                ? await operation.ExecuteAsync(bindingHandle, CancellationToken.None)
                : operation.Execute(bindingHandle, CancellationToken.None);
            _ = cursor.ToList();

            DefaultServer GetMockedServer(IConnectionHandle connection)
            {
                var serverId = new ServerId(new ClusterId(), __endpoint2);
                var connectionPool = Mock.Of<IConnectionPool>(cp =>
                    cp.AcquireConnection(It.IsAny<CancellationToken>()) == connection &&
                    cp.AcquireConnectionAsync(It.IsAny<CancellationToken>()) == Task.FromResult(connection));
                var connectionPoolFactory = Mock.Of<IConnectionPoolFactory>(pf =>
                    pf.CreateConnectionPool(It.IsAny<ServerId>(), It.IsAny<EndPoint>(), It.IsAny<IConnectionExceptionHandler>()) == connectionPool);
#pragma warning disable CS0618 // Type or member is obsolete
                return new DefaultServer(
                    serverId.ClusterId,
                    new ClusterClock(),
                    ClusterConnectionMode.Standalone,
                    ConnectionModeSwitch.UseConnectionMode,
                    null,
                    new ServerSettings(),
                    serverId.EndPoint,
                    connectionPoolFactory,
                    Mock.Of<IServerMonitorFactory>(mf => mf.Create(It.IsAny<ServerId>(), It.IsAny<EndPoint>()) == Mock.Of<IServerMonitor>(m => m.Lock == new object())),
                    null,
                    new Logging.EventLogger<Logging.LogCategories.SDAM>(Mock.Of<IEventSubscriber>(), Mock.Of<ILogger<Logging.LogCategories.SDAM>>()));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }
}
