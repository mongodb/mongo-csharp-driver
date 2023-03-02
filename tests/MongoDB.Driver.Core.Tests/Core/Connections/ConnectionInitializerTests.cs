/* Copyright 2013-present MongoDB Inc.
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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionInitializerTests
    {
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __emptyConnectionDescription = new ConnectionDescription(new ConnectionId(__serverId), new HelloResult(new BsonDocument()));

        [Theory]
        [ParameterAttributeData]
        public void ConnectionAuthentication_should_throw_an_ArgumentNullException_if_required_arguments_missed(
            [Values(false, true)] bool async)
        {
            var connectionInitializerContext = new ConnectionInitializerContext(__emptyConnectionDescription, new IAuthenticator[0]);
            var subject = CreateSubject();
            if (async)
            {
                Record.Exception(() => subject.AuthenticateAsync(null, connectionInitializerContext, CancellationToken.None).GetAwaiter().GetResult()).Should().BeOfType<ArgumentNullException>();
                Record.Exception(() => subject.AuthenticateAsync(Mock.Of<IConnection>(), null, CancellationToken.None).GetAwaiter().GetResult()).Should().BeOfType<ArgumentNullException>();
            }
            else
            {
                Record.Exception(() => subject.Authenticate(null, connectionInitializerContext, CancellationToken.None)).Should().BeOfType<ArgumentNullException>();
                Record.Exception(() => subject.Authenticate(Mock.Of<IConnection>(), null, CancellationToken.None)).Should().BeOfType<ArgumentNullException>();
            }
        }

        [Fact]
        public void ConnectionInitializerContext_should_throw_when_arguments_are_null()
        {
            var emptyAuthenticators = new IAuthenticator[0];

            Record.Exception(() => new ConnectionInitializerContext(null, emptyAuthenticators)).Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("description");
            Record.Exception(() => new ConnectionInitializerContext(__emptyConnectionDescription, null)).Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("authenticators");
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateInitialHelloCommand_without_server_api_should_return_legacy_hello_with_speculativeAuthenticate(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var credentials = new UsernamePasswordCredential(
                source: "Pathfinder", username: "Barclay", password: "Barclay-Alpha-1-7-Gamma");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);

            var subject = CreateSubject();
            var helloDocument = subject.CreateInitialHelloCommand(new[] { authenticator }, false);

            helloDocument.Should().Contain(OppressiveLanguageConstants.LegacyHelloCommandName);
            helloDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = helloDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(credentials.Source));
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateInitialHelloCommand_with_server_api_should_return_hello_with_speculativeAuthenticate(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var credentials = new UsernamePasswordCredential(
                source: "Pathfinder", username: "Barclay", password: "Barclay-Alpha-1-7-Gamma");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: new ServerApi(ServerApiVersion.V1));
            var helloDocument = subject.CreateInitialHelloCommand(new[] { authenticator }, false);

            helloDocument.Should().Contain("hello");
            helloDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = helloDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(credentials.Source));
        }

        [Theory]
        [ParameterAttributeData]
        public void Handshake_should_throw_an_ArgumentNullException_if_the_connection_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var exception = async
                ? Record.Exception(() => subject.SendHelloAsync(null, CancellationToken.None).GetAwaiter().GetResult())
                : Record.Exception(() => subject.SendHello(null, CancellationToken.None));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_acquire_connectionId_from_hello_response([Values(false, true)] bool async)
        {
            var helloResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(helloResponse);

            var subject = CreateSubject(withServerApi: true);
            var result = InitializeConnection(subject, connection, async, CancellationToken.None);

            var sentMessages = connection.GetSentMessages();
            sentMessages.Should().HaveCount(1);
            result.ConnectionId.LongServerValue.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_acquire_connectionId_from_legacy_hello_response([Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(legacyHelloReply);

            var subject = CreateSubject();
            var result = InitializeConnection(subject, connection, async, CancellationToken.None);

            var sentMessages = connection.GetSentMessages();
            sentMessages.Should().HaveCount(1);
            result.ConnectionId.LongServerValue.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_call_Authenticator_CustomizeInitialHelloCommand(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var credentials = new UsernamePasswordCredential(
                source: "Voyager", username: "Seven of Nine", password: "Omega-Phi-9-3");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);
            var connectionSettings = new ConnectionSettings(new[] { new AuthenticatorFactory(() => authenticator) });
            var connection = new MockConnection(__serverId, connectionSettings, eventSubscriber: null);
            connection.EnqueueReplyMessage(legacyHelloReply);

            var subject = CreateSubject();
            // We expect authentication to fail since we have not enqueued the expected authentication replies
            try
            {
                _ = InitializeConnection(subject, connection, async, CancellationToken.None);
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.Should().Be("Queue empty.");
            }

            var sentMessages = connection.GetSentMessages();
            var legacyHelloQuery = (QueryMessage)sentMessages[0];
            var legacyHelloDocument = legacyHelloQuery.Query;
            legacyHelloDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = legacyHelloDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(credentials.Source));
        }


        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_with_serverApi_should_send_hello([Values(false, true)] bool async)
        {
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);

            var connection = new MockConnection(__serverId);
            var helloReply = RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : 1, maxWireVersion : {WireVersion.Server42} }}");
            connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(helloReply));

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi);

            var result = InitializeConnection(subject, connection, async, CancellationToken.None);

            result.ConnectionId.LongServerValue.Should().Be(1);

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            sentMessages[0]["opcode"].AsString.Should().Be("opmsg");
            var helloRequestDocument = sentMessages[0]["sections"][0]["document"];
            helloRequestDocument["hello"].AsInt32.Should().Be(1);
            helloRequestDocument["apiVersion"].AsString.Should().Be("1");
            helloRequestDocument["apiStrict"].AsBoolean.Should().Be(true);
            helloRequestDocument["apiDeprecationErrors"].AsBoolean.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_without_serverApi_should_send_legacy_hello([Values(false, true)] bool async)
        {
            var connection = new MockConnection(__serverId);
            var helloReply = RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : 1, maxWireVersion : {WireVersion.Server42} }}");
            connection.EnqueueReplyMessage(MessageHelper.BuildReply(helloReply));

            var subject = CreateSubject();

            var result = InitializeConnection(subject, connection, async, CancellationToken.None);

            result.ConnectionId.LongServerValue.Should().Be(1);

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            sentMessages[0]["opcode"].AsString.Should().Be("query");
            sentMessages[0]["query"][OppressiveLanguageConstants.LegacyHelloCommandName].AsInt32.Should().Be(1);
            sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiVersion", out _).Should().BeFalse();
            sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiStrict", out _).Should().BeFalse();
            sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiDeprecationErrors", out _).Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_build_the_ConnectionDescription_correctly(
            [Values("noop", "zlib", "snappy", "zstd")] string compressorType,
            [Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson($"{{ ok : 1, compression : ['{compressorType}'], maxWireVersion : {WireVersion.Server36} }}"));
            var gleReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(legacyHelloReply);
            connection.EnqueueReplyMessage(gleReply);

            var subject = CreateSubject();
            var result = InitializeConnection(subject, connection, async, CancellationToken.None);

            result.MaxWireVersion.Should().Be(6);
            result.ConnectionId.LongServerValue.Should().Be(10);
            result.AvailableCompressors.Count.Should().Be(1);
            result.AvailableCompressors.Should().Contain(ToCompressorTypeEnum(compressorType));

            CompressorType ToCompressorTypeEnum(string ct)
            {
                switch (ct)
                {
                    case "noop": return CompressorType.Noop;
                    case "zlib": return CompressorType.Zlib;
                    case "snappy": return CompressorType.Snappy;
                    case "zstd": return CompressorType.ZStandard;
                    default:
                        throw new InvalidOperationException($"Unexpected compression {compressorType}.");
                }
            }
        }

        // private methods
        private IAuthenticator CreateAuthenticator(string authenticatorType, UsernamePasswordCredential credentials)
        {
            switch (authenticatorType)
            {
                case "SCRAM-SHA-1":
                    return new ScramSha1Authenticator(credentials, serverApi: null);
                case "SCRAM-SHA-256":
                    return new ScramSha256Authenticator(credentials, serverApi: null);
                case "default":
                    return new DefaultAuthenticator(credentials, serverApi: null);
                default:
                    throw new Exception("Invalid authenticator type.");
            }
        }

        private ConnectionInitializer CreateSubject(bool withServerApi = false) =>
            new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: withServerApi ? new ServerApi(ServerApiVersion.V1) : null);

        private ConnectionDescription InitializeConnection(ConnectionInitializer connectionInitializer, IConnection connection, bool async, CancellationToken cancellationToken)
        {
            ConnectionInitializerContext connectionInitializerContext;
            if (async)
            {
                connectionInitializerContext = connectionInitializer.SendHelloAsync(connection, cancellationToken).GetAwaiter().GetResult();
                return connectionInitializer.AuthenticateAsync(connection, connectionInitializerContext, cancellationToken).GetAwaiter().GetResult();
            }
            else
            {
                connectionInitializerContext = connectionInitializer.SendHello(connection, cancellationToken);
                return connectionInitializer.Authenticate(connection, connectionInitializerContext, cancellationToken);
            }
        }
    }

    internal static class ConnectionInitializerReflector
    {
        public static BsonDocument CreateInitialHelloCommand(
            this ConnectionInitializer initializer,
            IReadOnlyList<IAuthenticator> authenticators,
            bool loadBalanced) =>
                (BsonDocument)Reflector.Invoke(initializer, nameof(CreateInitialHelloCommand), authenticators, loadBalanced);
    }
}
