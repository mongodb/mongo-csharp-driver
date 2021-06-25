﻿/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Moq;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionInitializerTests
    {
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
        private readonly ConnectionInitializer _subject;

        public ConnectionInitializerTests()
        {
            _subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: null);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateInitialIsMaster_should_return_isMaster_with_speculativeAuthenticate(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var credentials = new UsernamePasswordCredential(
                source: "Pathfinder", username: "Barclay", password: "Barclay-Alpha-1-7-Gamma");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);
            var connectionSettings = new ConnectionSettings(new[] { new AuthenticatorFactory(() => authenticator) });

            var isMasterDocument = _subject.CreateInitialIsMasterCommand(new[] { authenticator }, false);

            isMasterDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = isMasterDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(credentials.Source));
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_acquire_connectionId_from_isMaster_response([Values(false, true)] bool async)
        {
            var isMasterReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var buildInfoReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);

            ConnectionDescription result;
            if (async)
            {
                result = _subject.HandshakeAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
                result = _subject.ConnectionAuthenticationAsync(connection, result, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = _subject.Handshake(connection, CancellationToken.None);
                result = _subject.ConnectionAuthentication(connection, result, CancellationToken.None);
            }

            var sentMessages = connection.GetSentMessages();
            sentMessages.Should().HaveCount(2);
            result.ConnectionId.ServerValue.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_call_Authenticator_CustomizeInitialIsMasterCommand(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var isMasterReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var buildInfoReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }"));
            var credentials = new UsernamePasswordCredential(
                source: "Voyager", username: "Seven of Nine", password: "Omega-Phi-9-3");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);
            var connectionSettings = new ConnectionSettings(new[] { new AuthenticatorFactory(() => authenticator) });
            var connection = new MockConnection(__serverId, connectionSettings, eventSubscriber: null);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);

            // We expect authentication to fail since we have not enqueued the expected authentication replies
            try
            {
                if (async)
                {
                    var description =_subject.HandshakeAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
                    _subject.ConnectionAuthenticationAsync(connection, description, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    var description = _subject.Handshake(connection, CancellationToken.None);
                    _subject.ConnectionAuthentication(connection, description, CancellationToken.None);
                }
            }
            catch (InvalidOperationException ex)
            {
                ex.Message.Should().Be("Queue empty.");
            }

            var sentMessages = connection.GetSentMessages();
            var isMasterQuery = (QueryMessage)sentMessages[0];
            var isMasterDocument = isMasterQuery.Query;
            isMasterDocument.Should().Contain("speculativeAuthenticate");
            var speculativeAuthenticateDocument = isMasterDocument["speculativeAuthenticate"].AsBsonDocument;
            speculativeAuthenticateDocument.Should().Contain("mechanism");
            var expectedMechanism = new BsonString(
                authenticatorType == "default" ? "SCRAM-SHA-256" : authenticatorType);
            speculativeAuthenticateDocument["mechanism"].Should().Be(expectedMechanism);
            speculativeAuthenticateDocument["db"].Should().Be(new BsonString(credentials.Source));
        }

        [Theory]
        [ParameterAttributeData]
        public void Handshake_should_throw_an_ArgumentNullException_if_the_connection_is_null(
            [Values(false, true)]
            bool async)
        {
            Action act;
            if (async)
            {
                act = () => _subject.HandshakeAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.Handshake(null, CancellationToken.None);
            }

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ConnectionAuthentication_should_throw_an_ArgumentNullException_if_the_connection_is_null(
            [Values(false, true)] bool async)
        {
            var mockConnectionDescription = new ConnectionDescription(new ConnectionId(__serverId), new IsMasterResult(new BsonDocument()), new BuildInfoResult(new BsonDocument("version", "0.0.0")));
            if (async)
            {
                Record.Exception(() => _subject.ConnectionAuthenticationAsync(null, mockConnectionDescription, CancellationToken.None).GetAwaiter().GetResult()).Should().BeOfType<ArgumentNullException>();
                Record.Exception(() => _subject.ConnectionAuthenticationAsync(Mock.Of<IConnection>(), null, CancellationToken.None).GetAwaiter().GetResult()).Should().BeOfType<ArgumentNullException>();
            }
            else
            {
                Record.Exception(() => _subject.ConnectionAuthentication(null, mockConnectionDescription, CancellationToken.None)).Should().BeOfType<ArgumentNullException>();
                Record.Exception(() => _subject.ConnectionAuthentication(Mock.Of<IConnection>(), null, CancellationToken.None)).Should().BeOfType<ArgumentNullException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_send_serverApi_in_isMaster_and_buildInfo(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            var isMasterReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var buildInfoReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi);

            ConnectionDescription result;
            if (async)
            {
                result = subject.HandshakeAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
                result = subject.ConnectionAuthenticationAsync(connection, result, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.Handshake(connection, CancellationToken.None);
                result = subject.ConnectionAuthentication(connection, result, CancellationToken.None);
            }

            result.ConnectionId.ServerValue.Should().Be(1);

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;

            sentMessages[0]["opcode"].AsString.Should().Be("query");
            sentMessages[0]["query"]["isMaster"].AsInt32.Should().Be(1);
            if (useServerApi)
            {
                sentMessages[0]["query"]["apiVersion"].AsString.Should().Be("1");
                sentMessages[0]["query"]["apiStrict"].AsBoolean.Should().Be(true);
                sentMessages[0]["query"]["apiDeprecationErrors"].AsBoolean.Should().Be(true);
            }
            else
            {
                sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiVersion", out _).Should().BeFalse();
                sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiStrict", out _).Should().BeFalse();
                sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiDeprecationErrors", out _).Should().BeFalse();
            }
            var expectedServerApiBuildInfoString = useServerApi ? ", apiVersion : \"1\", apiStrict : false, apiDeprecationErrors : true" : "";
            sentMessages[1].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId1}, database : \"admin\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ buildInfo : 1{expectedServerApiBuildInfoString} }}}}");
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_build_the_ConnectionDescription_correctly(
            [Values("noop", "zlib", "snappy", "zstd")] string compressorType,
            [Values(false, true)] bool async)
        {
            var isMasterReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson($"{{ ok: 1, compression: ['{compressorType}'] }}"));
            var buildInfoReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));
            var gleReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);
            connection.EnqueueReplyMessage(gleReply);

            ConnectionDescription result;
            if (async)
            {
                result = _subject.HandshakeAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
                result = _subject.ConnectionAuthenticationAsync(connection, result, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = _subject.Handshake(connection, CancellationToken.None);
                result = _subject.ConnectionAuthentication(connection, result, CancellationToken.None);
            }

            result.ServerVersion.Should().Be(new SemanticVersion(2, 6, 3));
            result.ConnectionId.ServerValue.Should().Be(10);
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
    }

    internal static class ConnectionInitializerReflector
    {
        public static BsonDocument CreateInitialIsMasterCommand(
            this ConnectionInitializer initializer,
            IReadOnlyList<IAuthenticator> authenticators,
            bool loadBalanced) =>
                (BsonDocument)Reflector.Invoke(initializer, nameof(CreateInitialIsMasterCommand), authenticators, loadBalanced);
    }
}
