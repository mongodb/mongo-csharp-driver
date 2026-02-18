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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.auth
{
    [Trait("Category", "Integration")]
    [Trait("Category", "Authentication")]
    [Trait("Category", "MongoDbOidc")]
    // Tests in this class can run in multiple OIDC environments controlled by OIDC_ENV:
    // - "test": Uses mocked callback, allows callback configuration and verification
    // - "azure", "gcp", "k8s": Uses real OIDC providers, callback verification skipped
    // Tests that require mocked callback (e.g., cache poisoning) explicitly require "test" environment.
    public class OidcAuthenticationProseTests : LoggableTestClass
    {
        private const string DatabaseName = "test";
        private const string CollectionName = "collName";
        private const string OidcTokensDirEnvName = "OIDC_TOKEN_DIR";
        private const string OidcEnvironmentName = "OIDC_ENV";
        private const string TokenName = "test_user1";

        public OidcAuthenticationProseTests(ITestOutputHelper output) : base(output)
        {
            OidcCallbackAdapterCachingFactory.Instance.Reset();
        }

        // 1.1 Callback is called during authentication
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L47
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_authentication_callback_called_during_authentication([Values(false, true)]bool async)
        {
            EnsureOidcIsConfigured();

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 1.2 Callback is called once for multiple connections
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L54
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_authentication_callback_called_once_for_multiple_connections([Values(false, true)]bool async)
        {
            EnsureOidcIsConfigured();

            var (settings, _, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            await ThreadingUtilities.ExecuteTasksOnNewThreads(10, async t =>
            {
                for (var i = 0; i < 100; i++)
                {
                    _ = async
                        ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                        : collection.FindSync(Builders<BsonDocument>.Filter.Empty);
                }
            }, (int)TimeSpan.FromSeconds(20).TotalMilliseconds);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
        }

        // 2.1 Valid Callback Inputs
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L63
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_valid_callback_inputs([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 2.2 OIDC Callback Returns Null
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L70
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_callback_returns_null([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Events.Should().BeEmpty();
        }

        // 2.3 OIDC Callback Returns Missing Data
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L76
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_callback_returns_missing_data([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock, "wrong token");
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoAuthenticationException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
        }

        // 2.4 Invalid Client Configuration with Callback
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L83
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_invalid_client_configuration([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            settings.Credential = settings.Credential.WithMechanismProperty("ENVIRONMENT", "test");
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            VerifyCallbackUsage(callbackMock, async, Times.Never());
            eventCapturer.Events.Should().BeEmpty();
        }

        // 2.5 Invalid use of ALLOWED_HOSTS
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L89
        [Theory]
        [ParameterAttributeData]
        public async Task Invalid_Allowed_Hosts_Usage([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("azure");

            var (settings, eventCapturer, _) = GetMongoClientSettings();
            settings.Credential = settings.Credential.WithMechanismProperty("ALLOWED_HOSTS", Array.Empty<string>());
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            eventCapturer.Events.Should().BeEmpty();
        }

        // 3.1 Authentication failure with cached tokens fetch a new token and retry auth
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L97
        [Theory]
        [ParameterAttributeData]
        public async Task Authentication_failure_with_cached_tokens_fetch_new_and_retry([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test"); // we do not have technical ability to poison cached access token for non-mocked callbacks.

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            // have to access to the adapter directly to poison the cached access token.
            var callbackAdapter = OidcCallbackAdapterCachingFactory.Instance.Get(new OidcConfiguration(
                client.Settings.Servers.Select(s => new DnsEndPoint(s.Host, s.Port)),
                settings.Credential.Username,
                settings.Credential._mechanismProperties()));

            ConfigureOidcCallback(callbackMock, "wrong token");
            var callbackParameters = new OidcCallbackParameters(1, null);
            _ = async
                ? await callbackAdapter.GetCredentialsAsync(callbackParameters, default)
                : callbackAdapter.GetCredentials(callbackParameters, default);

            callbackMock.Reset();
            ConfigureOidcCallback(callbackMock);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Count.Should().Be(4);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 3.2 Authentication failures without cached tokens return an error
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L105
        [Theory]
        [ParameterAttributeData]
        public async Task Authentication_failure_without_cached_tokens_return_error([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock, "wrong token");
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoAuthenticationException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
        }

        // 3.3 Unexpected error code does not clear the cache
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L112
        [Theory]
        [ParameterAttributeData]
        public async Task Unexpected_error_does_not_clear_token_cache([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured();

            var (settings, _, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            Exception exception;
            using (ConfigureFailPoint(settings, 1, (int)ServerErrorCode.IllegalOperation, "saslStart"))
            {
                exception = async
                    ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                    : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

                _ = async
                    ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                    : collection.FindSync(Builders<BsonDocument>.Filter.Empty);
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());
        }

        // 4.1 Reauthentication Succeeds
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L140
        [Theory]
        [ParameterAttributeData]
        public async Task ReAuthentication([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured();

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            using (ConfigureFailPoint(settings, 1, (int)ServerErrorCode.ReauthenticationRequired, "find"))
            {
                _ = async
                    ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                    : collection.FindSync(Builders<BsonDocument>.Filter.Empty);
            }

            VerifyCallbackUsage(callbackMock, async, Times.Exactly(2));
            eventCapturer.Count.Should().Be(4);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 4.2 Read Commands Fail If Reauthentication Fails
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L165
        [Theory]
        [ParameterAttributeData]
        public async Task Read_commands_fail_if_reauthentication_fails([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            // reconfigure mock to return invalid access token
            ConfigureOidcCallback(callbackMock, "wrong token");
            Exception exception;
            using (ConfigureFailPoint(settings, 1, (int)ServerErrorCode.ReauthenticationRequired, "find"))
            {
                exception = async
                    ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                    : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
            VerifyCallbackUsage(callbackMock, async, Times.Exactly(2));
            eventCapturer.Count.Should().Be(4);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
        }

        // 4.3 Write Commands Fail If Reauthentication Fails
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L190
        [Theory]
        [ParameterAttributeData]
        public async Task Write_commands_fail_if_reauthentication_fails([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test");

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var dummyDocument = new BsonDocument("dummy", "value");
            if (async)
            {
                await collection.InsertOneAsync(dummyDocument);
            }
            else
            {
                collection.InsertOne(dummyDocument);
            }

            // reconfigure mock to return invalid access token
            ConfigureOidcCallback(callbackMock, "wrong token");
            Exception exception;
            using (ConfigureFailPoint(settings, 1, (int)ServerErrorCode.ReauthenticationRequired, "insert"))
            {
                exception = async
                    ? await Record.ExceptionAsync(() => collection.InsertOneAsync(dummyDocument))
                    : Record.Exception(() => collection.InsertOne(dummyDocument));
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
            exception.InnerException.Should().BeOfType<MongoCommandException>();
            VerifyCallbackUsage(callbackMock, async, Times.Exactly(2));
            eventCapturer.Count.Should().Be(4);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
        }

        // 4.4 Speculative Authentication should be ignored on Reauthentication
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L215
        [Theory]
        [ParameterAttributeData]
        public async Task Speculative_authentication_should_be_ignored_on_reauthentication([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("test"); // we do not have technical ability to poison cached access token for non-mocked callbacks.

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            // have to access to the adapter directly to populate the cached access token.
            var callbackAdapter = OidcCallbackAdapterCachingFactory.Instance.Get(new OidcConfiguration(
                client.Settings.Servers.Select(s => new DnsEndPoint(s.Host, s.Port)),
                settings.Credential.Username,
                settings.Credential._mechanismProperties()));

            var callbackParameters = new OidcCallbackParameters(1, null);
            _ = async
                ? await callbackAdapter.GetCredentialsAsync(callbackParameters, default)
                : callbackAdapter.GetCredentials(callbackParameters, default);
            callbackMock.Invocations.Clear();

            VerifyCallbackUsage(callbackMock, async, Times.Never());
            eventCapturer.Count.Should().Be(0);

            var dummyDocument = new BsonDocument("dummy", "value");
            if (async)
            {
                await collection.InsertOneAsync(dummyDocument);
            }
            else
            {
                collection.InsertOne(dummyDocument);
            }

            VerifyCallbackUsage(callbackMock, async, Times.Never());
            eventCapturer.Count.Should().Be(0);

            using (ConfigureFailPoint(settings, 1, (int)ServerErrorCode.ReauthenticationRequired, "insert"))
            {
                var dummyDocument2 = new BsonDocument("dummy", "value2");
                if (async)
                {
                    await collection.InsertOneAsync(dummyDocument2);
                }
                else
                {
                    collection.InsertOne(dummyDocument2);
                }
            }

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 4.5 Reauthentication Succeeds when a Session is involved
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L245
        [Theory]
        [ParameterAttributeData]
        public async Task Reauthentication_Succeeds_when_Session_involved([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured();

            var (settings, eventCapturer, callbackMock) = GetMongoClientSettings();
            ConfigureOidcCallback(callbackMock);
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            using (ConfigureFailPoint(settings, 1, (int)ServerErrorCode.ReauthenticationRequired, "find"))
            {
                var session = client.StartSession();
                _ = async
                    ? await collection.FindAsync(session, Builders<BsonDocument>.Filter.Empty)
                    : collection.FindSync(session, Builders<BsonDocument>.Filter.Empty);
            }

            VerifyCallbackUsage(callbackMock, async, Times.Exactly(2));
            eventCapturer.Count.Should().Be(4);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 5.1 Azure With No Username
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L277
        [Theory]
        [ParameterAttributeData]
        public async Task Azure_auth_no_username([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("azure");

            var (settings, eventCapturer, _) = GetMongoClientSettings();
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);
            eventCapturer.Count.Should().Be(2);
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // 5.2 Azure with Bad Username
        // https://github.com/mongodb/specifications/blob/44176c1b633819b5a070e05148a989d0c79d406d/source/auth/tests/mongodb-oidc.md?plain=1#L283
        [Theory]
        [ParameterAttributeData]
        public async Task Azure_auth_bad_username_return_error([Values(false, true)] bool async)
        {
            EnsureOidcIsConfigured("azure");

            var (settings, _, _) = GetMongoClientSettings("bad");
            using var client = DriverTestConfiguration.CreateMongoClient(settings);
            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
        }

        private void ConfigureOidcCallback(Mock<IOidcCallback> callbackMock, string accessToken = null)
        {
            if (callbackMock == null)
            {
                return;
            }

            if (accessToken == null)
            {
                var tokenPath = Path.Combine(Environment.GetEnvironmentVariable(OidcTokensDirEnvName), TokenName);
                Ensure.That(File.Exists(tokenPath), $"OIDC token {tokenPath} doesn't exist.");
                accessToken = File.ReadAllText(tokenPath);
            }

            var response = new OidcAccessToken(accessToken, null);
            callbackMock
                .Setup(c => c.GetOidcAccessToken(It.IsAny<OidcCallbackParameters>(), It.IsAny<CancellationToken>()))
                .Returns(response);
            callbackMock
                .Setup(c => c.GetOidcAccessTokenAsync(It.IsAny<OidcCallbackParameters>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
        }

        private FailPoint ConfigureFailPoint(
            MongoClientSettings settings,
            int times,
            int errorCode,
            params string[] command)
        {
            var failPointCommand = new BsonDocument
            {
                { "configureFailPoint", FailPointName.FailCommand },
                { "mode", new BsonDocument("times", times) },
                {
                    "data",
                    new BsonDocument
                    {
                        { "failCommands", new BsonArray(command.Select(c => new BsonString(c))) },
                        { "errorCode", errorCode },
                        { "appName", settings.ApplicationName }
                    }
                }
            };

            var cluster = DriverTestConfiguration.Client.GetClusterInternal();
            var server = cluster.SelectServer(OperationContext.NoTimeout, new ReadPreferenceServerSelector(ReadPreference.Primary));
            var session = NoCoreSession.NewHandle();

            return FailPoint.Configure(server, session, failPointCommand);
        }

        private (MongoClientSettings MongoClientSettings, EventCapturer EventCapturer, Mock<IOidcCallback> CustomCallback) GetMongoClientSettings(string userName = null)
        {
            Mock<IOidcCallback> callback = null;
            var oidcEnvironment = Environment.GetEnvironmentVariable(OidcEnvironmentName);

            MongoCredential credential;
            switch (oidcEnvironment)
            {
                case "test":
                    Ensure.IsNull(userName, nameof(userName));
                    callback = new Mock<IOidcCallback>();
                    credential = MongoCredential.CreateOidcCredential(callback.Object);
                    break;
                case "azure":
                case "gcp":
                    credential = MongoCredential.CreateOidcCredential(oidcEnvironment, userName)
                        .WithMechanismProperty(OidcConfiguration.TokenResourceMechanismPropertyName, Environment.GetEnvironmentVariable("TOKEN_RESOURCE"));
                    break;
                case "k8s":
                    credential = MongoCredential.CreateOidcCredential(oidcEnvironment);
                    break;
                default:
                    throw new InvalidOperationException($"Not supported OIDC_ENV value: {oidcEnvironment}");
            }

            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var settings = DriverTestConfiguration.GetClientSettings();
            settings.RetryReads = false;
            settings.RetryWrites = false;
            settings.ReadPreference = ReadPreference.Primary;
            settings.MinConnectionPoolSize = 0;
            settings.Credential = credential;
            settings.ServerMonitoringMode = ServerMonitoringMode.Poll;
            settings.HeartbeatInterval = TimeSpan.FromSeconds(30);
            settings.ClusterConfigurator = (builder) => builder.Subscribe(eventCapturer);
            settings.ApplicationName = "OIDC_Prose_tests";

            return (settings, eventCapturer, callback);
        }

        private void EnsureOidcIsConfigured(string environmentType = null)
        {
            if (environmentType == null)
            {
                RequireEnvironment.Check().EnvironmentVariable(OidcEnvironmentName);
            }
            else
            {
                RequireEnvironment.Check().EnvironmentVariable(OidcEnvironmentName, environmentType);
            }
        }

        private void VerifyCallbackUsage(Mock<IOidcCallback> callbackMock, bool async, Times times)
        {
            if (callbackMock == null)
            {
                return;
            }

            if (async)
            {
                callbackMock.Verify(x => x.GetOidcAccessToken(It.IsAny<OidcCallbackParameters>(), It.IsAny<CancellationToken>()), Times.Never());
                callbackMock.Verify(x => x.GetOidcAccessTokenAsync(It.Is<OidcCallbackParameters>(p => p.Version == 1), It.IsAny<CancellationToken>()), times);
            }
            else
            {
                callbackMock.Verify(x => x.GetOidcAccessToken(It.Is<OidcCallbackParameters>(p => p.Version == 1), It.IsAny<CancellationToken>()), times);
                callbackMock.Verify(x => x.GetOidcAccessTokenAsync(It.IsAny<OidcCallbackParameters>(), It.IsAny<CancellationToken>()), Times.Never());
            }
        }
    }
}
