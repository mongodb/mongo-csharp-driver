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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.auth
{
    [Category("Authentication", "OidcMechanism")]
    public class OidcAuthenticationProseTests : LoggableTestClass
    {
        // some auth configuration may support only this name
        private const string DatabaseName = "test";
        private const string CollectionName = "collName";
        private const string OidcTokensDirEnvName = "OIDC_TOKEN_DIR";
        private const string TokenName = "test_user1";

        public OidcAuthenticationProseTests(ITestOutputHelper output) : base(output)
        {
            EnsureOidcIsConfigured();
            OidcCallbackAdapterCachingFactory.Instance.Reset();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L41
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_authentication_callback_called_during_authentication([Values(false, true)]bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, GetAccessTokenValue());
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L48
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_authentication_callback_called_once_for_multiple_connections([Values(false, true)]bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, GetAccessTokenValue());
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var collection = CreateMongoCollection(credential);

            await ThreadingUtilities.ExecuteTasksOnNewThreads(10, async t =>
            {
                for (var i = 0; i < 100; i++)
                {
                    _ = async
                        ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                        : collection.FindSync(Builders<BsonDocument>.Filter.Empty);
                }
            });

            VerifyCallbackUsage(callbackMock, async, Times.Once());
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L57
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_valid_callback_inputs([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, GetAccessTokenValue());
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L64
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_callback_returns_null([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Events.Should().BeEmpty();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L70
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_callback_returns_missing_data([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, "wrong token");
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());

            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L77
        [Theory]
        [ParameterAttributeData]
        public async Task Callback_validation_invalid_client_configuration([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, GetAccessTokenValue());
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object)
                .WithMechanismProperty("PROVIDER_NAME", "aws");
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            VerifyCallbackUsage(callbackMock, async, Times.Never());
            eventCapturer.Events.Should().BeEmpty();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L84
        [Theory]
        [ParameterAttributeData]
        public async Task Authentication_failure_with_cached_tokens_fetch_new_and_retry([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);

            // have to access to the adapter directly to poison the cached access token.
            var callbackAdapter = OidcCallbackAdapterCachingFactory.Instance.Get(new OidcConfiguration(
                CoreTestConfiguration.ConnectionString.Hosts,
                credential.Username,
                credential._mechanismProperties(),
                OidcKnownCallbackProviders.Instance));

            ConfigureOidcCallback(callbackMock, "wrong token");
            var callbackParameters = new OidcCallbackParameters(1);
            _ = async
                ? await callbackAdapter.GetCredentialsAsync(callbackParameters, default)
                : callbackAdapter.GetCredentials(callbackParameters, default);

            // configure mock with valid access token
            ConfigureOidcCallback(callbackMock, GetAccessTokenValue());

            // callbackAdapter should have cached wrong access token at this point.
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            _ = async
                ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                : collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            VerifyCallbackUsage(callbackMock, async, Times.Once());
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L92
        [Theory]
        [ParameterAttributeData]
        public async Task Authentication_failure_without_cached_tokens_return_error([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, "wrong token");
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty))
                : Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoConnectionException>();
            VerifyCallbackUsage(callbackMock, async, Times.Once());

            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
        }

        // https://github.com/mongodb/specifications/blob/ac903bf6edb859456c1005a439efcd0769e10870/source/auth/tests/mongodb-oidc.md?plain=1#L99
        [Theory]
        [ParameterAttributeData]
        public async Task ReAuthentication([Values(false, true)] bool async)
        {
            var callbackMock = new Mock<IOidcCallback>();
            ConfigureOidcCallback(callbackMock, GetAccessTokenValue());
            var credential = MongoCredential.CreateOidcCredential(callbackMock.Object);
            var eventCapturer = new EventCapturer().CaptureCommandEvents(SaslAuthenticator.SaslStartCommand);
            var collection = CreateMongoCollection(credential, eventCapturer);

            using (ConfigureFailPoint(1, (int)ServerErrorCode.ReauthenticationRequired, "find"))
            {
                _ = async
                    ? await collection.FindAsync(Builders<BsonDocument>.Filter.Empty)
                    : collection.FindSync(Builders<BsonDocument>.Filter.Empty);
            }

            VerifyCallbackUsage(callbackMock, async, Times.Exactly(2));
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        private void ConfigureOidcCallback(Mock<IOidcCallback> callbackMock, string accessToken)
        {
            callbackMock.Reset();

            var response = new OidcAccessToken(accessToken, null);
            callbackMock
                .Setup(c => c.GetOidcAccessToken(It.IsAny<OidcCallbackParameters>(), It.IsAny<CancellationToken>()))
                .Returns(response);
            callbackMock
                .Setup(c => c.GetOidcAccessTokenAsync(It.IsAny<OidcCallbackParameters>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));
        }

        private FailPoint ConfigureFailPoint(
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
                        { "errorCode",  errorCode }
                    }
                }
            };

            var cluster = DriverTestConfiguration.Client.Cluster;
            var session = NoCoreSession.NewHandle();

            return FailPoint.Configure(cluster, session, failPointCommand);
        }

        private MongoClientSettings CreateOidcMongoClientSettings(MongoCredential credential, EventCapturer eventCapturer = null)
        {
            var settings = DriverTestConfiguration.GetClientSettings();
            settings.RetryReads = false;
            settings.RetryWrites = false;
            settings.MinConnectionPoolSize = 0;
            settings.Credential = credential;
            if (eventCapturer != null)
            {
                settings.ClusterConfigurator = (builder) => builder.Subscribe(eventCapturer);
            }

            return settings;
        }

        private IMongoCollection<BsonDocument> CreateMongoCollection(MongoCredential credential, EventCapturer eventCapturer = null)
        {
            var clientSettings = CreateOidcMongoClientSettings(credential, eventCapturer);
            var client = DriverTestConfiguration.CreateDisposableClient(clientSettings);

            var db = client.GetDatabase(DatabaseName);
            return db.GetCollection<BsonDocument>(CollectionName);
        }

        private void EnsureOidcIsConfigured() =>
            // EG also requires aws_test_secrets_role
            RequireEnvironment
                .Check()
                .EnvironmentVariable(OidcTokensDirEnvName)
                .EnvironmentVariable("OIDC_TESTS_ENABLED");

        private string GetAccessTokenValue()
        {
            var tokenPath = Path.Combine(Environment.GetEnvironmentVariable(OidcTokensDirEnvName), TokenName);
            Ensure.That(File.Exists(tokenPath), $"OIDC token {tokenPath} doesn't exist.");

            return File.ReadAllText(tokenPath);
        }

        private void VerifyCallbackUsage(Mock<IOidcCallback> callbackMock, bool async, Times times)
        {
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
