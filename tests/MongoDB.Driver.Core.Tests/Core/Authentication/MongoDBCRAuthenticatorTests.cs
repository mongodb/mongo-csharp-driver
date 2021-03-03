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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Xunit;
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Core.Authentication
{
    public class MongoDBXCRAuthenticatorTests
    {
        private static readonly UsernamePasswordCredential __credential = new UsernamePasswordCredential("source", "user", "pencil");
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __descriptionCommandWireProtocol = new ConnectionDescription(
            new ConnectionId(__serverId),
            new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
            new BuildInfoResult(new BsonDocument("version", "4.7.0")));
        private static readonly ConnectionDescription __descriptionQueryWireProtocol = new ConnectionDescription(
            new ConnectionId(__serverId),
            new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
            new BuildInfoResult(new BsonDocument("version", "2.6.0")));

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_credential_is_null()
        {
#pragma warning disable 618
            Action act = () => new MongoDBCRAuthenticator(null);
#pragma warning restore 618

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)]
            bool async)
        {
#pragma warning disable 618
            var subject = new MongoDBCRAuthenticator(__credential);
#pragma warning restore 618

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None);
            }

            act.ShouldThrow<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_authentication_succeeds(
            [Values(false, true)]
            bool async)
        {
#pragma warning disable 618
            var subject = new MongoDBCRAuthenticator(__credential);
#pragma warning restore 618

            var getNonceReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{nonce: \"2375531c32080ae8\", ok: 1}"));
            var authenticateReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(getNonceReply);
            connection.EnqueueReplyMessage(authenticateReply);

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None);
            }

            act.ShouldNotThrow();
            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId0 = sentMessages[0]["requestId"].AsInt32;
            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;
            actualRequestId0.Should().BeInRange(expectedRequestId, expectedRequestId + 10);
            actualRequestId1.Should().BeInRange(actualRequestId0 + 1, actualRequestId0 + 11);

            sentMessages[0].Should().Be("{opcode: \"query\", requestId: " + actualRequestId0 + ", database: \"source\", collection: \"$cmd\", batchSize: -1, slaveOk: true, query: {getnonce: 1}}");
            sentMessages[1].Should().Be("{opcode: \"query\", requestId: " + actualRequestId1 + ", database: \"source\", collection: \"$cmd\", batchSize: -1, slaveOk: true, query: {authenticate: 1, user: \"user\", nonce: \"2375531c32080ae8\", key: \"21742f26431831d5cfca035a08c5bdf6\"}}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_command_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

#pragma warning disable 618
            var subject = new MongoDBCRAuthenticator(__credential, serverApi);
#pragma warning restore 618

            var connection = new MockConnection(__serverId);
            var getNonceResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{nonce: \"2375531c32080ae8\", ok: 1}"));
            var authenticateResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ok: 1}"));
            connection.EnqueueCommandResponseMessage(getNonceResponse);
            connection.EnqueueCommandResponseMessage(authenticateResponse);
            connection.Description = __descriptionCommandWireProtocol;

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            if (async)
            {
                subject.AuthenticateAsync(connection, __descriptionCommandWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Authenticate(connection, __descriptionCommandWireProtocol, CancellationToken.None);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId0 = sentMessages[0]["requestId"].AsInt32;
            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;
            actualRequestId0.Should().BeInRange(expectedRequestId, expectedRequestId + 10);
            actualRequestId1.Should().BeInRange(actualRequestId0 + 1, actualRequestId0 + 11);

            var expectedServerApiString = useServerApi ? ", apiVersion : \"1\", apiStrict : true, apiDeprecationErrors : true" : "";
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId0}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ getnonce : 1, $db : \"source\"{expectedServerApiString} }} }} ] }}");
            sentMessages[1].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId1}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ authenticate : 1, user : \"user\", nonce : \"2375531c32080ae8\", key : \"21742f26431831d5cfca035a08c5bdf6\", $db : \"source\"{expectedServerApiString} }} }} ] }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_query_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

#pragma warning disable 618
            var subject = new MongoDBCRAuthenticator(__credential, serverApi);
#pragma warning restore 618

            var connection = new MockConnection(__serverId);
            var getNonceReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{nonce: \"2375531c32080ae8\", ok: 1}"));
            var authenticateReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{ok: 1}"));
            connection.EnqueueReplyMessage(getNonceReply);
            connection.EnqueueReplyMessage(authenticateReply);
            connection.Description = __descriptionQueryWireProtocol;

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            if (async)
            {
                subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId0 = sentMessages[0]["requestId"].AsInt32;
            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;
            actualRequestId0.Should().BeInRange(expectedRequestId, expectedRequestId + 10);
            actualRequestId1.Should().BeInRange(actualRequestId0 + 1, actualRequestId0 + 11);

            var expectedServerApiString = useServerApi ? ", apiVersion : \"1\", apiStrict : true, apiDeprecationErrors : true" : "";
            sentMessages[0].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId0}, database : \"source\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ getnonce : 1{expectedServerApiString} }} }}");
            sentMessages[1].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId1}, database : \"source\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ authenticate : 1, user : \"user\", nonce : \"2375531c32080ae8\", key : \"21742f26431831d5cfca035a08c5bdf6\"{expectedServerApiString} }} }}");
        }
    }
}
