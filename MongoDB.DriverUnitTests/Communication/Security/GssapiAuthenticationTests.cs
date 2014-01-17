using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Communication.Security;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Communication.Security
{
    [TestFixture]
    [Explicit]
    [Category("Authentication")]
    [Category("GssapiMechanism")]
    public class GssapiAuthenticationTests
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
            var currentCredentialUsername = _settings.Credentials.Single().Username;
            _settings.Credentials = new[] 
            {
                MongoCredential.CreateGssapiCredential(currentCredentialUsername, "wrongPassword")
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