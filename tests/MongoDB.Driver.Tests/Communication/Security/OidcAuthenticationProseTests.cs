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
using System.Threading;
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
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "OidcMechanism")]
    public class OidcAuthenticationProseTests : LoggableTestClass
    {
        // some auth configuration may support only this name
        private const string DatabaseName = "test";
        private const string CollectionName = "coll";

        private const string ExpiredTokenNamePrefix = "_expires";
        private const string SecondaryPreferedConnectionStringSuffix = "&readPreference=secondaryPreferred";
        private const string DirectConnectionStringSuffix = "&directConnection=true";
        private const string DirectConnectionSecondaryPreferedConnectionStringSuffix = SecondaryPreferedConnectionStringSuffix + DirectConnectionStringSuffix;
        private const string DefaultTokenName = "test_user1";

        public OidcAuthenticationProseTests(ITestOutputHelper output) : base(output)
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
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(), expectedPrincipalName: settings.Credential.Username),
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
                $"test_user1#mongodb://localhost/?authMechanism=MONGODB-OIDC&authMechanismProperties=PROVIDER_NAME:aws{DirectConnectionSecondaryPreferedConnectionStringSuffix}",
                // Multiple Principal User 1
                $"test_user1#mongodb://localhost:27018/?authMechanism=MONGODB-OIDC&authMechanismProperties=PROVIDER_NAME:aws{DirectConnectionSecondaryPreferedConnectionStringSuffix}",
                // Multiple Principal User 2
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
            var providerName = settings.Credential.GetMechanismProperty<string>(MongoOidcAuthenticator.ProviderMechanismProperyName, defaultValue: null);
            DisposableEnvironmentVariable disposableEnvironmentVariable = null;

            try
            {
                if (providerName == null)
                {
                    settings.Credential = MongoCredential.CreateOidcCredential(
                        requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(tokenName), expectedPrincipalName: settings.Credential.Username),
                        principalName: settings.Credential.Username);
                }
                else
                {
                    var enviromentVariableName = "AWS_WEB_IDENTITY_TOKEN_FILE";
                    RequireEnvironment.Check().EnvironmentVariable(enviromentVariableName);
                    var expectedTokenPath = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable(enviromentVariableName)), tokenName);
                    Ensure.That(File.Exists(expectedTokenPath), $"OIDC token {expectedTokenPath} doesn't exist.");
                    disposableEnvironmentVariable = new DisposableEnvironmentVariable(
                        name: enviromentVariableName,
                        value: expectedTokenPath);

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
            finally
            {
                disposableEnvironmentVariable?.Dispose();
            }
        }

        // OIDC Allowed Hosts Blocked
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_handle_allowed_hosts(
            [Values("localhost", "127.0.0.1", "[::1]")] string host,
            [Values(null, "aws")] string providerName,
            [Values("", "dummy", "localhost", "localhost1", "127.0.0.1", "*localhost", "localhost;dummy", "::1", "example.com")] string allowedHosts,
            [Values(false, true)] bool withIgnoredExampleComArgument,
            [Values(false, true)] bool async)
        {
            var allowedHostsList = allowedHosts?.Split(';');
            var connectionString = GetConnectionString(host, providerName: providerName);
            if (withIgnoredExampleComArgument)
            {
                connectionString += "&ignored=example.com";
            }
            var settings = MongoClientSettings.FromConnectionString(GetConnectionString(host, providerName: providerName));
            if (providerName == null)
            {
                settings.Credential = MongoCredential.CreateOidcCredential(
                    requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(), expectedPrincipalName: settings.Credential.Username),
                    principalName: settings.Credential.Username,
                    allowedHosts: allowedHostsList);
            }
            else
            {
                settings.Credential = MongoCredential.CreateOidcCredential(
                    providerName: providerName,
                    allowedHosts: allowedHostsList);
            }

            var exception = await Record.ExceptionAsync(() => TestCase(async, settings));
            var isValidCase = allowedHostsList?.Any(h => h?.Replace("*", "") == host.Replace("[", "").Replace("]", ""));
            if (isValidCase.GetValueOrDefault())
            {
                exception.Should().BeNull();
            }
            else
            {
                var expectedHostsList = string.Join("', '", allowedHostsList ?? MongoOidcAuthenticator.DefaultAllowedHostNames);
                exception
                    .Should().BeOfType<MongoConnectionException>().Which.InnerException
                    .Should().BeOfType<InvalidOperationException>().Which.Message
                    .Should().Be($"The used host '{host.Replace("[", "").Replace("]", "")}' doesn't match allowed hosts list ['{expectedHostsList}'].");
            }
        }

        // Callback Validation
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_handle_invalid_callback_responses(
            [Values(null, "null", "missing", "extra")] string issueType,
            [Values(
                null, // valid callback
                "invalidRequest",
                "invalidRefresh")] string invalidState,
            [Values(false, true)] bool async)
        {
            if ((issueType == null && invalidState != null) || (issueType != null && invalidState == null))
            {
                throw new SkipException("Unsupported arguments combination.");
            }

            var connectionString = GetConnectionString();

            var validTokenPath = GetTokenPath();
            var accessToken = JwtHelper.GetValidTokenOrThrow(validTokenPath);
            var validJwtToken = new BsonDocument("accessToken", accessToken).Add("expiresInSeconds", TimeSpan.FromMinutes(10).TotalSeconds);
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            RequestCallbackProvider requestCallbackProvider;
            RefreshCallbackProvider refreshCallbackProvider;
            if (async)
            {
                requestCallbackProvider = new RequestCallbackProvider(requestCallbackFunc: null, requestCallbackAsyncFunc: (a, b, ct) => Task.Run(() => GetClientResponseDocument()));
                refreshCallbackProvider = new RefreshCallbackProvider(refreshCallbackFunc: null, refreshCallbackAsyncFunc: (a, b, c, ct) => Task.Run(() => GetClientResponseDocument()));
            }
            else
            {
                requestCallbackProvider = new RequestCallbackProvider(requestCallbackFunc: (a, b, ct) => GetClientResponseDocument());
                refreshCallbackProvider = new RefreshCallbackProvider(refreshCallbackFunc: (a, b, c, ct) => GetClientResponseDocument());
            }

            settings.Credential = invalidState switch
            {
                "invalidRequest" => MongoCredential.CreateOidcCredential(requestCallbackProvider),
                "invalidRefresh" => MongoCredential.CreateOidcCredential(requestCallbackProvider: new RequestCallbackProvider((a, b, ct) => validJwtToken), refreshCallbackProvider),
                null => MongoCredential.CreateOidcCredential(
                    // the callbacks must also include an unexpected key in the result to confirm that it is ignored.
                    requestCallbackProvider: OidcTestHelper.CreateRequestCallback(validateInput: true, validateToken: true, accessToken: accessToken, expireInSeconds: 60, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                    refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(validateInput: true, validateToken: true, accessToken: accessToken, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++)),
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
                            .Should().Match<Exception>(e => IsExpectedException(issueType, e));
                    }
                    break;
                case var state when state.EndsWith("Refresh"):
                    {
                        exception.Should().BeNull();

                        var requestCallback = settings.Credential.GetMechanismProperty<IOidcRefreshCallbackProvider>(MongoOidcAuthenticator.RefreshCallbackMechanismProperyName, defaultValue: null);
                        requestCallback.Should().NotBeNull();
                        var credentials = GetCachedCredentials();
                        credentials.Expire();
                        exception = await Record.ExceptionAsync(() => TestCase(async, settings));
                        exception
                            .Should().Match<Exception>(e => e is MongoConnectionException || e is InvalidOperationException).And.Subject.As<Exception>().InnerException
                            .Should().Match<Exception>(e => IsExpectedException(issueType, e));
                    }
                    break;
                default: throw new Exception($"Unexpected state {invalidState}.");
            }

            BsonDocument GetClientResponseDocument() =>
                issueType switch
                {
                    "null" => null,
                    "missing" => new BsonDocument(),
                    "extra" => new BsonDocument("accessToken", 1).Add("dummy", 1),
                    // valid case
                    _ => new BsonDocument("accessToken", 1)
                };
        }

        private bool IsExpectedException(string issueType, Exception e) =>
            issueType switch
            {
                "null" => e is ArgumentException ex && ex.Message.Contains("Value cannot be null"),
                "missing" => e is InvalidOperationException ex && ex.Message.Contains("The provided OIDC credentials must contain 'accessToken'."),
                "extra" => e is InvalidOperationException ex && ex.Message.Contains("The provided OIDC credentials contain unsupported key: 'dummy'."),
                _ => throw new ArgumentException($"Unsupported issue type: {issueType}.")
            };

        // Cached Credentials
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_correctly_cache_creds_with_callbacks_workflow(
            [Values(false, true)] bool async)
        {
            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath());

            // 1. Cache with refresh.
            // Create a new client with a request callback that gives credentials that expire in on minute.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: 60, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: null,
                principalName: settings.Credential.Username);
            await TestCase(async, settings);

            // Ensure that a ``find`` operation adds credentials to the cache.
            var validCredentials = GetCachedCredentials();
            validCredentials.AccessToken.Should().Be(tokenContent);
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            // Create a new client with the same request callback and a refresh callback.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: 60, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: 60, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);
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
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expireInSeconds: 60, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: null);

            await TestCase(async, settings);
            // Ensure that a ``find`` operation adds credentials to the cache.
            validCredentials = GetCachedCredentials();
            validCredentials.AccessToken.Should().Be(tokenContent);
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            // Create a new client with the a request callback but no refresh callback.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: null);
            await TestCase(async, settings);
            // Ensure that a ``find`` operation results in a call to the request callback.
            ValidateCallbacks(expectedRequestCallbackCalls: 2, expectedRefreshCallbackCalls: 0);


            // 3. Cache key includes callback
            // Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            //  Create a new client with a request callback that does not give an ```expiresInSeconds``` value.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: null, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: null,
                principalName: settings.Credential.Username);
            await TestCase(async, settings);
            //  Ensure that a ``find`` operation adds credentials to the cache.
            validCredentials = GetCachedCredentials();
            validCredentials.AccessToken.Should().Be(tokenContent);
            var cache = GetCacheList();
            cache.Should().HaveCount(1);

            // Create a new client with a different request callback.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: null,
                principalName: settings.Credential.Username);
            await TestCase(async, settings);
            cache = GetCacheList();
            // Ensure that a ``find`` operation adds a new entry to the cache.
            cache.Count.Should().Be(2);

            requestCallbackCalls = 0;
            refreshCallbackCalls = 0;

            // 4. Error clears cache
            // #. Clear the cache.
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);

            // Create a new client with a valid request callback that gives credentials
            // that expire within 5 minutes and a refresh callback that gives invalid credentials.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, expireInSeconds: (int)TimeSpan.FromMinutes(5).TotalSeconds - 1, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(validateInput: false, accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, invalidResponseDocument: true, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);
            var client = await TestCase(async, settings, keepClientAlive: true);

            var cachedCredentials = GetCachedCredentials();
            // #. Ensure that a ``find`` operation adds a new entry to the cache.
            cachedCredentials.ShouldBeRefreshed.Should().BeTrue();
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            // # Ensure that a subsequent ``find`` operation results in an error.
            var exception = await Record.ExceptionAsync(() => TestCase(async, settings));
            exception
                .Should().BeOfType<MongoConnectionException>().Which.InnerException
                .Should().BeOfType<InvalidOperationException>().Which.Message
                .Should().Contain("The provided OIDC credentials contain unsupported key: 'invalidField'.");

            // Ensure that the cached token has been cleared.
            cachedCredentials = GetCachedCredentials();
            cachedCredentials.Should().BeNull();

            // 5. AWS Automatic workflow does not use cache
            settings.Credential = MongoCredential.CreateOidcCredential(providerName: "aws");
            await TestCase(async, settings);
            // #. Ensure that a ``find`` operation does not add credentials to the cache.
            cache = GetCacheList();
            cache.Any(c => c.Key.ProviderName != null).Should().BeFalse();

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

            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath());

            const string commandName = "find";
            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // 1. Succeeds
            // #. Create request and refresh callbacks that return valid credentials that will not expire soon.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
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
            var failPointCommand = FailPoint.CreateFailPointCommand(times: 1, errorCode: (int)ServerErrorCode.ReauthenticationRequired, applicationName, commandName);
            // #. Perform another find operation that succeeds.
            await TestCase(async, client: client, failPoint: failPointCommand);

            // #. Assert that the refresh callback has been called once, if possible.
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

            // Create request and refresh callbacks that return valid credentials that will not expire soon.
            // Perform a ``find`` operation that succeeds.
            client = await TestCase(async, settings, applicationName: applicationName, keepClientAlive: true);

            // #. Force a reauthenication using a ``failCommand`` of the form:
            failPointCommand = FailPoint.CreateFailPointCommand(times: 2, errorCode: (int)ServerErrorCode.ReauthenticationRequired, applicationName, commandName, "saslStart");

            // #. Perform a ``find`` operation that succeeds.
            // #. Close the client.

            // Failpoint details:
            // * Find: triggeres reauthentication
            // * SaslStart: makes the reauth call failed that will lead to full cache clearing and last Auth call from scratch
            // * Successfull attempt
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

            // Failpoint details:
            // * Find: triggeres reauthentication
            // * SaslStart: makes the reauth call failed, but given there is no cache at this point,
            // a new Auth attempt is not triggered and the whole operation is failed
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

            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath());

            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // #. Create a client with a request callback that returns a valid token that will not expire soon.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username),
                principalName: settings.Credential.Username);

            // # Set a fail point for ``saslStart`` commands of the form:
            var failPointCommand = FailPoint.CreateFailPointCommand(times: 2, errorCode: 18, applicationName, "saslStart"); // should be no-op
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
            [Values(false, true)] bool withExpiredCredentials,
            [Values(false)] bool async)
        {
            if (port == 27017 && tokenName.Contains("user2"))
            {
                throw new SkipException("The user2 must be used only with port 27018.");
            }

            int? tokenExpiredInSeconds = withExpiredCredentials ? null : 600;
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

            var connectionString = GetConnectionString(port: port, principalName: principalName);
            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: GetTokenPath(tokenName), expectedPrincipalName: principalName, expireInSeconds: tokenExpiredInSeconds, validateToken: false),
                refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(accessToken: GetTokenPath(tokenName, allowExpired: false), expectedPrincipalName: principalName, expireInSeconds: tokenExpiredInSeconds, validateToken: false),
                principalName: principalName);

            await TestCase(async, settings, expirationDate);
        }

        // Lock Avoids Extra Callback Calls
        [Theory]
        [ParameterAttributeData]
        public void Oidc_authentication_should_avoid_extra_callbacks([Values(false, true)] bool async)
        {
            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            var applicationName = $"lockCallbacks_{async}";

            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath());

            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // #. Create a request callback that returns a token that will expire soon, and
            // a refresh callback.Ensure that the request callback has a time delay, and
            // that we can record the number of times each callback is called.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(
                    accessToken: tokenContent,
                    expectedPrincipalName: settings.Credential.Username,
                    expireInSeconds: (int)TimeSpan.FromMinutes(1).TotalSeconds,
                    callbackCalled: (a, b, ct) =>
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1)); // the second thread should start waiting on a lock
                        requestCallbackCalls++;
                    }),
                refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);

            // *Spawn two threads that do the following:
            var thread1 = new Thread(() =>
            {
                //* -Create a client with the callbacks.
                //* -Run a find operation that succeeds.
                //* -Close the client.
                //* -Create a new client with the callbacks.
                //* -Run a find operation that succeeds.
                //* -Close the client
                TestCase(async, settings, applicationName: applicationName).GetAwaiter().GetResult();
                TestCase(async, settings, applicationName: applicationName).GetAwaiter().GetResult();
            })
            {
                IsBackground = true
            };
            var thread2 = new Thread(() =>
            {
                //* -Create a client with the callbacks.
                //* -Run a find operation that succeeds.
                //* -Close the client.
                //* -Create a new client with the callbacks.
                //* -Run a find operation that succeeds.
                //* -Close the client
                TestCase(async, settings, applicationName: applicationName).GetAwaiter().GetResult();
                TestCase(async, settings, applicationName: applicationName).GetAwaiter().GetResult();
            })
            {
                IsBackground = true
            };
            thread1.Start();
            thread2.Start();

            thread1.Join();
            thread2.Join();

            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 2);

            void ValidateCallbacks(int expectedRequestCallbackCalls, int expectedRefreshCallbackCalls)
            {
                requestCallbackCalls.Should().Be(requestCallbackCalls);
                refreshCallbackCalls.Should().Be(refreshCallbackCalls);
            }
        }

        // Separate Connections Avoid Extra Callback Calls
        [Theory]
        [ParameterAttributeData]
        public async Task Oidc_authentication_should_avoid_extra_callbacks_with_separate_connections([Values(false, true)] bool async)
        {
            int requestCallbackCalls = 0;
            int refreshCallbackCalls = 0;

            var applicationName = $"lockCallbacksSeparateConnections_{async}";

            var tokenContent = JwtHelper.GetValidTokenOrThrow(GetTokenPath());

            var connectionString = GetConnectionString();
            var settings = MongoClientSettings.FromConnectionString(connectionString);

            // Create request and refresh callbacks that return tokens that will not expire
            // soon.Ensure that we can record the number of times each callback is called.
            settings.Credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: OidcTestHelper.CreateRequestCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, ct) => requestCallbackCalls++),
                refreshCallbackProvider: OidcTestHelper.CreateRefreshCallback(accessToken: tokenContent, expectedPrincipalName: settings.Credential.Username, callbackCalled: (a, b, c, ct) => refreshCallbackCalls++),
                principalName: settings.Credential.Username);

            // Create two clients using the callbacks
            // Peform a find operation on each client that succeeds.
            var client1 = await TestCase(async: async, settings, applicationName: applicationName, keepClientAlive: true);
            var client2 = await TestCase(async: async, settings, applicationName: applicationName, keepClientAlive: true);

            // Ensure that the request callback has been called once and the refresh callback has not been called.
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 0);

            requestCallbackCalls = 0;
            refreshCallbackCalls = 0;
            var failPointCommand = FailPoint.CreateFailPointCommand(times: 1, errorCode: (int)ServerErrorCode.ReauthenticationRequired, applicationName, "find");
            // Perform a ``find`` operation that succeds.
            await TestCase(async, client: client1, failPoint: failPointCommand);
            // Ensure that the request callback has been called once and the refresh callback has been called once.
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 1);

            requestCallbackCalls = 0;
            refreshCallbackCalls = 0;
            // Perform a ``find`` operation that succeds.
            await TestCase(async, client: client2, failPoint: failPointCommand);
            // Ensure that the request callback has been called once and the refresh callback has been called once.
            ValidateCallbacks(expectedRequestCallbackCalls: 1, expectedRefreshCallbackCalls: 1);


            void ValidateCallbacks(int expectedRequestCallbackCalls, int expectedRefreshCallbackCalls)
            {
                requestCallbackCalls.Should().Be(requestCallbackCalls);
                refreshCallbackCalls.Should().Be(refreshCallbackCalls);
            }
        }

        protected override void DisposeInternal()
        {
            OidcTestHelper.ClearStaticCache(ExternalCredentialsAuthenticators.Instance);
            base.DisposeInternal();
        }

        // private methods
        private string GetConnectionString(string host = "localhost", int port = 27017, string principalName = null, string providerName = null, bool onlyPrimary = false)
        {
            var connectionString = $"mongodb://{(principalName != null ? $"{principalName}@" : "")}{host}:{port}?authMechanism=MONGODB-OIDC{DirectConnectionStringSuffix}";
            if (!onlyPrimary)
            {
                connectionString += SecondaryPreferedConnectionStringSuffix;
            }
            if (providerName != null)
            {
                connectionString += "&authMechanismProperties=PROVIDER_NAME:aws";
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
            GetCacheList()
                .Single()
                .Value
                .CachedCredentials;

        private Dictionary<OidcInputConfiguration, OidcExternalAuthenticationCredentialsProvider> GetCacheList() =>
            OidcTestHelper
                .GetCachedOidcProviders<ExternalCredentialsAuthenticators, OidcInputConfiguration, OidcExternalAuthenticationCredentialsProvider>(ExternalCredentialsAuthenticators.Instance);

        private string GetTokenPath(string tokenName = DefaultTokenName, bool allowExpired = true)
        {
            tokenName = allowExpired ? tokenName : tokenName.Replace(ExpiredTokenNamePrefix, "");
            return Path.Combine(
                Environment.GetEnvironmentVariable("OIDC_TOKEN_DIR"),
                tokenName);
        }
    }
}
