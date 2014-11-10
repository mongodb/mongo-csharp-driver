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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using NUnit.Framework;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Authentication
{
    [TestFixture]
    public class MongoDBX509AuthenticatorTests
    {
        private static readonly ClusterId __clusterId = new ClusterId();
        private static readonly ServerId __serverId = new ServerId(__clusterId, new DnsEndPoint("localhost", 27017));
        private static readonly ConnectionDescription __description = new ConnectionDescription(
            new ConnectionId(__serverId),
            new IsMasterResult(new BsonDocument("ok", 1).Add("ismaster", 1)),
            new BuildInfoResult(new BsonDocument("version", "2.6.0")));

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void Constructor_should_throw_an_ArgumentException_when_username_is_null_or_empty(string username)
        {
            Action act = () => new MongoDBX509Authenticator(username);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void AuthenticateAsync_should_throw_an_AuthenticationException_when_authentication_fails()
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US");

            var reply = MessageHelper.BuildNoDocumentsReturnedReply<RawBsonDocument>();
            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            Action act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).Wait();

            act.ShouldThrow<MongoAuthenticationException>();
        }

        [Test]
        public void AuthenticateAsync_should_not_throw_when_authentication_succeeds()
        {
            var subject = new MongoDBX509Authenticator("CN=client,OU=kerneluser,O=10Gen,L=New York City,ST=New York,C=US");

            var reply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ok: 1}"));

            var connection = new MockConnection(__serverId);
            connection.EnqueueReplyMessage(reply);

            Action act = () => subject.AuthenticateAsync(connection, __description, CancellationToken.None).Wait();

            act.ShouldNotThrow();
        }
    }
}