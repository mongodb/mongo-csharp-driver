/* Copyright 2010-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Authentication
{
    public class AuthenticationHelperTests
    {
        [Theory]
        [InlineData("user", "pencil", "1c33006ec1ffd90f9cadcbcc0e118200")]
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

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_invoke_authenticators_when_they_exist(
            [Values(false, true)]
            bool async)
        {
            var description = new ConnectionDescription(
                new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
                new HelloResult(new BsonDocument("ok", 1).Add(new BsonElement("maxWireVersion", WireVersion.Server36))));

            var mockAuthenticator = new Mock<IAuthenticator>();
            var settings = new ConnectionSettings(authenticatorFactory: new AuthenticatorFactory(() => mockAuthenticator.Object));
            var authenticator = settings.AuthenticatorFactory?.Create();

            var mockConnection = new Mock<IConnection>();
            mockConnection.SetupGet(c => c.Description).Returns(description);
            mockConnection.SetupGet(c => c.Settings).Returns(settings);

            if (async)
            {
                await AuthenticationHelper.AuthenticateAsync(OperationContext.NoTimeout, mockConnection.Object, description, authenticator);

                mockAuthenticator.Verify(a => a.AuthenticateAsync(It.IsAny<OperationContext>(), mockConnection.Object, description), Times.Once);
            }
            else
            {
                AuthenticationHelper.Authenticate(OperationContext.NoTimeout, mockConnection.Object, description, authenticator);

                mockAuthenticator.Verify(a => a.Authenticate(It.IsAny<OperationContext>(), mockConnection.Object, description), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Authenticate_should_not_invoke_authenticator_when_connected_to_an_arbiter(
            [Values(false, true)]
            bool async)
        {
            var description = new ConnectionDescription(
                new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
                new HelloResult(new BsonDocument("ok", 1).Add("setName", "rs").Add("arbiterOnly", true).Add("maxWireVersion", WireVersion.Server36)));

            var mockAuthenticator = new Mock<IAuthenticator>();
            var settings = new ConnectionSettings(authenticatorFactory: new AuthenticatorFactory(() => mockAuthenticator.Object));
            var authenticator = settings.AuthenticatorFactory?.Create();

            var mockConnection = new Mock<IConnection>();
            mockConnection.SetupGet(c => c.Description).Returns(description);
            mockConnection.SetupGet(c => c.Settings).Returns(settings);

            if (async)
            {
                await AuthenticationHelper.AuthenticateAsync(OperationContext.NoTimeout, mockConnection.Object, description, authenticator);

                mockAuthenticator.Verify(a => a.AuthenticateAsync(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionDescription>()), Times.Never);
            }
            else
            {
                AuthenticationHelper.Authenticate(OperationContext.NoTimeout, mockConnection.Object, description, authenticator);

                mockAuthenticator.Verify(a => a.Authenticate(It.IsAny<OperationContext>(), It.IsAny<IConnection>(), It.IsAny<ConnectionDescription>()), Times.Never);
            }
        }
    }
}
