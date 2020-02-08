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
        private static readonly ConnectionDescription __description = new ConnectionDescription(
            new ConnectionId(__serverId),
            new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
            new BuildInfoResult(new BsonDocument("version", "4.0.0")));

        /*
         * This is a simple example of a SCRAM-SHA-256 authentication exchange. The username
         * 'user' and password 'pencil' are being used, with a client nonce of "rOprNGfwEbeRWgbNEkqO" 
         * C: n,,n=user,r=rOprNGfwEbeRWgbNEkqO
         * S: r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,s=W22ZaJ0SNY7soEsUEjb6gQ==,i=4096
         * C: c=biws,r=rOprNGfwEbeRWgbNEkqO%hvYDpWUa2RaTCAfuxFIlj)hNlF$k0,p=dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=
         * S: v=6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=
        */

        private static readonly string _clientRequest1 = $"n,,n=user,r={_clientNonce}";
        private static readonly string _serverResponse1 =
            $"r={_clientNonce}{_serverNonce},s={_serverSalt},i={_iterationCount}";
        private static readonly string _clientRequest2 =
            $"c=biws,r={_clientNonce}{_serverNonce},p=dHzbZapWIk4jUhN+Ute9ytag9zjfMHgsqmmiz7AndVQ=";
        private static readonly string _serverReponse2 = "v=6rriTRBi23WpRR/wtup+mMhUZUn/dB5nLTJRsjl95G4=";

        private static void Authenticate(
            ScramSha256Authenticator authenticator,
            IConnection connection,
            bool async)
        {
            if (async)
            {
                authenticator
                    .AuthenticateAsync(connection, __description, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                authenticator.Authenticate(connection, __description, CancellationToken.None);
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
            var exception = Record.Exception(() => new ScramSha256Authenticator(null));
            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)] bool async)
        {
            var subject = new ScramSha256Authenticator(__credential);

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            var act = async
                ? () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult()
                : (Action)(() => subject.Authenticate(connection, __description, CancellationToken.None));

            var exception = Record.Exception(act);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_r_value(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator);
            var poisonedSaslStart = PoisonSaslMessage(message: _clientRequest1, poison: "bluePill");
            var poisonedSaslStartReply = CreateSaslStartReply(poisonedSaslStart, _serverNonce, _serverSalt, _iterationCount);
            var poisonedSaslStartReplyMessage = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1, " +
                $" payload: BinData(0,\"{ToUtf8Base64(poisonedSaslStartReply)}\")," +
                @" done: false,
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(poisonedSaslStartReplyMessage);

            var act = async
                ? () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult()
                : (Action)(() => subject.Authenticate(connection, __description, CancellationToken.None));

            var exception = Record.Exception(act);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_serverSignature(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator);

            var saslStartReply = CreateSaslStartReply(_clientRequest1, _serverNonce, _serverSalt, _iterationCount);
            var poisonedSaslContinueReply = PoisonSaslMessage(message: _serverReponse2, poison: "redApple");
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
                act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __description, CancellationToken.None);
            }

            var exception = Record.Exception(act);

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_authentication_succeeds(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(_serverResponse1)}\")," +
                @" done: false, 
                   ok: 1}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(_serverReponse2)}\")," +
                @" done: true, 
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

            var expectedRequestId = RequestMessage.CurrentGlobalRequestId + 1;

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __description, CancellationToken.None);
            }

            var exception = Record.Exception(act);
            exception.Should().BeNull();
            SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 2, TimeSpan.FromSeconds(5)).Should().BeTrue();

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            sentMessages.Count.Should().Be(2);

            var actualRequestId0 = sentMessages[0]["requestId"].AsInt32;
            var actualRequestId1 = sentMessages[1]["requestId"].AsInt32;
            actualRequestId0.Should().BeInRange(expectedRequestId, expectedRequestId + 10);
            actualRequestId1.Should().BeInRange(actualRequestId0 + 1, actualRequestId0 + 11);

            sentMessages[0].Should().Be(
                @"{opcode: ""query""," +
                $" requestId: {actualRequestId0}," +
                @" database: ""source"", 
                   collection: ""$cmd"", 
                   batchSize: -1, 
                   slaveOk: true, 
                   query: {saslStart: 1, 
                           mechanism: ""SCRAM-SHA-256""," +
                $"         payload: new BinData(0, \"{ToUtf8Base64(_clientRequest1)}\")}}}}");
            sentMessages[1].Should().Be(
                @"{opcode: ""query""," +
                $" requestId: {actualRequestId1}," +
                @" database: ""source"", 
                   collection: ""$cmd"", 
                   batchSize: -1, 
                   slaveOk: true, 
                   query: {saslContinue: 1, 
                           conversationId: 1, " +
                $"         payload: new BinData(0, \"{ToUtf8Base64(_clientRequest2)}\")}}}}");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_use_cache(
            [Values(false, true)] bool async)
        {
            var randomStringGenerator = new ConstantRandomStringGenerator(_clientNonce);
            var subject = new ScramSha256Authenticator(__credential, randomStringGenerator);

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(_serverResponse1)}\")," +
                @" done: false,
                   ok: 1}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                @"{conversationId: 1," +
                $" payload: BinData(0,\"{ToUtf8Base64(_serverReponse2)}\")," +
                @" done: true,
                   ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

            if (async)
            {
                subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter()
                    .GetResult();
            }
            else
            {
                subject.Authenticate(connection, __description, CancellationToken.None);
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
                var subject = new ScramSha256Authenticator(__credential, randomStringGenerator);
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
