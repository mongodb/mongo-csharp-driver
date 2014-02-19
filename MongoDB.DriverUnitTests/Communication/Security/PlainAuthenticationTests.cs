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
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Communication.Security
{
    [TestFixture]
    [Explicit]
    [Category("Authentication")]
    [Category("PlainMechanism")]
    public class PlainAuthenticationTests
    {
        private static readonly string __collectionName = "test";

        private MongoClientSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = Configuration.TestClient.Settings.Clone();
        }

        [Test]
        public void TestNoCredentials()
        {
            _settings.Credentials = Enumerable.Empty<MongoCredential>();
            var client = new MongoClient(_settings);

            Assert.Throws<MongoQueryException>(() =>
            {
                client.GetServer()
                    .GetDatabase(Configuration.TestDatabase.Name)
                    .GetCollection(__collectionName)
                    .FindOne();
            });
        }

        [Test]
        public void TestSuccessfulAuthentication()
        {
            var client = new MongoClient(_settings);

            var result = client.GetServer()
                .GetDatabase(Configuration.TestDatabase.Name)
                .GetCollection(__collectionName)
                .FindOne();

            Assert.IsNotNull(result);
        }

        [Test]
        public void TestBadPassword()
        {
            var currentCredential = _settings.Credentials.Single();
            _settings.Credentials = new[] 
            {
                MongoCredential.CreatePlainCredential(currentCredential.Source, currentCredential.Username, "wrongPassword")
            };

            var client = new MongoClient(_settings);

            Assert.Throws<MongoConnectionException>(() =>
            {
                client.GetServer()
                    .GetDatabase(Configuration.TestDatabase.Name)
                    .GetCollection(__collectionName)
                    .FindOne();
            });
        }
    }
}