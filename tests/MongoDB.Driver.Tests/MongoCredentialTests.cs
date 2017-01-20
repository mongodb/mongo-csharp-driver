/* Copyright 2010-2014 MongoDB Inc.
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

using System.Linq;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoCredentialTests
    {
        [Fact]
        public void TestCreateMongoCRCredential()
        {
            var credential = MongoCredential.CreateMongoCRCredential("db", "username", "password");
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
            var credential = MongoCredential.CreateMongoX509Credential(null);
            Assert.Equal("MONGODB-X509", credential.Mechanism);
            Assert.Equal(null, credential.Username);
            Assert.IsType<ExternalEvidence>(credential.Evidence);
        }

        [Fact]
        public void TestEquals()
        {
            var a = MongoCredential.CreateMongoCRCredential("db", "user1", "password");
            var b = MongoCredential.CreateMongoCRCredential("db", "user1", "password");
            var c = MongoCredential.CreateMongoCRCredential("db", "user2", "password");
            var d = MongoCredential.CreateMongoCRCredential("db", "user2", "password1");
            var e = MongoCredential.CreateMongoCRCredential("db", "user2", "password1").WithMechanismProperty("TEST", true);
            var f = MongoCredential.CreateMongoCRCredential("db", "user2", "password1").WithMechanismProperty("TEST", true);
            var n = (MongoCredential)null;

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
            var credentials = MongoCredential.CreateMongoCRCredential("database", "username", "password");
#pragma warning disable 618
            Assert.Equal("password", credentials.Password);
#pragma warning restore
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
            var credential = MongoCredential.CreateMongoCRCredential("database", "username", "password");
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