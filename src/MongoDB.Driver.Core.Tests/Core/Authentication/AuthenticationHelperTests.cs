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

using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Authentication
{
    [TestFixture]
    public class AuthenticationHelperTests
    {
        [Test]
        [TestCase("user", "pencil", "1c33006ec1ffd90f9cadcbcc0e118200")]
        public void MongoPasswordDigest_should_create_the_correct_hash(string username, string password, string expected)
        {
            var securePassword = new SecureString();
            foreach (var c in password)
            {
                securePassword.AppendChar(c);
            }
            securePassword.MakeReadOnly();

            var passwordDigest = AuthenticationHelper.MongoPasswordDigest(username, securePassword);

            passwordDigest.Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Authenticate_should_invoke_authenticators_when_they_exist(
            [Values(false, true)]
            bool async)
        {
            var description = new ConnectionDescription(
                new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
                new IsMasterResult(new BsonDocument("ok", 1)),
                new BuildInfoResult(new BsonDocument("version", "2.8.0")));

            var authenticator = Substitute.For<IAuthenticator>();
            var settings = new ConnectionSettings(authenticators: new[] { authenticator });

            var connection = Substitute.For<IConnection>();
            connection.Description.Returns(description);
            connection.Settings.Returns(settings);

            if (async)
            {
                AuthenticationHelper.AuthenticateAsync(connection, description, CancellationToken.None).GetAwaiter().GetResult();

                authenticator.ReceivedWithAnyArgs().AuthenticateAsync(null, null, CancellationToken.None);
            }
            else
            {
                AuthenticationHelper.Authenticate(connection, description, CancellationToken.None);

                authenticator.ReceivedWithAnyArgs().Authenticate(null, null, CancellationToken.None);
            }
        }

        [Test]
        public void Authenticate_should_not_invoke_authenticators_when_connected_to_an_arbiter(
            [Values(false, true)]
            bool async)
        {
            var description = new ConnectionDescription(
                new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
                new IsMasterResult(new BsonDocument("ok", 1).Add("setName", "rs").Add("arbiterOnly", true)),
                new BuildInfoResult(new BsonDocument("version", "2.8.0")));

            var authenticator = Substitute.For<IAuthenticator>();
            var settings = new ConnectionSettings(authenticators: new[] { authenticator });

            var connection = Substitute.For<IConnection>();
            connection.Description.Returns(description);
            connection.Settings.Returns(settings);

            if (async)
            {
                AuthenticationHelper.AuthenticateAsync(connection, description, CancellationToken.None).GetAwaiter().GetResult();

                authenticator.DidNotReceiveWithAnyArgs().AuthenticateAsync(null, null, CancellationToken.None);
            }
            else
            {
                AuthenticationHelper.Authenticate(connection, description, CancellationToken.None);

                authenticator.DidNotReceiveWithAnyArgs().Authenticate(null, null, CancellationToken.None);
            }
        }
    }
}
