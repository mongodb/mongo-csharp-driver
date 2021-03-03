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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Xunit;
using MongoDB.Driver.Core.Connections;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using System.Linq;

namespace MongoDB.Driver.Core.Authentication
{
    public class ScramSha256AuthenticatorTests
    {
        // private constants
        private const string _clientNonce = "rOprNGfwEbeRWgbNEkqO";
        private const int _iterationCount = 4096;
        private const string _serverNonce = "%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0";
        private const string _serverSalt = "W22ZaJ0SNY7soEsUEjb6gQ==";

        // private static
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
            new BuildInfoResult(new BsonDocument("version", "3.4.0")));

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

        private static void Authenticate(
            ScramSha256Authenticator authenticator,
            IConnection connection,
            bool async)
        {
            if (async)
            {
                authenticator
                    .AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                authenticator.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None);
            }
        }

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

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_credential_is_null()
        {
            var exception = Record.Exception(() => new ScramSha256Authenticator(credential: null, serverApi: null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_command_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);

            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi);

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
        public void Authenticate_should_send_serverApi_with_query_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi);

            var connection = new MockConnection(__serverId);
            var saslStartReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson($"{{ conversationId : 1, payload : BinData(0,'{ToUtf8Base64(__serverResponse1)}'), done : false, ok : 1 }}"));
            connection.EnqueueReplyMessage(saslStartReply);
            var saslContinueReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson($"{{ conversationId : 1, payload : BinData(0,'{ToUtf8Base64(__serverResponse2)}'), done : true, ok : 1}}"));
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
            sentMessages[0].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId0}, database : \"source\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ saslStart : 1, mechanism : \"SCRAM-SHA-256\", payload : new BinData(0, \"{ToUtf8Base64(__clientRequest1)}\"), options : {{ \"skipEmptyExchange\" : true }}{expectedServerApiString} }} }}");
            sentMessages[1].Should().Be($"{{ opcode : \"query\", requestId : {actualRequestId1}, database : \"source\", collection : \"$cmd\", batchSize : -1, slaveOk : true, query : {{ saslContinue : 1, conversationId : 1, payload : new BinData(0, \"{ToUtf8Base64(__clientRequest2)}\"){expectedServerApiString} }} }}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)] bool async)
        {
            var subject = new ScramSha256Authenticator(__credential, serverApi: null);

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            var act = async
                ? () => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult()
                : (Action)(() => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None));

            var exception = Record.Exception(act);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_r_value(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi: null);
            var poisonedSaslStart = PoisonSaslMessage(message: __clientRequest1, poison: "bluePill");
            var poisonedSaslStartReply = CreateSaslStartReply(poisonedSaslStart, _serverNonce, _serverSalt, _iterationCount);
            var poisonedSaslStartReplyMessage = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(poisonedSaslStartReply)}\")," +
                @" done: false,
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(poisonedSaslStartReplyMessage);

            var act = async
                ? () => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult()
                : (Action)(() => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None));

            var exception = Record.Exception(act);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_serverSignature(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = CreateSaslStartReply(__clientRequest1, _serverNonce, _serverSalt, _iterationCount);
            var poisonedSaslContinueReply = PoisonSaslMessage(message: __serverResponse2, poison: "redApple");
            var saslStartReplyMessage = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(saslStartReply)}\")," +
                @" done: false,
                   ok: 1}"));
            var poisonedSaslContinueReplyMessage = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(poisonedSaslContinueReply)}\")," +
                @" done: true,
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReplyMessage);
            connection.EnqueueReplyMessage(poisonedSaslContinueReplyMessage);

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None);
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
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1," +
                $"  payload : BinData(0,'{ToUtf8Base64(__serverResponse1)}')," +
                @"  done : false,
                    ok : 1 }"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1," +
                $"  payload : BinData(0,'{ToUtf8Base64(__serverResponse2)}')," +
                $"  done : {new BsonBoolean(!useLongAuthentication)}," +
                @"  ok : 1 }"));
            var saslLastStepReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{ conversationId : 1," +
                $"  payload : BinData(0,'{ToUtf8Base64(__serverOptionalFinalResponse)}')," +
                @"  done : true,
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
                __descriptionQueryWireProtocol.ConnectionId,
                new IsMasterResult(isMasterResult),
                new BuildInfoResult(new BsonDocument("version", "3.4")));

            BsonDocument isMasterCommand = null;
            if (useSpeculativeAuthenticate)
            {
                // We must call CustomizeIsMasterCommand so that the authenticator thinks its started to speculatively
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

            var saslStartMessage = BsonDocument.Parse(
                @"{ opcode : 'query'," +
                $"  requestId : {actualRequestIds[0]}," +
                @"  database : 'source',
                    collection : '$cmd',
                    batchSize : -1,
                    slaveOk : true,
                    query : { saslStart : 1,
                              mechanism : 'SCRAM-SHA-256'," +
                $"            payload : new BinData(0, '{ToUtf8Base64(__clientRequest1)}')" +
                @"            options : { skipEmptyExchange: true }}}");

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
                $"            payload : new BinData(0, \"{ToUtf8Base64(__clientRequest2)}\")}}}}");
            expectedMessages.Add(saslContinueMessage);

            if (useLongAuthentication)
            {
                var saslOptionalFinalMessage = BsonDocument.Parse(
                    @"{ opcode : 'query'," +
                    $"  requestId : {(useSpeculativeAuthenticate ? actualRequestIds[1] : actualRequestIds[2])}," +
                    @"  database : 'source',
                        collection : '$cmd',
                        batchSize : -1,
                        slaveOk : true,
                        query : { saslContinue : 1,
                                  conversationId : 1, " +
                    $"            payload : new BinData(0, '{ToUtf8Base64(__clientOptionalFinalRequest)}')}}}}");
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
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi: null);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(__serverResponse1)}\")," +
                @" done: false,
                   ok: 1}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(__serverResponse2)}\")," +
                @" done: true,
                   ok: 1}"));

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

