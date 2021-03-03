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
using Xunit;
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Authentication
{
    public class MongoDBX509AuthenticatorTests
    {
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

        [Theory]
        [InlineData("")]
        public void Constructor_should_throw_an_ArgumentException_when_username_is_empty(string username)
        {
            Action act = () => new MongoDBX509Authenticator(username, serverApi: null);

            act.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_command_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", serverApi);

            var connection = new MockConnection(__serverId);
            var response = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ok: 1}"));
            connection.EnqueueCommandResponseMessage(response);
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

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            var actualRequestId = sentMessages[0]["requestId"].AsInt32;
            actualRequestId.Should().BeInRange(expectedRequestId, expectedRequestId + 10);

            var expectedServerApiString = useServerApi ? ", apiVersion : \"1\", apiStrict : true, apiDeprecationErrors : true" : "";
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ authenticate : 1, mechanism : \"MONGODB-X509\", user : \"CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US\", $db : \"$external\"{expectedServerApiString} }} }} ] }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_query_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", serverApi);

            var connection = new MockConnection(__serverId);
            var reply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{ok: 1}"));
            connection.EnqueueReplyMessage(reply);
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

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            var actualRequestId = sentMessages[0]["requestId"].AsInt32;
            actualRequestId.Should().BeInRange(expectedRequestId, expectedRequestId + 10);

            var expectedServerApiString = useServerApi ? ", apiVersion : \"1\", apiStrict : true, apiDeprecationErrors : true" : "";
            sentMessages[0].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId}, database : \"$external\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ authenticate : 1, mechanism : \"MONGODB-X509\", user : \"CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US\"{expectedServerApiString} }} }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", serverApi: null);

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(new SemanticVersion(3, 2, 0));
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
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", serverApi: null);

            var reply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(new SemanticVersion(3, 2, 0));
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

            act.ShouldNotThrow();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_username_is_null_and_server_does_not_support_null_username(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator(username: null, serverApi: null);

            var reply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(new SemanticVersion(3, 2, 0));
            connection.EnqueueReplyMessage(reply);

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None));
            }

            exception.Should().BeOfType<MongoConnectionException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_username_is_null_and_server_support_null_username(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator(username: null, serverApi: null);

            var reply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);
            var description = CreateConnectionDescription(new SemanticVersion(3, 4, 0));

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, description, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, description, CancellationToken.None));
            }

            exception.Should().BeNull();
        }

        // private methods
        private ConnectionDescription CreateConnectionDescription(SemanticVersion serverVersion)
        {
            var clusterId = new ClusterId(1);
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            var connectionId = new ConnectionId(serverId, 1);
            var isMasterResult = new IsMasterResult(new BsonDocument());
            var buildInfoResult = new BuildInfoResult(new BsonDocument
            {
                { "version", serverVersion.ToString() }
            });
            return new ConnectionDescription(connectionId, isMasterResult, buildInfoResult);
        }
    }
}
