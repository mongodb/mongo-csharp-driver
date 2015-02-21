using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class FilterBuilderTests
    {
        [Test]
        public void EQ()
        {
            var subject = new FilterBuilder<BsonDocument>();

            Assert(subject.EQ("x", 10), "{x: 10}");
        }

        [Test]
        public void EQ_Typed()
        {
            var subject = new FilterBuilder<TypedTest>();
            Assert(subject.EQ(x => x.FirstName, "Jack"), "{ fn: 'Jack'}");
            Assert(subject.EQ("firstName", "Jim"), "{ firstName: 'Jim'}");
        }

        [Test]
        public void In()
        {
            var subject = new FilterBuilder<BsonDocument>();

            Assert(subject.In("x", new[] { 10, 20 }), "{ x: { $in: [10,20]}}");
        }

        [Test]
        public void In_Typed()
        {
            var subject = new FilterBuilder<TypedTest>();
            Assert(subject.In(x => x.FavoriteColors, new[] { "blue", "green" }), "{ colors: { $in: ['blue','green']}}");
            Assert(subject.In("favColors", new[] { "blue", "green" }), "{ favColors: { $in: ['blue','green']}}");
        }

        private void Assert<TDocument>(Filter<TDocument> filter, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedFilter = filter.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedFilter.Should().Be(expectedJson);
        }

        private class TypedTest
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("colors")]
            public string[] FavoriteColors { get; set; }
        }
    }
}
