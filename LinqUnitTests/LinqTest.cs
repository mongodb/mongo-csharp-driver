using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.DefaultSerializer;
using MongoDB.Driver;
using MongoDB.Linq;
using NUnit.Framework;

namespace LinqUnitTests
{
    [TestFixture]
    public class LinqExtensionsTests
    {
        private class Person
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public int Age { get; set; }

            public Address Address { get; set; }

            public string[] Aliases { get; set; }
        }

        private class Address
        {
            public string City { get; set; }
        }

        private class Organization
        {
            public string Name { get; set; }

            public Address Address { get; set; }
        }

        private MongoServer server;
        private MongoDatabase database;
        private MongoCollection<Person> personCollection;
        private MongoCollection<Organization> orgCollection;

        [TestFixtureSetUp]
        public void Setup()
        {
            server = MongoServer.Create();
            server.Connect();
            database = server["onlinetests"];
            personCollection = database.GetCollection<Person>("person");
            orgCollection = database.GetCollection<Organization>("orgs");
        }


        [SetUp]
        public void TestSetup()
        {
            personCollection.RemoveAll(SafeMode.True);
            personCollection.Insert(new Person { FirstName = "Bob", LastName = "McBob", Age = 42, Address = new Address { City = "London" }, Aliases = new[] { "Blub" } }, SafeMode.True);
            personCollection.Insert(new Person { FirstName = "Jane", LastName = "McJane", Age = 35, Address = new Address { City = "Paris" } }, SafeMode.True);
            personCollection.Insert(new Person { FirstName = "Joe", LastName = "McJoe", Age = 21, Address = new Address { City = "Chicago" } }, SafeMode.True);

            orgCollection.RemoveAll(SafeMode.True);
            orgCollection.Insert(new Organization { Name = "The Muffler Shanty", Address = new Address { City = "London" } }, SafeMode.True);
        }

        [Test]
        public void Delete()
        {
            personCollection.Delete(p => true);

            Assert.AreEqual(0, personCollection.Count());
        }

        [Test]
        public void Find()
        {
            var people = personCollection.Find(x => x.Age > 21);

            Assert.AreEqual(2, people.Count());
        }

        [Test]
        public void FindOne_WithAny()
        {
            var person = personCollection.FindOne(e => e.Aliases.Any(a => a == "Blub"));

            Assert.IsNotNull(person);
            Assert.AreEqual("Bob", person.FirstName);
        }

    }

}
