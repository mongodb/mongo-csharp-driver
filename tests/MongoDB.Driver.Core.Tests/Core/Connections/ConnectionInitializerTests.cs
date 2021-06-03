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

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionInitializerTests
    {
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));

        [Theory]
        [ParameterAttributeData]
        public void CreateInitialHelloCommand_without_server_api_should_return_legacy_hello_with_speculativeAuthenticate(
            [Values("default", "SCRAM-SHA-256", "SCRAM-SHA-1")] string authenticatorType,
            [Values(false, true)] bool async)
        {
            var credentials = new UsernamePasswordCredential(
                source: "Pathfinder", username: "Barclay", password: "Barclay-Alpha-1-7-Gamma");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: null);
            var helloDocument = subject.CreateInitialHelloCommand(new[] { authenticator });

            helloDocument.Should().Contain("isMaster");
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
            var helloDocument = subject.CreateInitialHelloCommand(new[] { authenticator });

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
        public void InitializeConnection_should_acquire_connectionId_from_hello_response([Values(false, true)] bool async)
        {
            var helloReply = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var buildInfoReply = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.9.0\" }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(helloReply);
            connection.EnqueueCommandResponseMessage(buildInfoReply);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: new ServerApi(ServerApiVersion.V1));
            ConnectionDescription result;
            if (async)
            {
                result = subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.InitializeConnection(connection, CancellationToken.None);
            }

            var sentMessages = connection.GetSentMessages();
            sentMessages.Should().HaveCount(2);
            result.ConnectionId.ServerValue.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_acquire_connectionId_from_legacy_hello_response([Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var buildInfoReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(legacyHelloReply);
            connection.EnqueueReplyMessage(buildInfoReply);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: null);
            ConnectionDescription result;
            if (async)
            {
                result = subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.InitializeConnection(connection, CancellationToken.None);
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
            var legacyHelloReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }"));
            var buildInfoReply = MessageHelper.BuildReply(
                RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }"));
            var credentials = new UsernamePasswordCredential(
                source: "Voyager", username: "Seven of Nine", password: "Omega-Phi-9-3");
            var authenticator = CreateAuthenticator(authenticatorType, credentials);
            var connectionSettings = new ConnectionSettings(new[] { new AuthenticatorFactory(() => authenticator) });
            var connection = new MockConnection(__serverId, connectionSettings, eventSubscriber: null);
            connection.EnqueueReplyMessage(legacyHelloReply);
            connection.EnqueueReplyMessage(buildInfoReply);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: null);
            // We expect authentication to fail since we have not enqueued the expected authentication replies
            try
            {
                if (async)
                {
                    subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    subject.InitializeConnection(connection, CancellationToken.None);
                }
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
        public void InitializeConnection_should_throw_an_ArgumentNullException_if_the_connection_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: null);
            Action act;
            if (async)
            {
                act = () => subject.InitializeConnectionAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.InitializeConnection(null, CancellationToken.None);
            }

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_with_serverApi_should_send_hello_and_buildInfo([Values(false, true)] bool async)
        {
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);

            var connection = new MockConnection(__serverId);
            var helloReply = RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }");
            connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(helloReply));
            var buildInfoReply = RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }");
            connection.EnqueueCommandResponseMessage(MessageHelper.BuildCommandResponse(buildInfoReply));

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi);

            ConnectionDescription result;
            if (async)
            {
                result = subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.InitializeConnection(connection, CancellationToken.None);
            }

            result.ConnectionId.ServerValue.Should().Be(1);

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;

            sentMessages[0]["opcode"].AsString.Should().Be("opmsg");
            var helloRequestDocument = sentMessages[0]["sections"][0]["document"];
            helloRequestDocument["hello"].AsInt32.Should().Be(1);
            helloRequestDocument["apiVersion"].AsString.Should().Be("1");
            helloRequestDocument["apiStrict"].AsBoolean.Should().Be(true);
            helloRequestDocument["apiDeprecationErrors"].AsBoolean.Should().Be(true);

            sentMessages[1].Should().Be($"{{ \"opcode\" : \"opmsg\", \"requestId\" : {actualRequestId1}, \"responseTo\" : 0, \"sections\" : [ {{ \"payloadType\" : 0, \"document\" : {{ \"buildInfo\" : 1, \"$db\" : \"admin\", \"$readPreference\" : {{ \"mode\" : \"primaryPreferred\" }}, \"apiVersion\" : \"1\", \"apiStrict\" : false, \"apiDeprecationErrors\" : true }} }}] }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_without_serverApi_should_send_legacy_hello_and_buildInfo([Values(false, true)] bool async)
        {
            var connection = new MockConnection(__serverId);
            var helloReply = RawBsonDocumentHelper.FromJson("{ ok : 1, connectionId : 1 }");
            connection.EnqueueReplyMessage(MessageHelper.BuildReply(helloReply));
            var buildInfoReply = RawBsonDocumentHelper.FromJson("{ ok : 1, version : \"4.2.0\" }");
            connection.EnqueueReplyMessage(MessageHelper.BuildReply(buildInfoReply));

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, null);

            ConnectionDescription result;
            if (async)
            {
                result = subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.InitializeConnection(connection, CancellationToken.None);
            }

            result.ConnectionId.ServerValue.Should().Be(1);

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;

            sentMessages[0]["opcode"].AsString.Should().Be("query");
            sentMessages[0]["query"][OppressiveLanguageConstants.LegacyHelloCommandName].AsInt32.Should().Be(1);
            sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiVersion", out _).Should().BeFalse();
            sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiStrict", out _).Should().BeFalse();
            sentMessages[0]["query"].AsBsonDocument.TryGetElement("apiDeprecationErrors", out _).Should().BeFalse();
            sentMessages[1].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId1}, database : \"admin\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ buildInfo : 1 }}}}");
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnectionA_should_build_the_ConnectionDescription_correctly(
            [Values("noop", "zlib", "snappy", "zstd")] string compressorType,
            [Values(false, true)] bool async)
        {
            var legacyHelloReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson($"{{ ok: 1, compression: ['{compressorType}'] }}"));
            var buildInfoReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));
            var gleReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(legacyHelloReply);
            connection.EnqueueReplyMessage(buildInfoReply);
            connection.EnqueueReplyMessage(gleReply);

            var subject = new ConnectionInitializer("test", new[] { new CompressorConfiguration(CompressorType.Zlib) }, serverApi: null);
            ConnectionDescription result;
            if (async)
            {
                result = subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.InitializeConnection(connection, CancellationToken.None);
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
        public static BsonDocument CreateInitialHelloCommand(
            this ConnectionInitializer initializer,
            IReadOnlyList<IAuthenticator> authenticators) =>
                (BsonDocument)Reflector.Invoke(initializer, nameof(CreateInitialHelloCommand), authenticators);
    }
}
