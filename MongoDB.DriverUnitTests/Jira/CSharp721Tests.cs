using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.Jira.CSharp721
{
    [TestFixture]
    public class CSharp721Tests
    {
        private MongoDatabase _database;
        private MongoCollection<Entity> _collection;
        private const int MaxNoOfDocuments = 100;

        static CSharp721Tests()
        {
            BsonClassMap.RegisterClassMap<Entity>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true); // I think this is a bug to where you have to call this after AutoMap.
                    cm.SetIdMember(cm.GetMemberMap(c => c.Id).SetRepresentation(BsonType.ObjectId));
                    cm.GetMemberMap(c => c.OtherId).SetElementName("oid").SetRepresentation(BsonType.ObjectId);
                });
        }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            _database = Configuration.TestDatabase;

            var collectionSettings = new MongoCollectionSettings();
            _collection = _database.GetCollection<Entity>("csharp721", collectionSettings);
            _collection.Drop();
        }

        [Test]
        public void TestInterfaceMemberObjectIdCreation()
        {
            _collection.RemoveAll();
            for (var i = 0; i < MaxNoOfDocuments; i++)
            {
                var entity = new Entity();
                Assert.Null(entity.Id);
                Assert.Null(entity.OtherId);
                _collection.Insert(entity);
                Assert.NotNull(entity.Id);
                Assert.Null(entity.OtherId); // I believe this should be null.
            }
        }

        private interface IIdentity
        {
            string Id { get; }
        }

        private class Entity : IIdentity
        {
            public string Id { get; set; }

            public string OtherId { get; set; }
        }
    }
}