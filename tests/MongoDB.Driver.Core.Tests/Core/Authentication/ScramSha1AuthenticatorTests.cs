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
using MongoDB.TestHelpers.XunitExtensions;
using System.Linq;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Authentication
{
    public class ScramSha1AuthenticatorTests
    {
        private static readonly UsernamePasswordCredential __credential = new UsernamePasswordCredential("source", "user", "pencil");
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __descriptionCommandWireProtocol = new ConnectionDescription(
            new ConnectionId(__serverId),
            new HelloResult(
                new BsonDocument("ok", 1)
                .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                .Add("maxWireVersion", WireVersion.Server47)));

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
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values("MongoConnectionException", "MongoNotPrimaryException")] string exceptionName,
            [Values(false, true)] bool async)
        {
            var subject = new ScramSha1Authenticator(__credential, serverApi: null);

            var responseException = CoreExceptionHelper.CreateException(exceptionName);
            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(responseException);
            connection.Description = __descriptionCommandWireProtocol;

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, __descriptionCommandWireProtocol, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, __descriptionCommandWireProtocol, CancellationToken.None));
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_r_value(
            [Values(false, true)]
            bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator("fyko+d2lbbFgONRv9qkxdawL");
            var subject = new ScramSha1Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartResponse = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson("{conversationId: 1, payload: BinData(0,'cj1meWtvLWQybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done: false, ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(saslStartResponse);
            connection.Description = __descriptionCommandWireProtocol;

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __descriptionCommandWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __descriptionCommandWireProtocol, CancellationToken.None);
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

            var saslStartResponse = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson("{conversationId: 1, payload: BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done: false, ok: 1}"));
            var saslContinueResponse = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson("{conversationId: 1, payload: BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTBh'), done: true, ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(saslStartResponse);
            connection.EnqueueCommandResponseMessage(saslContinueResponse);
            connection.Description = __descriptionCommandWireProtocol;

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __descriptionCommandWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __descriptionCommandWireProtocol, CancellationToken.None);
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

            var saslStartResponse = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson("{ conversationId : 1, payload : BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done : false, ok : 1}"));
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1,
                    payload : BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTA9')," +
                $"  done : {new BsonBoolean(!useLongAuthentication)}, " +
                @"  ok : 1}"));
            var saslLastStepResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1,
                    payload : BinData(0,''),
                    done : true,
                    ok : 1 }"));

            var connection = new MockConnection(__serverId);
            var helloResult = (BsonDocument)__descriptionCommandWireProtocol.HelloResult.Wrapped.Clone();
            if (useSpeculativeAuthenticate)
            {
                helloResult.Add("speculativeAuthenticate", ((Type0CommandMessageSection<RawBsonDocument>)saslStartResponse.WrappedMessage.Sections[0]).Document);
            }

            connection.Description = new ConnectionDescription(__descriptionCommandWireProtocol.ConnectionId, new HelloResult(helloResult));

            BsonDocument helloCommand = null;
            if (useSpeculativeAuthenticate)
            {
                // Call CustomizeInitialHelloCommand so that the authenticator thinks its started to speculatively
                // authenticate
                helloCommand = subject.CustomizeInitialHelloCommand(new BsonDocument { { OppressiveLanguageConstants.LegacyHelloCommandName, 1 } });
            }
            else
            {
                connection.EnqueueCommandResponseMessage(saslStartResponse);
            }

            connection.EnqueueCommandResponseMessage(saslContinueResponse);
            if (useLongAuthentication)
            {
                connection.EnqueueCommandResponseMessage(saslLastStepResponse);
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

            var saslStartMessage = BsonDocument.Parse(@$"
            {{
                opcode : 'opmsg',
                requestId : {actualRequestIds[0]},
                responseTo : 0,
                sections : [
                {{
                    payloadType : 0,
                    document : {{
                        saslStart : 1,
                        mechanism : 'SCRAM-SHA-1',
                        payload : new BinData(0, 'biwsbj11c2VyLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdM'),
                        options : {{ skipEmptyExchange: true }},
                        '$db' : 'source'
                    }}
                }}
                ]
            }}");
            if (!useSpeculativeAuthenticate)
            {
                expectedMessages.Add(saslStartMessage);
            }

            var saslContinueMessage = BsonDocument.Parse(@$"
            {{
                opcode : 'opmsg',
                requestId : {(useSpeculativeAuthenticate ? actualRequestIds[0] : actualRequestIds[1])},
                responseTo : 0,
                sections : [
                {{
                    payloadType : 0,
                    document : {{
                        saslContinue : 1,
                        conversationId : 1,
                        payload : new BinData(0, 'Yz1iaXdzLHI9ZnlrbytkMmxiYkZnT05Sdjlxa3hkYXdMSG8rVmdrN3F2VU9LVXd1V0xJV2c0bC85U3JhR01IRUUscD1NQzJUOEJ2Ym1XUmNrRHc4b1dsNUlWZ2h3Q1k9'),
                        '$db' : 'source'
                    }}
                }}
                ]
            }}");
            expectedMessages.Add(saslContinueMessage);

            if (useLongAuthentication)
            {
                var saslOptionalFinalMessage = BsonDocument.Parse($@"
                {{
                    opcode : 'opmsg',
                    requestId : {(useSpeculativeAuthenticate ? actualRequestIds[1] : actualRequestIds[2])},
                    responseTo : 0,
                    sections : [
                    {{
                        payloadType : 0,
                        document : {{
                            saslContinue : 1,
                            conversationId : 1,
                            payload : new BinData(0, ''),
                            '$db' : 'source'
                        }}
                    }}
                    ]
                }}");
                expectedMessages.Add(saslOptionalFinalMessage);
            }

            sentMessages.Should().Equal(expectedMessages);
            if (useSpeculativeAuthenticate)
            {
                helloCommand.Should().Contain("speculativeAuthenticate");
                var speculativeAuthenticateDocument = helloCommand["speculativeAuthenticate"].AsBsonDocument;
                var expectedSpeculativeAuthenticateDocument = saslStartMessage["sections"].AsBsonArray[0]["document"].AsBsonDocument;
                var dollarsDbElement = expectedSpeculativeAuthenticateDocument.GetElement("$db");
                expectedSpeculativeAuthenticateDocument.RemoveElement(dollarsDbElement); // $db is automatically added by wireProtocol processing that can be different from db specified in authenticator
                expectedSpeculativeAuthenticateDocument.Add(new BsonElement("db", __credential.Source));
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

            var saslStartResponse = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson(
                    "{conversationId: 1, payload: BinData(0,'cj1meWtvK2QybGJiRmdPTlJ2OXFreGRhd0xIbytWZ2s3cXZVT0tVd3VXTElXZzRsLzlTcmFHTUhFRSxzPXJROVpZM01udEJldVAzRTFURFZDNHc9PSxpPTEwMDAw'), done: false, ok: 1}"));
            var saslContinueResponse = MessageHelper.BuildCommandResponse(
                RawBsonDocumentHelper.FromJson(
                    "{conversationId: 1, payload: BinData(0,'dj1VTVdlSTI1SkQxeU5ZWlJNcFo0Vkh2aFo5ZTA9'), done: true, ok: 1}"));
            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(saslStartResponse);
            connection.EnqueueCommandResponseMessage(saslContinueResponse);
            connection.Description = __descriptionCommandWireProtocol;

            if (async)
            {
                subject.AuthenticateAsync(connection, __descriptionCommandWireProtocol, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                subject.Authenticate(connection, __descriptionCommandWireProtocol, CancellationToken.None);
            }

            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5))
                .Should()
                .BeTrue();

            subject._cache().Should().NotBe(null);
            subject._cache()._cacheKey().Should().NotBe(null);
            subject._cache()._cachedEntry().Should().NotBe(null);
        }
    }
}
