using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using NUnit.Framework;

namespace MongoDB.DriverUnitTests.EndToEndTests
{

    //NOTE: Without the change in the BsonClassMapSerializer this test will fail.
    [TestFixture]
    public class When_serializing_an_object_with_a_property_of_type_IEnumerable
    {
        private MongoCollection<BsonDocument> mongoCollection;

        [TestFixtureSetUp]
        public void Context()
        {
            var connectionStringUrl = new MongoUrl("mongodb://localhost/integration_tests");
            MongoDatabase mongoDatabase = MongoDatabase.Create(connectionStringUrl);
            mongoCollection = mongoDatabase.GetCollection("end_to_end_tests");
            if(mongoCollection.Exists())
            {
                mongoCollection.Drop();
            }

            var elementWithIEnumerableProperty = new ElementWithIEnumerableProperty{Name = "Test", Objects = new List<object>{"Test value for serialization"}.Select(x =>x)};
            mongoCollection.Insert(elementWithIEnumerableProperty);
        }

        [Test]
        public void Should_persist_the_content_of_the_property_so_that_it_can_be_desieralized_from_the_db()
        {

            var retrievedObject = mongoCollection.FindOneAs<ElementWithIEnumerableProperty>(Query.EQ("Name", BsonValue.Create("Test")));
            Assert.That(retrievedObject.Objects.Count(), Is.EqualTo(1));
            Assert.That(retrievedObject.Objects.Contains("Test value for serialization"));
        }
    }

    public class ElementWithIEnumerableProperty
    {
        public ObjectId Id { get; set; }

        public string  Name { get; set; }

        public IEnumerable<object> Objects { get; set; }
    }
}