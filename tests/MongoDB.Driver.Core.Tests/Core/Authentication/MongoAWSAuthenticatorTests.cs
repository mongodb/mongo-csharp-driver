/* Copyright 2020–present MongoDB Inc.
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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.Authentication
{
    public class MongoAWSAuthenticatorTests
    {
        // private constants
        private const int ClientNonceLength = 32;

        #region static
        // private static fields
        private static readonly ConnectionDescription __descriptionCommandWireProtocol;
        private static readonly ConnectionDescription __descriptionQueryWireProtocol;
        private static readonly IRandomByteGenerator __randomByteGenerator = new DefaultRandomByteGenerator();
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));

        // static constructor
        static MongoAWSAuthenticatorTests()
        {
            __descriptionCommandWireProtocol = new ConnectionDescription(
                new ConnectionId(__serverId),
                new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
                new BuildInfoResult(new BsonDocument("version", "4.7.0")));
            __descriptionQueryWireProtocol = new ConnectionDescription(
                new ConnectionId(__serverId),
                new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
                new BuildInfoResult(new BsonDocument("version", "2.6.0")));
        }
        #endregion

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_have_expected_result(
            [Values(false, true)] bool async)
        {
            var dateTime = DateTime.UtcNow;
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(ClientNonceLength));
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");

            AwsSignatureVersion4.CreateAuthorizationRequest(
                dateTime,
                credential.Username,
                credential.Password,
                null,
                serverNonce,
                host,
                out var authHeader,
                out var timestamp);

            var mockClock = new Mock<IClock>();
            mockClock.Setup(x => x.UtcNow).Returns(dateTime);

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var expectedClientFirstMessage = new BsonDocument
            {
                { "r", clientNonce },
                { "p", (int)'n' }
            };
            var expectedClientSecondMessage = new BsonDocument
            {
                { "a", authHeader },
                { "d", timestamp }
            };
            var serverFirstMessage = new BsonDocument
            {
                { "s", serverNonce },
                { "h", host }
            };

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1 }}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                "{ conversationId : 1, done : true, payload : BinData(0,\"\"), ok : 1}"));

            var subject = new MongoAWSAuthenticator(credential, null, mockRandomByteGenerator.Object, mockClock.Object, serverApi: null);

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

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

            var expectedFirstMessage = GetExpectedSaslStartQueryMessage(actualRequestId0, expectedClientFirstMessage);
            var expectedSecondMessage = GetExpectedSaslContinueQueryMessage(actualRequestId1, expectedClientSecondMessage);

            sentMessages[0].Should().Be(expectedFirstMessage);
            sentMessages[1].Should().Be(expectedSecondMessage);
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_command_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var dateTime = DateTime.UtcNow;
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(ClientNonceLength));
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            AwsSignatureVersion4.CreateAuthorizationRequest(
                dateTime,
                credential.Username,
                credential.Password,
                null,
                serverNonce,
                host,
                out var authHeader,
                out var timestamp);

            var mockClock = new Mock<IClock>();
            mockClock.Setup(x => x.UtcNow).Returns(dateTime);

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var expectedClientFirstMessage = new BsonDocument
            {
                { "r", clientNonce },
                { "p", (int)'n' }
            };
            var expectedClientSecondMessage = new BsonDocument
            {
                { "a", authHeader },
                { "d", timestamp }
            };
            var serverFirstMessage = new BsonDocument
            {
                { "s", serverNonce },
                { "h", host }
            };

            var saslStartReplyString = $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1 }}";
            var saslContinueReplyString = "{ conversationId : 1, done : true, payload : BinData(0,\"\"), ok : 1}";

            var subject = new MongoAWSAuthenticator(credential, null, mockRandomByteGenerator.Object, mockClock.Object, serverApi);

            var connection = new MockConnection(__serverId);
            var saslStartResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(saslStartReplyString));
            var saslContinueResponse = MessageHelper.BuildCommandResponse(RawBsonDocumentHelper.FromJson(saslContinueReplyString));
            connection.EnqueueCommandResponseMessage(saslStartResponse);
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
            string expectedFirstMessage = GetExpectedSaslStartCommandMessage(actualRequestId0, expectedClientFirstMessage, expectedServerApiString);
            string expectedSecondMessage = GetExpectedSaslContinueCommandMessage(actualRequestId1, expectedClientSecondMessage, expectedServerApiString);

            sentMessages[0].Should().Be(expectedFirstMessage);
            sentMessages[1].Should().Be(expectedSecondMessage);
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_send_serverApi_with_query_wire_protocol(
            [Values(false, true)] bool useServerApi,
            [Values(false, true)] bool async)
        {
            var dateTime = DateTime.UtcNow;
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(ClientNonceLength));
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");
            var serverApi = useServerApi ? new ServerApi(ServerApiVersion.V1, true, true) : null;

            AwsSignatureVersion4.CreateAuthorizationRequest(
                dateTime,
                credential.Username,
                credential.Password,
                null,
                serverNonce,
                host,
                out var authHeader,
                out var timestamp);

            var mockClock = new Mock<IClock>();
            mockClock.Setup(x => x.UtcNow).Returns(dateTime);

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var expectedClientFirstMessage = new BsonDocument
            {
                { "r", clientNonce },
                { "p", (int)'n' }
            };
            var expectedClientSecondMessage = new BsonDocument
            {
                { "a", authHeader },
                { "d", timestamp }
            };
            var serverFirstMessage = new BsonDocument
            {
                { "s", serverNonce },
                { "h", host }
            };

            var saslStartReplyString = $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1 }}";
            var saslContinueReplyString = "{ conversationId : 1, done : true, payload : BinData(0,\"\"), ok : 1}";

            var subject = new MongoAWSAuthenticator(credential, null, mockRandomByteGenerator.Object, mockClock.Object, serverApi);

            var connection = new MockConnection(__serverId);
            var saslStartReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson(saslStartReplyString));
            var saslContinueReply = MessageHelper.BuildReply(RawBsonDocumentHelper.FromJson(saslContinueReplyString));
            connection.EnqueueReplyMessage(saslStartReply);
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
            string expectedFirstMessage = GetExpectedSaslStartQueryMessage(actualRequestId0, expectedClientFirstMessage, expectedServerApiString);
            string expectedSecondMessage = GetExpectedSaslContinueQueryMessage(actualRequestId1, expectedClientSecondMessage, expectedServerApiString);

            sentMessages[0].Should().Be(expectedFirstMessage);
            sentMessages[1].Should().Be(expectedSecondMessage);
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)] bool async)
        {
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");
            var subject = new MongoAWSAuthenticator(credential, properties: null, serverApi: null);

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
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

            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_host(
            [Values("", "abc..def")] string host,
            [Values(false, true)] bool async)
        {
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(ClientNonceLength));
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var serverFirstMessage = new BsonDocument
            {
                { "s", serverNonce },
                { "h", host },
            };

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1 }}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                "{ conversationId : 1, done : true, payload : BinData(0,\"\"), ok : 1}"));

            var subject = new MongoAWSAuthenticator(credential, null, mockRandomByteGenerator.Object, SystemClock.Instance, serverApi: null);

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None));
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
            exception.Message.Should().Be("Server returned an invalid sts host.");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_invalid_nonce(
            [Values(false, true)] bool async)
        {
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var invalidServerNonce = __randomByteGenerator.Generate(ClientNonceLength * 2);
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var serverFirstMessage = new BsonDocument
            {
                { "s", invalidServerNonce },
                { "h", host }
            };

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1 }}"));

            var subject = new MongoAWSAuthenticator(credential, null, mockRandomByteGenerator.Object, SystemClock.Instance, serverApi: null);

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None));
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
            exception.Message.Should().Be("Server sent an invalid nonce.");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_server_provides_unexpected_field(
            [Values(false, true)] bool async)
        {
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(ClientNonceLength));
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var serverFirstMessage = new BsonDocument
            {
                { "s", serverNonce },
                { "h", host },
                { "u", "unexpected" }
            };

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1 }}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                "{ conversationId : 1, done : true, payload : BinData(0,\"\"), ok : 1}"));

            var subject = new MongoAWSAuthenticator(credential, null, mockRandomByteGenerator.Object, SystemClock.Instance, serverApi: null);

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, __descriptionQueryWireProtocol, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, __descriptionQueryWireProtocol, CancellationToken.None));
            }

            exception.Should().BeOfType<MongoAuthenticationException>();
            exception.Message.Should().Be("Server returned unexpected fields: u.");
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_with_session_token_should_have_expected_result(
            [Values(false, true)] bool async)
        {
            var dateTime = DateTime.UtcNow;
            var clientNonce = __randomByteGenerator.Generate(ClientNonceLength);
            var serverNonce = Combine(clientNonce, __randomByteGenerator.Generate(ClientNonceLength));
            var host = "sts.amazonaws.com";
            var credential = new UsernamePasswordCredential("$external", "permanentuser", "FAKEFAKEFAKEFAKEFAKEfakefakefakefakefake");
            var sessionToken = "MXUpbuzwzPo67WKCNYtdBq47taFtIpt+SVx58hNx1/jSz37h9d67dtUOg0ejKrv83u8ai+VFZxMx=";

            AwsSignatureVersion4.CreateAuthorizationRequest(
                dateTime,
                credential.Username,
                credential.Password,
                sessionToken,
                serverNonce,
                host,
                out var authorizationHeader,
                out var timestamp);

            var mockClock = new Mock<IClock>();
            mockClock.Setup(x => x.UtcNow).Returns(dateTime);

            var mockRandomByteGenerator = new Mock<IRandomByteGenerator>();
            mockRandomByteGenerator.Setup(x => x.Generate(It.IsAny<int>())).Returns(clientNonce);

            var expectedClientFirstMessage = new BsonDocument
            {
                { "r", clientNonce },
                { "p", (int)'n' }
            };
            var expectedClientSecondMessage = new BsonDocument
            {
                { "a", authorizationHeader },
                { "d", timestamp },
                { "t", sessionToken }
            };
            var serverFirstMessage = new BsonDocument
            {
                { "s", serverNonce },
                { "h", host }
            };

            var saslStartReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                $"{{ conversationId : 1, done : false, payload : BinData(0,\"{ToBase64(serverFirstMessage.ToBson())}\"), ok : 1}}"));
            var saslContinueReply = MessageHelper.BuildReply<RawBsonDocument>(RawBsonDocumentHelper.FromJson(
                "{ conversationId : 1, done : true, payload : BinData(0,\"\"), ok : 1}"));

            var properties = new[] { new KeyValuePair<string, string>("AWS_SESSION_TOKEN", sessionToken) };
            var subject = new MongoAWSAuthenticator(credential, properties, mockRandomByteGenerator.Object, mockClock.Object, serverApi: null);

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(saslStartReply);
            connection.EnqueueReplyMessage(saslContinueReply);

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

            var expectedFirstMessage = GetExpectedSaslStartQueryMessage(actualRequestId0, expectedClientFirstMessage);
            var expectedSecondMessage = GetExpectedSaslContinueQueryMessage(actualRequestId1, expectedClientSecondMessage);

            sentMessages[0].Should().Be(expectedFirstMessage);
            sentMessages[1].Should().Be(expectedSecondMessage);
        }

        // private methods
        private static string GetExpectedSaslContinueCommandMessage(int requestId, BsonDocument clientMessage, string expectedServerApiString = null)
        {
            return
                "{" +
                    "opcode : \"opmsg\", " +
                    $"requestId : {requestId}, " +
                    "responseTo : 0, " +
                    "sections : " +
                    "[" +
                        "{" +
                            "payloadType : 0, " +
                            "document : " +
                            "{" +
                                "saslContinue : 1, " +
                                "conversationId : 1, " +
                                $"payload : new BinData(0, \"{ToBase64(clientMessage.ToBson())}\"), " +
                                "$db : \"$external\" " +
                                expectedServerApiString +
                            "}" +
                        "}" +
                    "]" +
                "}";
        }

        private static string GetExpectedSaslStartCommandMessage(int requestId, BsonDocument clientMessage, string expectedServerApiString = null)
        {
            return
                "{" +
                    "opcode : \"opmsg\", " +
                    $"requestId : {requestId}, " +
                    "responseTo : 0, " +
                    "sections : " +
                    "[" +
                        "{" +
                            "payloadType : 0, " +
                            "document : " +
                            "{" +
                                "saslStart : 1, " +
                                "mechanism : \"MONGODB-AWS\", " +
                                $"payload : new BinData(0, \"{ToBase64(clientMessage.ToBson())}\"), " +
                                "$db : \"$external\" " +
                                expectedServerApiString +
                            "}" +
                        "}" +
                    "]" +
                "}";
        }

        private static string GetExpectedSaslContinueQueryMessage(int requestId, BsonDocument clientMessage, string expectedServerApiString = null)
        {
            return
                "{" +
                    "opcode : \"query\", " +
                    $"requestId : {requestId}, " +
                    "database : \"$external\", " +
                    "collection : \"$cmd\", " +
                    "batchSize : -1, " +
                    "slaveOk : true, " +
                    "query : " +
                    "{ " +
                        "\"saslContinue\" : 1, " +
                        "\"conversationId\" : 1, " +
                        $"\"payload\" : new BinData(0, \"{ToBase64(clientMessage.ToBson())}\") " +
                        expectedServerApiString +
                    "}" +
                "}";
        }

        private static string GetExpectedSaslStartQueryMessage(int requestId, BsonDocument clientMessage, string expectedServerApiString = null)
        {
            return
                "{" +
                    "opcode : \"query\", " +
                    $"requestId : {requestId}, " +
                    "database : \"$external\", " +
                    "collection : \"$cmd\", " +
                    "batchSize : -1, " +
                    "slaveOk : true, " +
                    "query : " +
                    "{" +
                        "\"saslStart\" : 1, " +
                        "\"mechanism\" : \"MONGODB-AWS\", " +
                        $"\"payload\" : new BinData(0, \"{ToBase64(clientMessage.ToBson())}\")" +
                        expectedServerApiString +
                    "}" +
                "}";
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }

        private static string ToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
    }
}
