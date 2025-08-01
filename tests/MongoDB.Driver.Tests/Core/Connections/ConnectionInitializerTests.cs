﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Authentication.ScramSha;
using MongoDB.TestHelpers.XunitExtensions;
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
        public async Task ConnectionAuthentication_should_throw_if_operationContext_is_null(
            [Values(false, true)] bool async)
        {
            var connectionInitializerContext = new ConnectionInitializerContext(__emptyConnectionDescription, null);
            var subject = CreateSubject();
            var exception = async ?
                await Record.ExceptionAsync(() => subject.AuthenticateAsync(null, Mock.Of<IConnection>(), connectionInitializerContext)) :
                Record.Exception(() => subject.Authenticate(null, Mock.Of<IConnection>(), connectionInitializerContext));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("operationContext");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ConnectionAuthentication_should_throw_if_connection_is_null(
            [Values(false, true)] bool async)
        {
            var connectionInitializerContext = new ConnectionInitializerContext(__emptyConnectionDescription, null);
            var subject = CreateSubject();
            var exception = async ?
                await Record.ExceptionAsync(() => subject.AuthenticateAsync(OperationContext.NoTimeout, null, connectionInitializerContext)) :
                Record.Exception(() => subject.Authenticate(OperationContext.NoTimeout, null, connectionInitializerContext));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("connection");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ConnectionAuthentication_should_throw_if_connectionInitializerContext_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var exception = async ?
                await Record.ExceptionAsync(() => subject.AuthenticateAsync(OperationContext.NoTimeout, Mock.Of<IConnection>(), null)) :
                Record.Exception(() => subject.Authenticate(OperationContext.NoTimeout, Mock.Of<IConnection>(), null));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("connectionInitializerContext");
        }

        [Fact]
        public void ConnectionInitializerContext_should_throw_when_description_is_null()
        {
            var exception = Record.Exception(() => new ConnectionInitializerContext(null, Mock.Of<IAuthenticator>()));

            exception.Should().BeOfType<ArgumentNullException>()
                .Subject.ParamName.Should().Be("description");
        }

        [Fact]
        public void ConnectionInitializerContext_should_not_throw_when_authenticator_is_null()
        {
            _ = new ConnectionInitializerContext(__emptyConnectionDescription, null);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateInitialHelloCommand_should_return_expected_hello_with_speculativeAuthenticate(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(true, false)] bool withServerApi,
            [Values(true, false)] bool loadBalanced)
        {
            var identity = new MongoExternalIdentity(source: "Pathfinder", username: "Barclay");
            var evidence = new PasswordEvidence("Barclay-Alpha-1-7-Gamma");
            var authenticator = CreateAuthenticator(authenticatorType, identity, evidence);

            var subject = CreateSubject(withServerApi);
            var helloDocument = subject.CreateInitialHelloCommand(authenticator, loadBalanced);

            var expectedHelloCommand = withServerApi || loadBalanced ? "hello" : OppressiveLanguageConstants.LegacyHelloCommandName;
            helloDocument.Should().Contain(expectedHelloCommand);
            helloDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = helloDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(identity.Source));
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Handshake_should_throw_if_operationContext_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var exception = async ?
                await Record.ExceptionAsync(() => subject.SendHelloAsync(null, Mock.Of<IConnection>())) :
                Record.Exception(() => subject.SendHello(null, Mock.Of<IConnection>()));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Handshake_should_throw_if_connection_is_null(
            [Values(false, true)] bool async)
        {
            var subject = CreateSubject();
            var exception = async ?
                await Record.ExceptionAsync(() => subject.SendHelloAsync(OperationContext.NoTimeout, null)) :
                Record.Exception(() => subject.SendHello(OperationContext.NoTimeout, null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task InitializeConnection_should_acquire_connectionId_from_hello_response(
            [Values(1, int.MaxValue, (long)int.MaxValue + 1, long.MaxValue, 1d, (double)int.MaxValue+1, (double)int.MaxValue*4)] object serverConnectionId,
            [Values(false, true)] bool async)
        {
            var formattedServerConnectionId = $"{serverConnectionId}" + (serverConnectionId is double ? ".0" : "");
            var helloResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : {formattedServerConnectionId} }}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(helloResponse);

            var subject = CreateSubject(withServerApi: true);
            var result = await InitializeConnection(subject, connection, async);

            var sentMessages = connection.GetSentMessages();
            sentMessages.Should().HaveCount(1);
            result.ConnectionId.LongServerValue.ShouldBeEquivalentTo(serverConnectionId);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task InitializeConnection_should_acquire_connectionId_from_legacy_hello_response(
            [Values(1, int.MaxValue, (long)int.MaxValue + 1, long.MaxValue, 1d, (double)int.MaxValue+1, (double)int.MaxValue*4)] object serverConnectionId,
            [Values(false, true)] bool async)
        {
            var formattedServerConnectionId = $"{serverConnectionId}" + (serverConnectionId is double ? ".0" : "");
            var legacyHelloReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : {formattedServerConnectionId} }}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(legacyHelloReply);

            var subject = CreateSubject();
            var result = await InitializeConnection(subject, connection, async);

            var sentMessages = connection.GetSentMessages();
            sentMessages.Should().HaveCount(1);
            result.ConnectionId.LongServerValue.ShouldBeEquivalentTo(serverConnectionId);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task InitializeConnection_should_call_Authenticator_CustomizeInitialHelloCommand(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var identity = new MongoExternalIdentity(source: "Voyager", username: "Seven of Nine");
            var evidence = new PasswordEvidence("Omega-Phi-9-3");
            var authenticator = CreateAuthenticator(authenticatorType, identity, evidence);
            var connectionSettings = new ConnectionSettings(new AuthenticatorFactory(() => authenticator));
            var connection = new MockConnection(__serverId, connectionSettings, eventSubscriber: null);
            connection.EnqueueReplyMessage(legacyHelloReply);

            var subject = CreateSubject();
            // We expect authentication to fail since we have not enqueued the expected authentication replies
            var exception = await Record.ExceptionAsync(() => InitializeConnection(subject, connection, async));
            exception.Message.Should().Be("Queue empty.");

            var sentMessages = connection.GetSentMessages();
            var legacyHelloQuery = (QueryMessage)sentMessages[0];
            var legacyHelloDocument = legacyHelloQuery.Query;
            legacyHelloDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = legacyHelloDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(identity.Source));
        }


        [Theory]
        [ParameterAttributeData]
        public async Task InitializeConnection_with_serverApi_should_send_hello([Values(false, true)] bool async)
        {
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);

            var connection = new MockConnection(__serverId);
            var helloReply = RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : 1, maxWireVersion : {WireVersion.Server42} }}");
            connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(helloReply));

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi, null);

            var result = await InitializeConnection(subject, connection, async);

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
        public async Task InitializeConnection_without_serverApi_should_send_legacy_hello([Values(false, true)] bool async)
        {
            var connection = new MockConnection(__serverId);
            var helloReply = RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : 1, maxWireVersion : {WireVersion.Server42} }}");
            connection.EnqueueReplyMessage(MessageHelper.BuildReply(helloReply));

            var subject = CreateSubject();

            var result = await InitializeConnection(subject, connection, async);

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
        public async Task InitializeConnection_without_serverApi_but_with_loadBalancing_should_send_hello([Values(false, true)] bool async)
        {
            var connection = new MockConnection(__serverId, new ConnectionSettings(loadBalanced:true), null);
            var helloReply = RawBsonDocumentHelper.FromJson($"{{ ok : 1, connectionId : 1, maxWireVersion : {WireVersion.Server42}, serviceId : '{ObjectId.GenerateNewId()}' }}");
            connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(helloReply));

            var subject = CreateSubject();

            var result = await InitializeConnection(subject, connection, async);

            result.ConnectionId.LongServerValue.Should().Be(1);

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            sentMessages[0]["opcode"].AsString.Should().Be("opmsg");
            var helloRequestDocument = sentMessages[0]["sections"][0]["document"];
            helloRequestDocument["hello"].AsInt32.Should().Be(1);
            helloRequestDocument.AsBsonDocument.TryGetElement("apiVersion", out _).Should().BeFalse();
            helloRequestDocument.AsBsonDocument.TryGetElement("apiStrict", out _).Should().BeFalse();
            helloRequestDocument.AsBsonDocument.TryGetElement("apiDeprecationErrors", out _).Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task InitializeConnection_should_build_the_ConnectionDescription_correctly(
            [Values("noop", "zlib", "snappy", "zstd")] string compressorType,
            [Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson($"{{ ok : 1, compression : ['{compressorType}'], maxWireVersion : {WireVersion.Server36} }}"));
            var gleReply = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(legacyHelloReply);
            connection.EnqueueCommandResponseMessage(gleReply);

            var subject = CreateSubject();
            var result = await InitializeConnection(subject, connection, async);

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

        [Theory]
        [ParameterAttributeData]
        public async Task InitializeConnection_should_switch_command_wire_protocol_after_handshake_if_OP_MSG_is_supported(
            [Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson(
                    $"{{ ok : 1, connectionId : 1, maxWireVersion : {WireVersion.Server42} }}"));
            var identity = new MongoExternalIdentity(source: "Voyager", username: "Seven of Nine");
            var evidence = new PasswordEvidence("Omega-Phi-9-3");
            var authenticator = CreateAuthenticator("default", identity, evidence);
            var connectionSettings = new ConnectionSettings(new AuthenticatorFactory(() => authenticator));
            var connection = new MockConnection(__serverId, connectionSettings, eventSubscriber: null);
            connection.EnqueueReplyMessage(legacyHelloReply);

            var subject = CreateSubject();
            // We expect authentication to fail since we have not enqueued the expected authentication replies
            var exception = await Record.ExceptionAsync(() => InitializeConnection(subject, connection, async));
            exception.Message.Should().Be("Queue empty.");


            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            sentMessages[0]["opcode"].AsString.Should().Be("query");
            sentMessages[1]["opcode"].AsString.Should().Be("opmsg");
        }

        // private methods
        private IAuthenticator CreateAuthenticator(string authenticatorType, MongoIdentity identity, MongoIdentityEvidence evidence)
        {
            var saslContext = CreateSaslContext(authenticatorType, identity, evidence);
            switch (authenticatorType)
            {
                case "SCRAM-SHA-1":
                    return new SaslAuthenticator(ScramShaSaslMechanism.CreateScramSha1Mechanism(saslContext), null);
                case "SCRAM-SHA-256":
                    return new SaslAuthenticator(ScramShaSaslMechanism.CreateScramSha256Mechanism(saslContext), null);
                case "default":
                    return new DefaultAuthenticator(identity, evidence, [__serverId.EndPoint], null);
                default:
                    throw new Exception("Invalid authenticator type.");
            }
        }

        private SaslContext CreateSaslContext(string authenticatorType, MongoIdentity identity, MongoIdentityEvidence evidence)
            => new()
            {
                Mechanism = authenticatorType,
                Identity = identity,
                IdentityEvidence = evidence,
                MechanismProperties = null,
                EndPoint = __serverId.EndPoint,
                ClusterEndPoints = [__serverId.EndPoint]
            };

        private ConnectionInitializer CreateSubject(bool withServerApi = false) =>
            new ConnectionInitializer(
                "test",
                new[] { new CompressorConfiguration(CompressorType.Zlib) },
                serverApi: withServerApi ? new ServerApi(ServerApiVersion.V1) : null,
                libraryInfo: null);

        private async Task<ConnectionDescription> InitializeConnection(ConnectionInitializer connectionInitializer, MockConnection connection, bool async)
        {
            ConnectionInitializerContext connectionInitializerContext;
            if (async)
            {
                connectionInitializerContext = await connectionInitializer.SendHelloAsync(OperationContext.NoTimeout, connection);
                connection.Description = connectionInitializerContext.Description;
                connectionInitializerContext = await connectionInitializer.AuthenticateAsync(OperationContext.NoTimeout, connection, connectionInitializerContext);
                return connectionInitializerContext.Description;
            }
            else
            {
                connectionInitializerContext = connectionInitializer.SendHello(OperationContext.NoTimeout, connection);
                connection.Description = connectionInitializerContext.Description;
                connectionInitializerContext = connectionInitializer.Authenticate(OperationContext.NoTimeout, connection, connectionInitializerContext);
                return connectionInitializerContext.Description;
            }
        }
    }

    internal static class ConnectionInitializerReflector
    {
        public static BsonDocument CreateInitialHelloCommand(
            this ConnectionInitializer initializer,
            IAuthenticator authenticator,
            bool loadBalanced) =>
                (BsonDocument)Reflector.Invoke(initializer, nameof(CreateInitialHelloCommand), OperationContext.NoTimeout, authenticator, loadBalanced);
    }
}
