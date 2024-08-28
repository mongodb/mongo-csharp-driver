/* Copyright 2018–present MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Authentication.ScramSha;
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
    public class ScramSha256AuthenticatorTests
    {
        // private constants
        private const string _clientNonce = "rOprNGfwEbeRWgbNEkqO";
        private const int _iterationCount = 4096;
        private const string _serverNonce = "%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0";
        private const string _serverSalt = "W22ZaJ0SNY7soEsUEjb6gQ==";
        private const string TestUserName = "user";
        private const string TestUserSource = "source";
        private const string TestUserPassword = "pencil";

        // private static
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __descriptionCommandWireProtocol = new ConnectionDescription(
            new ConnectionId(__serverId),
            new HelloResult(
                new BsonDocument("ok", 1)
                .Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, 1)
                .Add("maxWireVersion", WireVersion.Server47)));

        /*
         * This is a simple example of a SCRAM-SHA-256 authentication exchange. The username
         * 'user' and password 'pencil' are being used, with a client nonce of "rOprNGfwEbeRWgbNEkqO"
         * C: n,,n=user,r=rOprNGfwEbeRWgbNEkqO
         * S: r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096
         * C: c=biws,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,p=dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=
         * S: v=6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=
        */

        private static readonly string __clientRequest1 = $"n,,n=user,r={_clientNonce}";
        private static readonly string __serverResponse1 =
            $"r={_clientNonce}{_serverNonce},s={_serverSalt},i={_iterationCount}";
        private static readonly string __clientRequest2 =
            $"c=biws,r={_clientNonce}{_serverNonce},p=dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=";
        private static readonly string __serverResponse2 = "v=6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=";
        private static readonly string __clientOptionalFinalRequest = "";
        private static readonly string __serverOptionalFinalResponse = "";

        /* In response, the server sends a "server-first-message" containing the
        * user's iteration count i and the user's salt, and appends its own
        * nonce to the client-specified one. */
        private static string CreateSaslStartReply(
            string clientSaslStart,
            string serverNonce,
            string serverSalt,
            int iterationCount)
        {
            // strip expected GS2 header of "n,," before parsing map
            var clientNonce = SaslMapParser.Parse(clientSaslStart.Substring(3))['r'];
            return $"r={clientNonce}{serverNonce},s={serverSalt},i={iterationCount}";
        }

        // "poisons" a SASL key-attribute pair with a particular value
        private static string PoisonSaslMessage(string message, string poison)
        {
            return message.Substring(0, message.Length - poison.Length) + poison;
        }

        private static string ToUtf8Base64(string s)
        {
            return Convert.ToBase64String((System.Text.Encoding.UTF8.GetBytes(s)));
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_command_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);

            var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, serverApi);

            var connection = new MockConnection(__serverId);
            var saslStartResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson($"{{ conversationId : 1, payload : BinData(0,'{ToUtf8Base64(__serverResponse1)}'), done : false, ok : 1 }}"));
            connection.EnqueueCommandResponseMessage(saslStartResponse);
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson($"{{ conversationId : 1, payload : BinData(0,'{ToUtf8Base64(__serverResponse2)}'), done : true, ok : 1}}"));
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
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId0}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ saslStart : 1, mechanism : \"SCRAM-SHA-256\", payload : new BinData(0, \"{ToUtf8Base64(__clientRequest1)}\"), options : {{ \"skipEmptyExchange\" : true }}, $db : \"source\"{expectedServerApiString} }} }} ] }}");
            sentMessages[1].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId1}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ saslContinue : 1, conversationId : 1, payload : new BinData(0, \"{ToUtf8Base64(__clientRequest2)}\"), $db : \"source\"{expectedServerApiString} }} }} ] }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_with_loadBalancedConnection_should_use_command_wire_protocol(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);

            var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, null);

            var connection = new MockConnection(__serverId, new ConnectionSettings(loadBalanced: true), null);
            var saslStartResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson($"{{ conversationId : 1, payload : BinData(0,'{ToUtf8Base64(__serverResponse1)}'), done : false, ok : 1 }}"));
            connection.EnqueueCommandResponseMessage(saslStartResponse);
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson($"{{ conversationId : 1, payload : BinData(0,'{ToUtf8Base64(__serverResponse2)}'), done : true, ok : 1}}"));
            connection.EnqueueCommandResponseMessage(saslContinueResponse);
            connection.Description = null;

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
            var expectedEndString =  ", \"$readPreference\" : { \"mode\" : \"primaryPreferred\" }";
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId0}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ saslStart : 1, mechanism : \"SCRAM-SHA-256\", payload : new BinData(0, \"{ToUtf8Base64(__clientRequest1)}\"), options : {{ \"skipEmptyExchange\" : true }}, $db : \"source\"{expectedEndString} }} }} ] }}");
            sentMessages[1].Should().Be($"{{ opcode : \"opmsg\", requestId : {actualRequestId1}, responseTo : 0, sections : [ {{ payloadType : 0, document : {{ saslContinue : 1, conversationId : 1, payload : new BinData(0, \"{ToUtf8Base64(__clientRequest2)}\"), $db : \"source\"{expectedEndString} }} }} ] }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values("MongoConnectionException", "MongoNotPrimaryException")] string exceptionName,
            [Values(false, true)] bool async)
        {
            var subject = CreateScramSha256SaslAuthenticator(DefaultRandomStringGenerator.Instance, null);

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
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, null);
            var poisonedSaslStart = PoisonSaslMessage(message: __clientRequest1, poison: "bluePill");
            var poisonedSaslStartResponse = CreateSaslStartReply(poisonedSaslStart, _serverNonce, _serverSalt, _iterationCount);
            var poisonedSaslStartResponseMessage = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(poisonedSaslStartResponse)}\")," +
                @" done: false,
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(poisonedSaslStartResponseMessage);
            connection.Description = __descriptionCommandWireProtocol;

            Action action;
            if (async)
            {
                action = () => subject.AuthenticateAsync(connection, __descriptionCommandWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.Authenticate(connection, __descriptionCommandWireProtocol, CancellationToken.None);
            }

            var exception = Record.Exception(action);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_serverSignature(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, null);

            var saslStartReply = CreateSaslStartReply(__clientRequest1, _serverNonce, _serverSalt, _iterationCount);
            var poisonedSaslContinueReply = PoisonSaslMessage(message: __serverResponse2, poison: "redApple");
            var saslStartResponseMessage = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(saslStartReply)}\")," +
                @" done: false,
                   ok: 1}"));
            var poisonedSaslContinueResponseMessage = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(poisonedSaslContinueReply)}\")," +
                @" done: true,
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueCommandResponseMessage(saslStartResponseMessage);
            connection.EnqueueCommandResponseMessage(poisonedSaslContinueResponseMessage);
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

            var exception = Record.Exception(act);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_authentication_succeeds(
            [Values(false, true)] bool useSpeculativeAuthenticate,
            [Values(false, true)] bool useLongAuthentication,
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, null);

            var saslStartResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1," +
                $"  payload : BinData(0,'{ToUtf8Base64(__serverResponse1)}')," +
                @"  done : false,
                    ok : 1 }"));
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1," +
                $"  payload : BinData(0,'{ToUtf8Base64(__serverResponse2)}')," +
                $"  done : {new BsonBoolean(!useLongAuthentication)}," +
                @"  ok : 1 }"));
            var saslLastStepResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1," +
                $"  payload : BinData(0,'{ToUtf8Base64(__serverOptionalFinalResponse)}')," +
                @"  done : true,
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
                // We must call CustomizeInitialHelloCommand so that the authenticator thinks its started to speculatively
                // authenticate
                helloCommand = subject.CustomizeInitialHelloCommand(new BsonDocument { { OppressiveLanguageConstants.LegacyHelloCommandName, 1 } }, default);
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

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

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
            for (var i = 0; i != actualRequestIds.Count; ++i)
            {
                actualRequestIds[i].Should().BeInRange(expectedRequestId + i, expectedRequestId + 10 + i);
            }

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
                        mechanism : 'SCRAM-SHA-256',
                        payload : new BinData(0, '{ToUtf8Base64(__clientRequest1)}'),
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
                        payload : new BinData(0, '{ ToUtf8Base64(__clientRequest2)}'),
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
                            payload : new BinData(0, '{ToUtf8Base64(__clientOptionalFinalRequest)}'),
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
                expectedSpeculativeAuthenticateDocument.Add(new BsonElement("db", TestUserSource));
                speculativeAuthenticateDocument.Should().Be(expectedSpeculativeAuthenticateDocument);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_use_cache(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, null);

            var saslStartResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(__serverResponse1)}\")," +
                @" done: false,
                   ok: 1}"));
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(__serverResponse2)}\")," +
                @" done: true,
                   ok: 1}"));

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

            var scramShaMechanism = (ScramShaSaslMechanism)subject.Mechanism;
            scramShaMechanism._cache().Should().NotBe(null);
            scramShaMechanism._cache()._cacheKey().Should().NotBe(null);
            scramShaMechanism._cache()._cachedEntry().Should().NotBe(null);
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_work_regardless_of_culture(
            [Values("da-DK", "en-US")] string name,
            [Values(false, true)] bool async)
        {
            SetCultureAndResetAfterTest(name, () =>
            {
                var randomStringGenerator = new ConstantRandomStringGenerator("a");

                // ScramSha1Authenticator will have exactly the same code paths
                var subject = CreateScramSha256SaslAuthenticator(randomStringGenerator, serverApi: null);
                var mockConnection = new MockConnection();

                var payload1 = $"r=aa,s={_serverSalt},i=1";
                var serverResponse1 = $"{{ ok : 1, payload : BinData(0,\"{ToUtf8Base64(payload1)}\"), done : true, conversationId : 1 }}";
                var serverResponseRawDocument1 = RawBsonDocumentHelper.FromJson(serverResponse1);
                var serverResponseMessage1 = MessageHelper.BuildCommandResponse(serverResponseRawDocument1);

                var payload2 = $"v=v1wZS02d7kZVSzuKoB7TuI+jIpSsKvnQUkU9Oqj2t+w=";
                var serverResponse2 = $"{{ ok : 1, payload : BinData(0,\"{ToUtf8Base64(payload2)}\"), done : true }}";
                var serverResponseRawDocument2 = RawBsonDocumentHelper.FromJson(serverResponse2);
                var serverResponseMessage2 = MessageHelper.BuildCommandResponse(serverResponseRawDocument2);

                mockConnection.EnqueueCommandResponseMessage(serverResponseMessage1);
                mockConnection.EnqueueCommandResponseMessage(serverResponseMessage2);

                mockConnection.Description = __descriptionCommandWireProtocol;

                if (async)
                {
                    subject
                        .AuthenticateAsync(mockConnection, __descriptionCommandWireProtocol, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    subject.Authenticate(mockConnection, __descriptionCommandWireProtocol, CancellationToken.None);
                }
            });

            void SetCultureAndResetAfterTest(string cultureName, Action test)
            {
                var originalCulture = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(cultureName);

                try
                {
                    test();
                }
                finally
                {
                    Thread.CurrentThread.CurrentCulture = originalCulture;
                }
            }
        }

        private static SaslAuthenticator CreateScramSha256SaslAuthenticator(IRandomStringGenerator randomStringGenerator, ServerApi serverApi)
        {
            var saslContext = new SaslContext
            {
                EndPoint = __serverId.EndPoint,
                ClusterEndPoints = [ __serverId.EndPoint ],
                Identity = new MongoExternalIdentity(TestUserSource, TestUserName),
                IdentityEvidence = new PasswordEvidence(TestUserPassword),
                Mechanism = "SCRAM-SHA-256",
                MechanismProperties = null,
            };

            var awsSaslMechanism = ScramShaSaslMechanism.CreateScramSha256Mechanism(saslContext, randomStringGenerator);
            return new SaslAuthenticator(awsSaslMechanism, serverApi);
        }
    }

    internal static class ScramShaSaslMechanismReflector
    {
        public static ScramCache _cache(this ScramShaSaslMechanism obj) =>
             (ScramCache)Reflector.GetFieldValue(obj, nameof(_cache));

    }

    internal static class ScramCacheReflector
    {
        public static ScramCacheKey _cacheKey(this ScramCache obj) =>
            (ScramCacheKey)Reflector.GetFieldValue(obj, nameof(_cacheKey));

        public static ScramCacheEntry _cachedEntry(this ScramCache obj) =>
            (ScramCacheEntry)Reflector.GetFieldValue(obj, nameof(_cachedEntry));
    }
}
