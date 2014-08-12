/* Copyright 2013-2014 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Connections
{
    [TestFixture]
    public class ConnectionInitializerTests
    {
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionId __connectionId = new ConnectionId(__serverId);
        private ConnectionInitializer _subject;

        [SetUp]
        public void Setup()
        {
            _subject = new ConnectionInitializer();
        }

        [Test]
        public void InitializeConnectionAsync_should_throw_an_ArgumentNullException_if_the_connection_is_null()
        {
            Action act = () => _subject.InitializeConnectionAsync(null, __connectionId, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void InitializeConnectionAsync_should_throw_an_ArgumentNullException_if_the_serverId_is_null()
        {
            var connection = Substitute.For<IConnection>();
            Action act = () => _subject.InitializeConnectionAsync(connection, null, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void InitializeConnectionAsync_should_build_the_ConnectionDescription_correctly()
        {
            var isMasterReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1 }"));
            var buildInfoReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));
            var gleReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);
            connection.EnqueueReplyMessage(gleReply);

            var result = _subject.InitializeConnectionAsync(connection, __connectionId, Timeout.InfiniteTimeSpan, CancellationToken.None).Result;

            result.ServerVersion.Should().Be(new SemanticVersion(2, 6, 3));
            result.ConnectionId.ServerValue.Should().Be(10);
        }

        [Test]
        public void InitializeConnectionAsync_should_invoke_authenticators_when_they_exist()
        {
            var isMasterReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1 }"));
            var buildInfoReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));
            var gleReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);
            connection.EnqueueReplyMessage(gleReply);
            var authenticator = Substitute.For<IAuthenticator>();
            connection.Settings = new ConnectionSettings()
                .WithAuthenticators(new[] { authenticator });

            _subject.InitializeConnectionAsync(connection, __connectionId, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            authenticator.ReceivedWithAnyArgs().AuthenticateAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        [Test]
        public void InitializeConnectionAsync_should_not_invoke_authenticators_when_connected_to_an_arbiter()
        {
            var isMasterReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, setName: \"funny\", arbiterOnly: true }"));
            var buildInfoReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));
            var gleReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);
            connection.EnqueueReplyMessage(gleReply);
            var authenticator = Substitute.For<IAuthenticator>();
            connection.Settings = new ConnectionSettings()
                .WithAuthenticators(new[] { authenticator });

            _subject.InitializeConnectionAsync(connection, __connectionId, Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            authenticator.DidNotReceiveWithAnyArgs().AuthenticateAsync(null, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }
    }
}