using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoDB.DriverUnitTests.Jira
{
    [TestFixture]
    public class Free10922Tests
    {
        interface IMongoObject
        {
            BsonObjectId Id { get; set; }
            string Name { get; set; }
        }

        class MongoObject : IMongoObject
        {
            public BsonObjectId Id { get; set; }
            public string Name { get; set; }
        }

        MongoServer m_db;
        string m_dbName;

        private void Save<T>(T entity) where T : IMongoObject
        {
            GetDatabase().GetCollection<T>(typeof(T).Name).Save(entity);
        }

        private T Get<T>(string name) where T : IMongoObject
        {
            Type t = typeof(T);

            // Throws
            return GetDatabase().GetCollection<T>(typeof(T).Name).AsQueryable().Where(o => o.Name == name).FirstOrDefault();
        }

        private MongoDatabase GetDatabase()
        {
            if (m_db == null)
            {
                var conString = "mongodb://localhost/MongoTest";
                MongoUrl url = new MongoUrl(conString);
                m_dbName = url.DatabaseName;
                m_db = MongoServer.Create(url);
            }

            return m_db.GetDatabase(m_dbName);
        }

        [Test]
        public void Run()
        {
            MongoObject o = new MongoObject();
            o.Name = "Foo";
            Save(o);

            MongoObject b = Get<MongoObject>("Foo");

            Assert.IsNotNull(b);
        }
    }
}
