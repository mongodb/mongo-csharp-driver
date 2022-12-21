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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Authentication.External;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Authentication;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "OidcMechanism")]
    public class OidcAuthenticationProseTests : IDisposable
    {
        // some auth configuration may support only this name
        private const string DatabaseName = "test";
        private const string CollectionName = "coll";
        private const string ExpiredTokenNamePrefix = "_expires";
        private const string SecondaryPreferedConnectionStringSuffix = "&readPreference=secondaryPreferred";
        private const string DirectConnectionStringSuffix = "&directConnection=true";
        private const string DirectConnectionSecondaryPreferedConnectionStringSuffix = SecondaryPreferedConnectionStringSuffix + DirectConnectionStringSuffix;

        public OidcAuthenticationProseTests()
        {
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);
            EnsureOidcIsConfigured();
        }

        // Prose tests
        // Callback-Driven Auth
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_handle_single_principal_and_configured_callback(
            [Values(
                // Single Principal Implicit Username
                $"mongodb://localhost/?authMechanism=MONGODB-OIDC{DirectConnectionSecondaryPreferedConnectionStringSuffix}",
                // Single Principal Explicit Username
                $"mongodb://test_user1@localhost/?authMechanism=MONGODB-OIDC{DirectConnectionSecondaryPreferedConnectionStringSuffix}")]
            string connectionString,
            [Values(false, true)] bool async)
        {
            const string tokenName = "test_user1";

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(tokenName), expectedPrincipalName: settings.Credential.Username),
                principalName: settings.Credential.Username);

            await TestCase(async, settings);
        }

        // Multiple Principals
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_handle_Multiple_Principals(
            [Values(
                // Multiple Principal User 1
                $"test_user1#mongodb://test_user1@localhost:27018/?authMechanism=MONGODB-OIDC{DirectConnectionSecondaryPreferedConnectionStringSuffix}",
                // Multiple Principal User 2
                $"test_user2#mongodb://test_user2@localhost:27018/?authMechanism=MONGODB-OIDC{DirectConnectionSecondaryPreferedConnectionStringSuffix}",

                // AWS Automatic Auth
                // Single Principal
                $"test_user1#mongodb://localhost:27018/?authMechanism=MONGODB-OIDC&authMechanismProperties=PROVIDER_NAME:aws{DirectConnectionSecondaryPreferedConnectionStringSuffix}",
                $"test_user2#mongodb://localhost:27018/?authMechanism=MONGODB-OIDC&authMechanismProperties=PROVIDER_NAME:aws{DirectConnectionSecondaryPreferedConnectionStringSuffix}",
                // Multiple Principal No User
                $"#mongodb://localhost:27018/?authMechanism=MONGODB-OIDC{DirectConnectionSecondaryPreferedConnectionStringSuffix}"
            )] string connectionDetails,
            [Values(false, true)] bool async)
        {
            var split = connectionDetails.Split('#');
            var tokenName = split[0];
            var connectionString = split[1];

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            var providerName = settings.Credential.GetMechanismProperty<string>(MongoOidcAuthenticator.ProviderName, defaultValue: null);
            if (providerName == null)
            {
                settings.Credential = MongoCredential.CreateOidcCredential(
                    requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(tokenName), expectedPrincipalName: settings.Credential.Username),
                    principalName: settings.Credential.Username);
            }
            else
            {
                RequireEnvironment.Check().EnvironmentVariable("AWS_WEB_IDENTITY_TOKEN_FILE");

                _ = JwtHelper.GetValidTokenOrThrow(GetTokenPath(tokenName));
                settings.Credential = MongoCredential.CreateOidcCredential(providerName: providerName);
            }

            var exception = await Record.ExceptionAsync(() => TestCase(async, settings));
            if (string.IsNullOrEmpty(tokenName))
            {
                exception.Should().BeOfType<MongoAuthenticationException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        // Callback Validation
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_handle_invalid_callback_responses(
            [Values(false, true)] bool isNullResponse,
            [Values(
                null, // valid callback
                "invalidSyncRequest",
                "invalidAsyncRequest",
                "invalidSyncRefresh",
                "invalidAsyncRefresh")] string invalidState,
            [Values(false, true)] bool async)
        {
            var connectionString = GetConnectionString();

            var validTokenPath = GetTokenPath("test_user1");
            var accessToken = JwtHelper.GetValidTokenOrThrow(validTokenPath);
            var validJwtToken = new BsonDocument("accessToken", accessToken).Add("expiresInSeconds", TimeSpan.FromMinutes(10).TotalSeconds);
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            settings.Credential = invalidState switch
            {
                "invalidSyncRequest" => MongoCredential.CreateOidcCredential(requestTokenFunc: (a, b, ct) => InvalidDocument()),
                "invalidAsyncRequest" => MongoCredential.CreateOidcCredential(requestTokenAsyncFunc: (a, b, ct) => Task.FromResult(InvalidDocument())),
                "invalidSyncRefresh" => MongoCredential.CreateOidcCredential(requestTokenFunc: (a, b, ct) => validJwtToken, refreshTokenFunc: (a, b, c, ct) => InvalidDocument()),
                "invalidAsyncRefresh" => MongoCredential.CreateOidcCredential(requestTokenFunc: (a, b, ct) => validJwtToken, refreshTokenAsyncFunc: (a, b, c, ct) => Task.FromResult(InvalidDocument())),
                null => MongoCredential.CreateOidcCredential(
                    // the callbacks must also include an unexpected key in the result to confirm that it is ignored.
                    requestTokenFunc: OidcTestHelper.CreateRequestCallback(invalidResponseDocument: true, validateInput: true, validateToken: true, accessToken: accessToken, expireInSeconds: 60, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                    refreshTokenFunc: OidcTestHelper.CreateRefreshCallback(invalidResponseDocument: true, validateInput: true, validateToken: true, accessToken: accessToken, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++)),
                _ => throw new Exception($"Unexpected invalid state: {invalidState}."),
            };
            var exception = await Record.ExceptionAsync(() => TestCase(async, settings));
            switch (invalidState)
            {
                case null:
                    {
                        // Perform a ``find`` operation that succeeds.  Verify that the request
                        // callback was called with the appropriate inputs, including the timeout
                        // parameter if possible.Ensure that there are no unexpected fields.
                        exception.Should().BeNull();
                        requestCallbackCalls.Should().Be(1);
                        refreshCallbackCalls.Should().Be(0);

                        // Perform another ``find`` operation that succeeds.Verify that the refresh
                        // callback was called with the appropriate inputs, including the timeout
                        // parameter if possible.Ensure that there are no unexpected fields.
                        await TestCase(async, settings);
                        requestCallbackCalls.Should().Be(1);
                        refreshCallbackCalls.Should().Be(1);
                    }
                    break;
                case var state when state.EndsWith("Request"):
                    {
                        exception
                            .Should().Match<Exception>(e => e is MongoConnectionException || e is InvalidOperationException).And.Subject.As<Exception>().InnerException
                            .Should().Match<Exception>(e => isNullResponse ? e is ArgumentNullException : e is InvalidOperationException).And.Subject.As<Exception>().Message
                            .Should().Match<string>(m => m.Contains(isNullResponse ? "Value cannot be null" : "The provided OIDC credentials contain unsupported key: dummy."));
                    }
                    break;
                case var state when state.EndsWith("Refresh"):
                    {
                        exception.Should().BeNull();

                        var requestCallback = settings.Credential.GetMechanismProperty<IRefreshCallbackProvider>(MongoOidcAuthenticator.RefreshCallbackName, defaultValue: null);
                        requestCallback.Should().NotBeNull();
                        var provider = OidcTestHelper.GetOidcProvider<ExternalCredentialsAuthenticators, IOidcExternalAuthenticationCredentialsProvider>(ExternalCredentialsAuthenticators.Instance);
                        provider.CachedCredentials.Expire();
                        exception = await Record.ExceptionAsync(() => TestCase(async, settings));
                        exception
                            .Should().Match<Exception>(e => e is MongoConnectionException || e is InvalidOperationException).And.Subject.As<Exception>().InnerException
                            .Should().Match<Exception>(e => isNullResponse ? e is ArgumentNullException : e is InvalidOperationException).And.Subject.As<Exception>().Message
                            .Should().Match<string>(m => m.Contains(isNullResponse ? "Value cannot be null" : "The provided OIDC credentials contain unsupported key: dummy."));
                    }
                    break;
                default: throw new Exception($"Unexpected state {invalidState}.");
            }

            BsonDocument InvalidDocument() => isNullResponse ? null : new BsonDocument("dummy", 1);
        }

        // Cached Credentials
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_cache_creds_with_callbacks_workflow([Values(false, true)] bool async)
        {
            const string tokenName = "test_user1";

            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath(tokenName));

            // 1. Cache with refresh.
            // Create a new client with a request callback that gives credentials that expire in on minute.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: 60, callbackCalled: (a,b,ct) => requestCallbackCalls++),
                refreshTokenFunc: OidcTestHelper.CreateRefreshCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: 60, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);
            var client = await TestCase(async, settings, keepClientAlive: true);

            // Ensure that a ``find`` operation adds credentials to the cache.
            var validCredentials = GetCachedCredentials();
            validCredentials.AccessToken.Should().Be(tokenContent);
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            // Create a new client with the same request callback and a refresh callback.
            await TestCase(async, settings);
            // Ensure that a ``find`` operation results in a call to the refresh callback.
            // Validate the refresh callback inputs, including the timeout parameter if possible.
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 1);

            // 2. Cache with no refresh
            // Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            requestCallbackCalls = 0;
            refreshCallbackCalls = 0;

            // Create a new client with a request callback that gives credentials that expire in one minute.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expireInSeconds: 60, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshTokenFunc: null);

            await TestCase(async, settings);
            // Ensure that a ``find`` operation adds credentials to the cache.
            validCredentials = GetCachedCredentials();
            validCredentials.AccessToken.Should().Be(tokenContent);
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshTokenFunc: null);
            await TestCase(async, settings);
            // Ensure that a ``find`` operation results in a call to the request callback.
            ValidateCallbacks(expectedRequestCallbackCalls: 2, expectedRefreshCallbackCalls: 0);


            // 3. Cache key includes callback
            // Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            //  Create a new client with a request callback that does not give an ```expiresInSeconds``` value.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: null, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshTokenFunc: null,
                principalName: settings.Credential.Username);
            await TestCase(async, settings);
            // - Ensure that a ``find`` operation adds credentials to the cache.
            validCredentials = GetCachedCredentials();
            validCredentials.AccessToken.Should().Be(tokenContent);
            var cache = OidcTestHelper.GetOidcProvidersCache<ExternalCredentialsAuthenticators, OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>(ExternalCredentialsAuthenticators.Instance);
            cache.Value.Count.Should().Be(1);

            // Create a new client with a different request callback.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshTokenFunc: null,
                principalName: settings.Credential.Username);
            await TestCase(async, settings);
            cache.Value.Count.Should().Be(2);

            requestCallbackCalls = 0;
            refreshCallbackCalls = 0;

            // 4. Error clears cache
            // #. Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);
            // Create a new client with a valid request callback that gives credentials
            // refresh callback that gives invalid credentials.
            // that expire within 5 minutes and a refresh callback that gives invalid credentials.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: (int)TimeSpan.FromMinutes(5).TotalSeconds - 1, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshTokenFunc: OidcTestHelper.CreateRefreshCallback(validateInput: false, accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, invalidResponseDocument: true, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);
            client = await TestCase(async, settings, keepClientAlive: true);

            var cachedCredentials = GetCachedCredentials();
            // #. Ensure that a ``find`` operation adds a new entry to the cache.
            cachedCredentials.ShouldBeRefreshed.Should().BeTrue();
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            // # Ensure that a subsequent ``find`` operation results in an error.
            var exception = await Record.ExceptionAsync(() => TestCase(async, settings));
            exception
                .Should().BeOfType<MongoConnectionException>().Which.InnerException
                .Should().BeOfType<InvalidOperationException>().Which.Message
                .Should().Contain("The provided OIDC credentials contain unsupported key: invalidField.");

            // Ensure that the cached token has been cleared.
            cachedCredentials = GetCachedCredentials();
            cachedCredentials.Should().BeNull();

            // 5. AWS Automatic workflow does not use cache
            settings.Credential = MongoCredential.CreateOidcCredential(providerName: "aws");
            await TestCase(async, settings);
            // #. Ensure that a ``find`` operation does not add credentials to the cache.
            cache = OidcTestHelper.GetOidcProvidersCache<ExternalCredentialsAuthenticators, OidcCacheKey, IOidcExternalAuthenticationCredentialsProvider>(ExternalCredentialsAuthenticators.Instance);
            cache.Value.Any(c => c.Key.OidcInputConfiguration.ProviderName!= null).Should().BeFalse();

            void ValidateCallbacks(int expectedRequestCallbackCalls, int expectedRefreshCallbackCalls)
            {
                requestCallbackCalls.Should().Be(requestCallbackCalls);
                refreshCallbackCalls.Should().Be(refreshCallbackCalls);
            }
        }

        // Reauthentication
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_behave_when_reauthentication_is_requested(
            [Values(false, true)] bool async)
        {
            string applicationName = $"Reauthentication_{async}";
            const string tokenName = "test_user1";

            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath(tokenName));

            var commandName = "find";
            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // 1. Succeeds
            // #. Create request and refresh callbacks that return valid credentials that will not expire soon.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshTokenFunc: OidcTestHelper.CreateRefreshCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);

            // #. Create a client with the callbacks and an event listener capable
            // of listening for SASL commands.
            var eventCapturer = new EventCapturer().CaptureCommandEvents("saslStart", "saslContinue", OppressiveLanguageConstants.LegacyHelloCommandName, "hello", commandName);
            var client = await TestCase(async, settings, eventCapturer: eventCapturer, keepClientAlive: true, applicationName: applicationName);
            var res = eventCapturer.Events.ToList();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Which.CommandName.Should().Be(OppressiveLanguageConstants.LegacyHelloCommandName);
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Which.CommandName.Should().Be(OppressiveLanguageConstants.LegacyHelloCommandName);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Which.CommandName.Should().Be("saslContinue");
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Which.CommandName.Should().Be("saslContinue");
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Which.CommandName.Should().Be(commandName);
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Which.CommandName.Should().Be(commandName);
            eventCapturer.Any().Should().BeFalse();

            // #. Assert that the refresh callback has not been called.
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            // #. Force a reauthenication using a ``failCommand`` of the form:
            var failPointCommand = FailPoint.CreateFailPointCommand(times: 1, errorCode:391, applicationName, commandName);
            // #. Perform another find operation.
            await TestCase(async, client: client, failPoint: failPointCommand);

            // #. Assert that the refresh callback has been called, if possible.
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 1);
            // #. Assert that a ``find`` operation was started twice and a ``saslStart`` operation was started once during the command execution.
            // #. Assert that a ``find`` operation succeeeded once and the ``saslStart`` operation succeeded during the command execution.
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Which.CommandName.Should().Be(commandName);
            // #. Assert that a ``find`` operation failed once during the command execution.
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>().Which.CommandName.Should().Be(commandName);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Which.CommandName.Should().Be("saslStart");
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Which.CommandName.Should().Be("saslStart");
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Which.CommandName.Should().Be(commandName);
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Which.CommandName.Should().Be(commandName);
            eventCapturer.Any().Should().BeFalse();

            // #. Close the client.
            client.Dispose();

            eventCapturer.Any().Should().BeFalse();

            // 2. Retries and Succeeds with Cache
            // #. Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            // #. Create request and refresh callbacks that return valid credentials that will not expire soon.
            var mongoClientSettings = GetOidcEffectiveMongoClientSettings(settings, applicationName, eventCapturer);
            client = DriverTestConfiguration.CreateDisposableClient(mongoClientSettings);
            // # Perform a find operation that succeeds (to force a speculative auth).
            await TestCase(async, client);

            // #. Force a reauthenication using a ``failCommand`` of the form:
            failPointCommand = FailPoint.CreateFailPointCommand(times: 2, errorCode: 391, applicationName, commandName, "saslStart");

            // #. Perform a ``find`` operation that succeeds.
            // #. Close the client.
            await TestCase(async, client, failPoint: failPointCommand);

            // 3. Retries and Fails with no Cache
            // #. Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            // # Create request and refresh callbacks that return valid credentials that will not expire soon.
            // #. Perform a ``find`` operation that succeeds (to force a speculative auth).
            client = await TestCase(async, settings, eventCapturer: eventCapturer, applicationName: applicationName, keepClientAlive: true);

            // #. Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            // #. Force a reauthenication using a ``failCommand`` of the form:
            // #. Perform a ``find`` operation that fails.
            var exception = await Record.ExceptionAsync(() => TestCase(async, client, failPoint: failPointCommand));
            exception.Should().BeOfType<MongoAuthenticationException>().Which.InnerException.Should().BeOfType<MongoCommandException>();

            void ValidateCallbacks(int expectedRequestCallbackCalls, int expectedRefreshCallbackCalls)
            {
                requestCallbackCalls.Should().Be(requestCallbackCalls);
                refreshCallbackCalls.Should().Be(refreshCallbackCalls);
            }
        }

        // Speculative Authentication
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_support_speculative_authentication([Values(false, true)] bool async)
        {
            var applicationName = $"SpeculativeTest_{async}";
            const string tokenName = "test_user1";

            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath(tokenName));

            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // #. Create a client with a request callback that returns a valid token that will not expire soon.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username),
                principalName: settings.Credential.Username);

            // # Set a fail point for ``saslStart`` commands of the form:
            var failPointCommand = FailPoint.CreateFailPointCommand(times: 2, errorCode: 18, applicationName, "saslStart");
            // #. Perform a ``find`` operation.
            // #. Close the client.
            await TestCase(async, settings, failPoint: failPointCommand, applicationName: applicationName);

            // #. Create a new client with the same properties without clearing the cache.
            // #. Set a fail point for ``saslStart`` commands.
            // #. Perform a ``find`` operation.
            // #. Close the client.
            await TestCase(async, settings, failPoint: failPointCommand, applicationName: applicationName);
        }

        // not prose, but similar tests
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_support_reading_token(
            [Values(
                27017, // server is configured with single principal name
                27018)] // server is configured with multiply principal names
            int port,
            [Values(
                "test_user1_expires", // the credentials will be expired in N min
                "test_user1", // the credentials are valid
                "test_user2")] string tokenName, // the credentials are valid
            [Values(false, true)] bool cacheInvolved,
            [Values(false)] bool async)
        {
            if (port == 27017 && tokenName.Contains("user2"))
            {
                throw new SkipException("The user2 must be used only with port 27018.");
            }

            int? tokenExpiredInSeconds = cacheInvolved ? 600 : null;
            var accessToken = JwtHelper.GetTokenContent(GetTokenPath(tokenName));
            var jwtDetails = JwtHelper.GetJwtDetails(accessToken);
            DateTime? expirationDate = null;
            if (tokenName.Contains(ExpiredTokenNamePrefix))
            {
                expirationDate = jwtDetails.ExpirationDate;
                var expiredAfter = expirationDate - DateTime.UtcNow;
                if (expiredAfter < TimeSpan.FromSeconds(10))
                {
                    // 10 seconds is enough to open a connection, but run an actual operation on the expired token.
                    throw new SkipException($"Expires token is too old. Remaining time: {expiredAfter}.");
                }
            }
            var principalName = tokenName.Replace(ExpiredTokenNamePrefix, "");
            jwtDetails.Subject.Should().Be(principalName);

            var connectionString = GetConnectionString(port, principalName);
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestTokenFunc: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(tokenName), expectedPrincipalName: principalName, expireInSeconds: tokenExpiredInSeconds, validateToken: false),
                refreshTokenFunc: OidcTestHelper.CreateRefreshCallback(accessToken: GetTokenPath(tokenName, allowExpired: false), expectedPrincipalName: principalName, expireInSeconds: tokenExpiredInSeconds, validateToken: false),
                principalName: principalName);

            await TestCase(async, settings, expirationDate);
        }

        public void Dispose() => OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

        // private methods
        private string GetConnectionString(int port = 27017, string principalName = null, string providerName = null, bool onlyPrimary = false)
        {
            var connectionString = $"mongodb://localhost:{port}?authMechanism=MONGODB-OIDC{DirectConnectionStringSuffix}";
            if (!onlyPrimary)
            {
                connectionString += SecondaryPreferedConnectionStringSuffix;
            }
            if (providerName != null)
            {
                connectionString += "&authMechanismProperties=PROVIDER_NAME:aws";
            }
            if (principalName != null)
            {
                connectionString += $"&authMechanismProperties=PRINCIPAL_NAME:{principalName}";
            }
            return connectionString;
        }

        private void EnsureOidcIsConfigured() =>
            // EG also requires aws_test_secrets_role 
            RequireEnvironment
                .Check()
                .EnvironmentVariable("OIDC_TOKEN_DIR")
                .EnvironmentVariable("OIDC_TESTS_ENABLED");

        private MongoClientSettings GetOidcEffectiveMongoClientSettings(MongoClientSettings settings = null, string applicationName = null, EventCapturer eventCapturer = null)
        {
            settings ??= DriverTestConfiguration.GetClientSettings();
            settings.ApplicationName = applicationName;
            settings.RetryReads = false;
            settings.RetryWrites = false;
            settings.MinConnectionPoolSize = 0;
            if (eventCapturer != null)
            {
                settings.ClusterConfigurator = (builder) => builder.Subscribe(eventCapturer);
            }
            return settings;
        }

        private async Task<DisposableMongoClient> TestCase(
            bool async,
            MongoClientSettings settings = null,
            DateTime? expirationTime = null,
            BsonDocument failPoint = null,
            EventCapturer eventCapturer = null,
            string applicationName = null,
            bool keepClientAlive = false)
        {
            settings = GetOidcEffectiveMongoClientSettings(settings, applicationName, eventCapturer);
            var client = DriverTestConfiguration.CreateDisposableClient(settings);
            await TestCase(async, client, expirationTime, failPoint);
            if (keepClientAlive)
            {
                return client;
            }
            else
            {
                client.Dispose();
                return null;
            }
        }

        private async Task TestCase(
            bool async,
            IMongoClient client = null,
            DateTime? expirationTime = null,
            BsonDocument failPoint = null)
        {
            using (failPoint != null ? FailPoint.Configure(DriverTestConfiguration.Client.Cluster, NoCoreSession.NewHandle(), failPoint) : null)
            {
                var database = client.GetDatabase(DatabaseName);
                var collection = database.GetCollection<BsonDocument>(CollectionName);

                if (expirationTime.HasValue)
                {
                    EnsureToken(expirationTime.Value, isValid: true, skipTestIfNotExpected: true);
                    // ensure one connection is opened in the pool
                    _ = database.RunCommand<BsonDocument>("{ ping : 1 }");
                    // wait until token expires
                    await Task.Delay(expirationTime.Value - DateTime.UtcNow + TimeSpan.FromMilliseconds(1));
                    EnsureToken(expirationTime.Value, isValid: false);
                }

                _ = async
                    ? await collection.FindAsync(FilterDefinition<BsonDocument>.Empty)
                    : collection.FindSync(FilterDefinition<BsonDocument>.Empty);
            }

            void EnsureToken(DateTime? expirationDate, bool isValid, bool skipTestIfNotExpected = false)
            {
                if (expirationDate.HasValue)
                {
                    var now = DateTime.UtcNow;
                    if (isValid ? expirationDate < now : expirationDate >= now)
                    {
                        if (skipTestIfNotExpected)
                        {
                            throw new SkipException($"The jwt token is already expired. Now: {now}, but expirationDate: {expirationDate.Value}.");
                        }
                        else
                        {
                            throw new Exception($"The jwt token is already expired. Now: {now}, but expirationDate: {expirationDate.Value}.");
                        }
                    }
                }
            }
        }

        private OidcCredentials GetCachedCredentials() =>
            OidcTestHelper
                .GetOidcProvider<ExternalCredentialsAuthenticators, IOidcExternalAuthenticationCredentialsProvider>(ExternalCredentialsAuthenticators.Instance)
                .CachedCredentials;

        private string GetTokenPath(string tokenName, bool allowExpired = true)
        {
            tokenName = allowExpired ? tokenName : tokenName.Replace(ExpiredTokenNamePrefix, "");
            return Path.Combine(
                Environment.GetEnvironmentVariable("OIDC_TOKEN_DIR"),
                tokenName);
        }
    }
}