#if !NETCOREAPP1_1
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
                var subject = new ScramSha256Authenticator(__credential, randomStringGenerator, serverApi: null);
                var mockConnection = new MockConnection();

                var payload1 = $"r=aa,s={_serverSalt},i=1";
                var serverResponse1 = $"{{ ok : 1, payload : BinData(0,\"{ToUtf8Base64(payload1)}\"), done : true, conversationId : 1 }}";
                var serverResponseRawDocument1 = RawBsonDocumentHelper.FromJson(serverResponse1);
                var serverResponseMessage1 = MessageHelper.BuildReply(serverResponseRawDocument1);

                var payload2 = $"v=v1wZS02d7kZVSzuKoB7TuI+jIpSsKvnQUkU9Oqj2t+w=";
                var serverResponse2 = $"{{ ok : 1, payload : BinData(0,\"{ToUtf8Base64(payload2)}\"), done : true }}";
                var serverResponseRawDocument2 = RawBsonDocumentHelper.FromJson(serverResponse2);
                var serverResponseMessage2 = MessageHelper.BuildReply(serverResponseRawDocument2);

                mockConnection.EnqueueReplyMessage(serverResponseMessage1);
                mockConnection.EnqueueReplyMessage(serverResponseMessage2);

                Authenticate(subject, mockConnection, async);
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
#endif
    }

    internal static class ScramShaAuthenticatorReflector
    {
        public static ScramCache _cache(this ScramShaAuthenticator obj) =>
             (ScramCache)Reflector.GetFieldValue(Reflector.GetFieldValue(obj, "_mechanism"), nameof(_cache));

    }

    internal static class ScramCacheReflector
    {
        public static ScramCacheKey _cacheKey(this ScramCache obj) =>
            (ScramCacheKey)Reflector.GetFieldValue(obj, nameof(_cacheKey));

        public static ScramCacheEntry _cachedEntry(this ScramCache obj) =>
            (ScramCacheEntry)Reflector.GetFieldValue(obj, nameof(_cachedEntry));
    }
}
