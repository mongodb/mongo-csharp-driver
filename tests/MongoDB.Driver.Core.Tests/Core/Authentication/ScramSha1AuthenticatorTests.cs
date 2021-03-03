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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using Xunit;
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using System.Linq;

namespace MongoDB.Driver.Core.Authentication
{
    public class ScramSha1AuthenticatorTests
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
            Action act = () => new ScramSha1Authenticator(credential: null, serverApi: null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_command_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");

            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi);

            var connection = new MockConnection(__serverId);
            var saslStartResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ conversationId : 1, payload : BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done : false, ok : 1 }"));
            connection.EnqueueCommandResponseMessage(saslStartResponse);
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson("{ conversationId : 1, payload : BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTA9'), done : true, ok : 1}"));
            connection.EnqueueCommandResponseMessage(saslContinueResponse);
            connection.Description = __descriptionCommandWireProtocol;

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
            var expectedServerApiString = useServerApi ? ", apiVersion : \"1\", apiStrict : true, apiDeprecationErrors : true" : "";
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId0}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ saslStart : 1, mechanism : \"SCRAM-SHA-1\", payload : new BinData(0, \"biwsbj11c2VyLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdM\"), options : {{ \"skipEmptyExchange\" : true }}, $db : \"source\"{expectedServerApiString} }} }} ] }}");
            sentMessages[1].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId1}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ saslContinue : 1, conversationId : 1, payload : new BinData(0, \"Yz1iaXdzLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdMSG8rVmdrN3F2VU9LVXd1V0xJV2c0bC85U3JhR01IRUUscD1NQzJUOEJ2Ym1XUmNrRHc4b1dsNUlWZ2h3Q1k9\"), $db : \"source\"{expectedServerApiString} }} }} ] }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_query_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");

            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi);

            var connection = new MockConnection(__serverId);
            var saslStartReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{ conversationId : 1, payload : BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done : false, ok : 1 }"));
            connection.EnqueueReplyMessage(saslStartReply);
            var saslContinueReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson("{ conversationId : 1, payload : BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTA9'), done : true, ok : 1}"));
            connection.EnqueueReplyMessage(saslContinueReply);
            connection.Description = __descriptionQueryWireProtocol;

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
            var expectedServerApiString = useServerApi ? ", apiVersion : \"1\", apiStrict : true, apiDeprecationErrors : true" : "";
            sentMessages[0].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId0}, database : \"source\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ saslStart : 1, mechanism : \"SCRAM-SHA-1\", payload : new BinData(0, \"biwsbj11c2VyLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdM\"), options : {{ \"skipEmptyExchange\" : true }}{expectedServerApiString} }} }}");
            sentMessages[1].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId1}, database : \"source\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ saslContinue : 1, conversationId : 1, payload : new BinData(0, \"Yz1iaXdzLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdMSG8rVmdrN3F2VU9LVXd1V0xJV2c0bC85U3JhR01IRUUscD1NQzJUOEJ2Ym1XUmNrRHc4b1dsNUlWZ2h3Q1k9\"){expectedServerApiString} }} }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)]
            bool async)
        {
            var subject = new ScramSha1Authenticator(__credential, serverApi: null);

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
        public void Authenticate_should_throw_when_server_provides_invalid_r_value(
            [Values(false, true)]
            bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");
            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{conversationId: 1, payload: BinData(0,'cj1meWtvLWQybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done: false, ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);

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
        public void Authenticate_should_throw_when_server_provides_invalid_serverSignature(
            [Values(false, true)]
            bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");
            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{conversationId: 1, payload: BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done: false, ok: 1}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{conversationId: 1, payload: BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTBh'), done: true, ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

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
            [Values(false, true)] bool useSpeculativeAuthenticate,
            [Values(false, true)] bool useLongAuthentication,
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");
            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ conversationId : 1, payload : BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done : false, ok : 1}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1,
                    payload : BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTA9')," +
                $"  done : {new BsonBoolean(!useLongAuthentication)}, " +
                @"  ok : 1}"));
            var saslLastStepReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1,
                    payload : BinData(0,''),
                    done : true,
                    ok : 1 }"));

            var connection = new MockConnection(__serverId);
            var isMasterResult = (BsonDocument)__descriptionQueryWireProtocol.IsMasterResult.Wrapped.Clone();
            if (useSpeculativeAuthenticate)
            {
                isMasterResult.Add("speculativeAuthenticate", saslStartReply.Documents[0].ToBsonDocument());
            }

            /* set buildInfoResult to 3.4 to force authenticator to use Query Message Wire Protocol because MockConnection
             * does not support OP_MSG */
            connection.Description = new ConnectionDescription(
                __descriptionQueryWireProtocol.ConnectionId, new IsMasterResult(isMasterResult), new BuildInfoResult(new BsonDocument("version", "3.4")));

            BsonDocument isMasterCommand = null;
            if (useSpeculativeAuthenticate)
            {
                // Call CustomizeIsMasterCommand so that the authenticator thinks its started to speculatively
                // authenticate
                isMasterCommand = subject.CustomizeInitialIsMasterCommand(new BsonDocument { { "isMaster", 1 } });
            }
            else
            {
                connection.EnqueueReplyMessage(saslStartReply);
            }

            connection.EnqueueReplyMessage(saslContinueReply);
            if (useLongAuthentication)
            {
                connection.EnqueueReplyMessage(saslLastStepReply);
            }

            Exception exception;
            if (async)
            {
                exception = Record.Exception(
                    () => subject.AuthenticateAsync(connection, connection.Description, CancellationToken.None)
                        .GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(
                    () => subject.Authenticate(connection, connection.Description, CancellationToken.None));
            }

            exception.Should().BeNull();
            var expectedSentMessageCount = 3 - (useLongAuthentication ? 0 : 1) - (useSpeculativeAuthenticate ? 1 : 0);
            SpinWait.SpinUntil(
                () => connection.GetSentMessages().Count >= expectedSentMessageCount,
                TimeSpan.FromSeconds(5)
                ).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(expectedSentMessageCount);

            var actualRequestIds = sentMessages.Select(m => m["requestId"].AsInt32).ToList();

            var expectedMessages = new List<BsonDocument>();

            var saslStartMessage = BsonDocument.Parse(
                @"{ opcode : 'query'," +
                $"  requestId : {actualRequestIds[0]}, " +
                @"  database : 'source',
                    collection : '$cmd',
                    batchSize : -1,
                    slaveOk : true,
                    query : { saslStart : 1,
                             mechanism : 'SCRAM-SHA-1'," +
                $"           payload : new BinData(0, 'biwsbj11c2VyLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdM')" +
                @"           options : { skipEmptyExchange: true }}}");
            if (!useSpeculativeAuthenticate)
            {
                expectedMessages.Add(saslStartMessage);
            }

            var saslContinueMessage = BsonDocument.Parse(
                @"{ opcode : 'query'," +
                $"  requestId : {(useSpeculativeAuthenticate ? actualRequestIds[0] : actualRequestIds[1])}," +
                @"  database : 'source',
                    collection : '$cmd',
                    batchSize : -1,
                    slaveOk : true,
                    query : { saslContinue : 1,
                             conversationId : 1, " +
                $"           payload : new BinData(0, 'Yz1iaXdzLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdMSG8rVmdrN3F2VU9LVXd1V0xJV2c0bC85U3JhR01IRUUscD1NQzJUOEJ2Ym1XUmNrRHc4b1dsNUlWZ2h3Q1k9')}}}}");
            expectedMessages.Add(saslContinueMessage);

            if (useLongAuthentication)
            {
                var saslOptionalFinalMessage = BsonDocument.Parse(
                     @"{opcode : 'query'," +
                     $" requestId : {(useSpeculativeAuthenticate ? actualRequestIds[1] : actualRequestIds[2])}," +
                     @" database : 'source',
                        collection : '$cmd',
                        batchSize : -1,
                        slaveOk : true,
                        query : { saslContinue : 1,
                                 conversationId : 1, " +
                     $"          payload : new BinData(0, '')}}}}");
                expectedMessages.Add(saslOptionalFinalMessage);
            }

            sentMessages.Should().Equal(expectedMessages);
            if (useSpeculativeAuthenticate)
            {
                isMasterCommand.Should().Contain("speculativeAuthenticate");
                var speculativeAuthenticateDocument = isMasterCommand["speculativeAuthenticate"].AsBsonDocument;
                var expectedSpeculativeAuthenticateDocument =
                    saslStartMessage["query"].AsBsonDocument.Add("db", __credential.Source);
                speculativeAuthenticateDocument.Should().Be(expectedSpeculativeAuthenticateDocument);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_use_cache(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");
            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson(
                    "{conversationId: 1, payload: BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done: false, ok: 1}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson(
                    "{conversationId: 1, payload: BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTA9'), done: true, ok: 1}"));
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

            if (async)
            {
                subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter()
                    .GetResult();
            }
            else
            {
                subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should()
                .BeTrue();

            subject._cache().Should().NotBe(null);
            subject._cache()._cacheKey().Should().NotBe(null);
            subject._cache()._cachedEntry().Should().NotBe(null);
        }
    }
}
