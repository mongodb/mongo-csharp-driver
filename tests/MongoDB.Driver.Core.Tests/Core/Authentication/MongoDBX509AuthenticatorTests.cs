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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Xunit;
using MongoDB.Driver.Core.Connections;
using System.Threading.Tasks;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    public class MongoDBX509AuthenticatorTests
    {
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __description = new ConnectionDescription(
            new ConnectionId(__serverId),
            new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
            new BuildInfoResult(new BsonDocument("version", "2.6.0")));

        [Theory]
        [InlineData("")]
        public void Constructor_should_throw_an_ArgumentException_when_username_is_empty(string username)
        {
            Action act = () => new MongoDBX509Authenticator(username);

            act.ShouldThrow<ArgumentException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_an_AuthenticationException_when_authentication_fails(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US");

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(new SemanticVersion(3, 2, 0));
            connection.EnqueueReplyMessage(reply);

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __description, CancellationToken.None);
            }

            act.ShouldThrow<MongoAuthenticationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_authentication_succeeds(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US");

            var reply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(new SemanticVersion(3, 2, 0));
            connection.EnqueueReplyMessage(reply);

            Action act;
            if (async)
            {
                act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.Authenticate(connection, __description, CancellationToken.None);
            }

            act.ShouldNotThrow();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_throw_when_username_is_null_and_server_does_not_support_null_username(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator(null);

            var reply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.Description = CreateConnectionDescription(new SemanticVersion(3, 2, 0));
            connection.EnqueueReplyMessage(reply);

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => subject.AuthenticateAsync(connection, __description, CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => subject.Authenticate(connection, __description, CancellationToken.None));
            }

            exception.Should().BeOfType<MongoConnectionException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Authenticate_should_not_throw_when_username_is_null_and_server_support_null_username(
            [Values(false, true)]
            bool async)
        {
            var subject = new MongoDBX509Authenticator(null);

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