/* Copyright 2013-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Helpers;
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Driver.Core.Connections
{
    public class ConnectionInitializerTests
    {
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
        private ConnectionInitializer _subject;

        public ConnectionInitializerTests()
        {
            _subject = new ConnectionInitializer("test");
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnection_should_throw_an_ArgumentNullException_if_the_connection_is_null(
            [Values(false, true)]
            bool async)
        {
            Action act;
            if (async)
            {
                act = () => _subject.InitializeConnectionAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.InitializeConnection(null, CancellationToken.None);
            }

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeConnectionA_should_build_the_ConnectionDescription_correctly(
            [Values(false, true)]
            bool async)
        {
            var isMasterReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1 }"));
            var buildInfoReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));
            var gleReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, connectionId: 10 }"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(isMasterReply);
            connection.EnqueueReplyMessage(buildInfoReply);
            connection.EnqueueReplyMessage(gleReply);

            ConnectionDescription result;
            if (async)
            {
                result = _subject.InitializeConnectionAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = _subject.InitializeConnection(connection, CancellationToken.None);
            }

            result.ServerVersion.Should().Be(new SemanticVersion(2, 6, 3));
            result.ConnectionId.ServerValue.Should().Be(10);
        }

        [Fact]
        public void CreateIsMasterCommand_should_return_expected_result()
        {
            var result = _subject.CreateIsMasterCommand();

            var names = result.Names.ToList();
            names.Count.Should().Be(2);
            names[0].Should().Be("isMaster");
            names[1].Should().Be("client");
            result[0].Should().Be(1);
            var clientDocument = result[1].AsBsonDocument;
            var clientDocumentNames = clientDocument.Names.ToList();
            clientDocumentNames.Count.Should().Be(4);
            clientDocumentNames[0].Should().Be("application");
            clientDocumentNames[1].Should().Be("driver");
            clientDocumentNames[2].Should().Be("os");
            clientDocumentNames[3].Should().Be("platform");
            clientDocument["application"]["name"].AsString.Should().Be("test");
            clientDocument["driver"]["name"].AsString.Should().Be("mongo-csharp-driver");
            clientDocument["driver"]["version"].BsonType.Should().Be(BsonType.String);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateIsMasterCommand_with_args_should_return_expected_result(
            [Values("{ client : { driver : 'dotnet', version : '2.4.0' }, os : { type : 'Windows' } }")]
            string clientDocumentString)
        {
            var clientDocument = BsonDocument.Parse(clientDocumentString);

            var result = _subject.CreateIsMasterCommand(clientDocument);

            result.Should().Be($"{{ isMaster : 1, client : {clientDocumentString} }}");
        }
    }
}