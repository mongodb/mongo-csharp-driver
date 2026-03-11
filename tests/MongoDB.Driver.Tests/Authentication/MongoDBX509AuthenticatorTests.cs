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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Authentication
{
    public class MongoDBX509AuthenticatorTests
    {
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __descriptionCommandWireProtocol = new ConnectionDescription(
            new ConnectionId(__serverId),
            new HelloResult(
                new BsonDocument("ok", 1)
                .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                .Add("maxWireVersion", WireVersion.Server47)));

        [Fact]
        public void Constructor_should_throw_an_ArgumentException_when_username_is_empty()
        {
            var exception = Record.Exception(() => new MongoDBX509Authenticator("", serverApi: null));

            exception.Should().BeOfType<ArgumentException>().Subject
                .ParamName.Should().Be("username");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_send_serverApi_with_command_wire_protocol(
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
                await subject.AuthenticateAsync(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol);
            }
            else
            {
                subject.Authenticate(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol);
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
        public async Task Authenticate_with_loadBalancedConnection_should_use_command_wire_protocol(
            [Values(false, true)] bool async)
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", null);

            var connection = new MockConnection(__serverId, new ConnectionSettings(loadBalanced: true), null);
            var response = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ok: 1}"));
            connection.EnqueueCommandResponseMessage(response);
            connection.Description = null;

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            if (async)
            {
                await subject.AuthenticateAsync(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol);
            }
            else
            {
                subject.Authenticate(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(1);

            var actualRequestId = sentMessages[0]["requestId"].AsInt32;
            actualRequestId.Should().BeInRange(expectedRequestId, expectedRequestId + 10);

            var expectedEndString = ", \"$readPreference\" : { \"mode\" : \"primaryPreferred\" }";
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ authenticate : 1, mechanism : \"MONGODB-X509\", user : \"CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US\", $db : \"$external\"{expectedEndString} }} }} ] }}");
        }


        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", serverApi: null);

            var response = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ }"));
            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(maxWireVersion: WireVersion.Server36);
            connection.EnqueueCommandResponseMessage(response);

            var exception = async ?
                await Record.ExceptionAsync(() => subject.AuthenticateAsync(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol)) :
                Record.Exception(() => subject.Authenticate(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol));

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_not_throw_when_authentication_succeeds(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US", serverApi: null);

            var response = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(maxWireVersion: WireVersion.Server36);
            connection.EnqueueCommandResponseMessage(response);

            var exception = async ?
                await Record.ExceptionAsync(() => subject.AuthenticateAsync(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol)) :
                Record.Exception(() => subject.Authenticate(OperationContext.NoTimeout, connection, __descriptionCommandWireProtocol));

            exception.Should().BeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_not_throw_when_username_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator(username: null, serverApi: null);

            var response = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(response);
            var description = CreateConnectionDescription(maxWireVersion: WireVersion.Server36);
            connection.Description = description;

            var exception = async ?
                await Record.ExceptionAsync(() => subject.AuthenticateAsync(OperationContext.NoTimeout, connection, description)) :
                Record.Exception(() => subject.Authenticate(OperationContext.NoTimeout, connection, description));

            exception.Should().BeNull();
        }

        // private methods
        private ConnectionDescription CreateConnectionDescription(int maxWireVersion)
        {
            var clusterId = new ClusterId(1);
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            var connectionId = new ConnectionId(serverId, 1);
            var helloResult = new HelloResult(new BsonDocument("maxWireVersion", maxWireVersion));
            return new ConnectionDescription(connectionId, helloResult);
        }
    }
}
