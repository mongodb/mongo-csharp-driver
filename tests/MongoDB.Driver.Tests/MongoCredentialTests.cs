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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Authentication.Oidc;
using MongoDB.Driver.Core.TestHelpers.Authentication;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoCredentialTests
    {
        [Fact]
        public void TestCreateMongoCRCredential()
        {
#pragma warning disable 618
            var credential = MongoCredential.CreateMongoCRCredential("db", "username", "password");
#pragma warning restore 618
            Assert.Equal("MONGODB-CR", credential.Mechanism);
            Assert.Equal("username", credential.Username);
            Assert.Equal(new PasswordEvidence("password"), credential.Evidence);
        }

        [Fact]
        public void TestCreateMongoX509Credential()
        {
            var credential = MongoCredential.CreateMongoX509Credential("username");
            Assert.Equal("MONGODB-X509", credential.Mechanism);
            Assert.Equal("username", credential.Username);
            Assert.IsType<ExternalEvidence>(credential.Evidence);
        }

        [Fact]
        public void TestCreateMongoX509Credential_without_username()
        {
            var credential = MongoCredential.CreateMongoX509Credential();
            Assert.Equal("MONGODB-X509", credential.Mechanism);
            Assert.Equal(null, credential.Username);
            Assert.IsType<ExternalEvidence>(credential.Evidence);
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateOidcCredential_should_initialize_all_required_properties_in_callback_mode(
            [Values(false, true)] bool withRefreshCallbackProvider,
            [Values(false, true)] bool async)
        {
            const string principalName = "principalName";
            const string allowedHost = "allowedHost";
            var requestTokenSyncDocument = new BsonDocument("requestSync", 1);
            var requestTokenAsyncDocument = new BsonDocument("requestAsync", 1);
            var refreshTokenSyncDocument = new BsonDocument("refreshSync", 1);
            var refreshTokenAsyncDocument = new BsonDocument("refreshAsync", 1);
            RequestCallback requestFunc = (a, b, ct) => requestTokenSyncDocument;
            RefreshCallback refreshFunc = (a, b, c, ct) => refreshTokenSyncDocument;

            var credential = MongoCredential.CreateOidcCredential(
                requestCallbackProvider: (RequestCallbackProvider)GetCallbackProvider(isRequest: true),
                refreshCallbackProvider: withRefreshCallbackProvider ? (RefreshCallbackProvider)GetCallbackProvider(isRequest: false) : null,
                principalName,
                allowedHosts: new[] { allowedHost });

            credential.Mechanism.Should().Be("MONGODB-OIDC");
            credential.Username.Should().Be(principalName);
            credential.Evidence.Should().BeOfType<ExternalEvidence>();

            var dummyDocument = new BsonDocument();
            var requestProvider = credential.GetMechanismProperty<IOidcRequestCallbackProvider>(MongoOidcAuthenticator.RequestCallbackName, defaultValue: null)
                .Should().BeOfType<RequestCallbackProvider>().Subject;
            requestProvider.GetTokenResult(new OidcClientInfo(principalName), dummyDocument, CancellationToken.None).Should().Be(async ? null : requestTokenSyncDocument);
            requestProvider.GetTokenResultAsync(new OidcClientInfo(principalName), dummyDocument, CancellationToken.None)?.GetAwaiter().GetResult().Should().Be(async ? requestTokenAsyncDocument : null);
            var refreshProvider = credential.GetMechanismProperty<IOidcRefreshCallbackProvider>(MongoOidcAuthenticator.RefreshCallbackName, defaultValue: null);
            if (withRefreshCallbackProvider)
            {
                refreshProvider.Should().BeOfType<RefreshCallbackProvider>();
                refreshProvider.GetTokenResult(new OidcClientInfo(principalName), dummyDocument, dummyDocument, CancellationToken.None).Should().Be(async ? null : refreshTokenSyncDocument);
                refreshProvider.GetTokenResultAsync(new OidcClientInfo(principalName), dummyDocument, dummyDocument, CancellationToken.None)?.GetAwaiter().GetResult().Should().Be(async ? refreshTokenAsyncDocument : null);
            }
            else
            {
                refreshProvider.Should().BeNull();
            }
            credential.GetMechanismProperty<IEnumerable<string>>(MongoOidcAuthenticator.AllowedHostsName, defaultValue: null)
                .Should().BeAssignableTo<IEnumerable<string>>().Subject.Single()
                .Should().Be(allowedHost);

            object GetCallbackProvider(bool isRequest) =>
                isRequest
                    ? async
                        ? new RequestCallbackProvider(requestCallbackFunc: null, requestCallbackAsyncFunc: (a, b, ct) => Task.Run(() => requestTokenAsyncDocument), autoGenerateMissedCallback: false)
                        : new RequestCallbackProvider((a, b, ct) => requestTokenSyncDocument, autoGenerateMissedCallback: false)
                    : async
                        ? new RefreshCallbackProvider(refreshCallbackFunc: null, refreshCallbackAsyncFunc: (a, b, c, ct) => Task.Run(() => refreshTokenAsyncDocument), autoGenerateMissedCallback: false)
                        : new RefreshCallbackProvider((a, b, c, ct) => refreshTokenSyncDocument, autoGenerateMissedCallback: false);
        }

        [Fact]
        public void CreateOidcCredential_should_initialize_all_required_properties_in_provider_mode()
        {
            const string providerName = "providerName";
            const string allowedHost = "allowedHost";
            var credential = MongoCredential.CreateOidcCredential(providerName, new[] { allowedHost });

            credential.Mechanism.Should().Be("MONGODB-OIDC");
            credential.Username.Should().BeNull();
            credential.Evidence.Should().BeOfType<ExternalEvidence>();
            credential.GetMechanismProperty<string>(MongoOidcAuthenticator.ProviderName, defaultValue: null)
                .Should().BeOfType<string>().Subject
                .Should().Be(providerName);
            credential.GetMechanismProperty<IEnumerable<string>>(MongoOidcAuthenticator.AllowedHostsName, defaultValue: null)
                .Should().BeAssignableTo<IEnumerable<string>>().Subject.Single()
                .Should().Be(allowedHost);
        }

        [Fact]
        public void TestEquals()
        {
#pragma warning disable 618
            var a = MongoCredential.CreateMongoCRCredential("db", "user1", "password");
            var b = MongoCredential.CreateMongoCRCredential("db", "user1", "password");
            var c = MongoCredential.CreateMongoCRCredential("db", "user2", "password");
            var d = MongoCredential.CreateMongoCRCredential("db", "user2", "password1");
            var e = MongoCredential.CreateMongoCRCredential("db", "user2", "password1").WithMechanismProperty("TEST", true);
            var f = MongoCredential.CreateMongoCRCredential("db", "user2", "password1").WithMechanismProperty("TEST", true);
            var n = (MongoCredential)null;
#pragma warning restore 618

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));
            Assert.False(c.Equals(d));
            Assert.False(d.Equals(e));
            Assert.True(e.Equals(f));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);
            Assert.False(c == d);
            Assert.False(d == e);
            Assert.True(e == f);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
            Assert.True(c != d);
            Assert.True(d != e);
            Assert.False(e != f);
        }

        [Fact]
        public void TestPassword()
        {
#pragma warning disable 618
            var credentials = MongoCredential.CreateMongoCRCredential("database", "username", "password");
            Assert.Equal("password", credentials.Password);
#pragma warning restore 618
        }

        [Fact]
        public void TestCreateGssapiCredentialWithOnlyUsername()
        {
            var username = "testuser";
            var credential = MongoCredential.CreateGssapiCredential(username);
            Assert.Equal(username, credential.Username);
            Assert.IsType<ExternalEvidence>(credential.Evidence);
            Assert.Equal("GSSAPI", credential.Mechanism);
            Assert.Equal("$external", credential.Source);
            Assert.Equal(new ExternalEvidence(), credential.Evidence);
        }

        [Fact]
        public void TestCreatePlainCredential()
        {
            var credential = MongoCredential.CreatePlainCredential("$external", "a", "b");
            Assert.Equal("a", credential.Username);
            Assert.IsType<PasswordEvidence>(credential.Evidence);
            Assert.Equal("PLAIN", credential.Mechanism);
            Assert.Equal("$external", credential.Source);
            Assert.Equal(new PasswordEvidence("b"), credential.Evidence);
        }

        [Fact]
        public void TestMechanismProperty()
        {
#pragma warning disable 618
            var credential = MongoCredential.CreateMongoCRCredential("database", "username", "password");
#pragma warning restore 618
            var withProperties = credential
                .WithMechanismProperty("SPN", "awesome")
                .WithMechanismProperty("OTHER", 10);


            Assert.NotSame(credential, withProperties);
            Assert.Null(credential.GetMechanismProperty<string>("SPN", null));
            Assert.Equal(0, credential.GetMechanismProperty<int>("OTHER", 0));
            Assert.Equal("awesome", withProperties.GetMechanismProperty<string>("SPN", null));
            Assert.Equal(10, withProperties.GetMechanismProperty<int>("OTHER", 0));
        }
    }
}
