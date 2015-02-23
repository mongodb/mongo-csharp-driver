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
        public void All()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.All("x", new[] { 10, 20 }), "{x: {$all: [10,20]}}");
        }

        [Test]
        public void All_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.All(x => x.FavoriteColors, new[] { "blue", "green" }), "{colors: {$all: ['blue','green']}}");
            Assert(subject.All("favColors", new[] { "blue", "green" }), "{favColors: {$all: ['blue','green']}}");
        }

        [Test]
        public void And()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.Equal("a", 1),
                subject.Equal("b", 2));

            Assert(filter, "{a: 1, b: 2}");
        }

        [Test]
        public void And_with_clashing_keys_should_get_promoted_to_dollar_form()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.Equal("a", 1),
                subject.Equal("a", 2));

            Assert(filter, "{$and: [{a: 1}, {a: 2}]}");
        }

        [Test]
        public void And_with_an_empty_filter()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                "{}",
                subject.Equal("a", 10));

            Assert(filter, "{a: 10}");
        }

        [Test]
        public void And_with_a_nested_and_should_get_flattened()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.And(
                subject.And("{a: 1}", new BsonDocument("b", 2)),
                subject.Equal("c", 3));

            Assert(filter, "{a: 1, b: 2, c: 3}");
        }

        [Test]
        public void ElemMatch()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.ElementMatch<BsonDocument>("a", "{b: 1}"), "{a: {$elemMatch: {b: 1}}}");
        }

        [Test]
        public void ElemMatch_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.ElementMatch<Pet[], Pet>("Pets", "{name: 'Fluffy'}"), "{Pets: {$elemMatch: {name: 'Fluffy'}}}");
            Assert(subject.ElementMatch<Pet[], Pet>(x => x.Pets, "{name: 'Fluffy'}"), "{pets: {$elemMatch: {name: 'Fluffy'}}}");
            Assert(subject.ElementMatch<Pet[], Pet>(x => x.Pets, x => x.Name == "Fluffy"), "{pets: {$elemMatch: {name: 'Fluffy'}}}");
        }

        [Test]
        public void Equal()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Equal("x", 10), "{x: 10}");
        }

        [Test]
        public void Equal_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Equal(x => x.FirstName, "Jack"), "{fn: 'Jack'}");
            Assert(subject.Equal("FirstName", "Jim"), "{FirstName: 'Jim'}");
        }

        [Test]
        public void Exists()
        {
            var subject = CreateSubject<BsonDocument>();
            Assert(subject.Exists("x"), "{x: {$exists: true}}");
            Assert(subject.Exists("x", false), "{x: {$exists: false}}");
        }

        [Test]
        public void Exists_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Exists(x => x.FirstName), "{fn: {$exists: true}}");
            Assert(subject.Exists("FirstName", false), "{FirstName: {$exists: false}}");
        }

        [Test]
        public void GreaterThan()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GreaterThan("x", 10), "{x: {$gt: 10}}");
        }

        [Test]
        public void GreaterThan_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.GreaterThan(x => x.Age, 10), "{age: {$gt: 10}}");
            Assert(subject.GreaterThan("Age", 10), "{Age: {$gt: 10}}");
        }

        [Test]
        public void GreaterThanOrEqual()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GreaterThanOrEqual("x", 10), "{x: {$gte: 10}}");
        }

        [Test]
        public void GreaterThanOrEqual_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.GreaterThanOrEqual(x => x.Age, 10), "{age: {$gte: 10}}");
            Assert(subject.GreaterThanOrEqual("Age", 10), "{Age: {$gte: 10}}");
        }

        [Test]
        public void In()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.In("x", new[] { 10, 20 }), "{x: {$in: [10,20]}}");
        }

        [Test]
        public void In_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.In(x => x.FavoriteColors, new[] { "blue", "green" }), "{colors: {$in: ['blue','green']}}");
            Assert(subject.In("favColors", new[] { "blue", "green" }), "{favColors: {$in: ['blue','green']}}");
        }

        [Test]
        public void LessThan()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.LessThan("x", 10), "{x: {$lt: 10}}");
        }

        [Test]
        public void LessThan_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.LessThan(x => x.Age, 10), "{age: {$lt: 10}}");
            Assert(subject.LessThan("Age", 10), "{Age: {$lt: 10}}");
        }

        [Test]
        public void LessThanOrEqual()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.LessThanOrEqual("x", 10), "{x: {$lte: 10}}");
        }

        [Test]
        public void LessThanOrEqual_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.LessThanOrEqual(x => x.Age, 10), "{age: {$lte: 10}}");
            Assert(subject.LessThanOrEqual("Age", 10), "{Age: {$lte: 10}}");
        }

        [Test]
        public void Not_with_and()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{$and: [{a: 1}, {b: 2}]}");

            Assert(filter, "{$nor: [{$and: [{a: 1}, {b: 2}]}]}");
        }

        [Test]
        public void Not_with_equal()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{a: 1}");

            Assert(filter, "{a: {$ne: 1}}");
        }

        [Test]
        public void Not_with_exists()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.Exists("a"));

            Assert(filter, "{a: {$exists: false}}");

            var filter2 = subject.Not(subject.Exists("a", false));

            Assert(filter2, "{a: {$exists: true}}");
        }

        [Test]
        public void Not_with_in()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.In("a", new[] { 10, 20 }));

            Assert(filter, "{a: {$nin: [10, 20]}}");
        }

        [Test]
        public void Not_with_not()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.Not("{a: 1}"));

            Assert(filter, "{a: 1}");
        }

        [Test]
        public void Not_with_not_equal()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{a: {$ne: 1}}");

            Assert(filter, "{a: 1}");
        }

        [Test]
        public void Not_with_not_in()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not(subject.NotIn("a", new[] { 10, 20 }));

            Assert(filter, "{a: {$in: [10, 20]}}");
        }

        [Test]
        public void Not_with_not_or()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{$nor: [{a: 1}, {b: 2}]}");

            Assert(filter, "{$or: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void Not_with_or()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Not("{$or: [{a: 1}, {b: 2}]}");

            Assert(filter, "{$nor: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void NotEqual()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.NotEqual("x", 10), "{x: {$ne: 10}}");
        }

        [Test]
        public void NotEqual_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.NotEqual(x => x.Age, 10), "{age: {$ne: 10}}");
            Assert(subject.NotEqual("Age", 10), "{Age: {$ne: 10}}");
        }

        [Test]
        public void NotIn()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.NotIn("x", new[] { 10, 20 }), "{x: {$nin: [10,20]}}");
        }

        [Test]
        public void NotIn_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.NotIn(x => x.FavoriteColors, new[] { "blue", "green" }), "{colors: {$nin: ['blue','green']}}");
            Assert(subject.NotIn("favColors", new[] { "blue", "green" }), "{favColors: {$nin: ['blue','green']}}");
        }

        [Test]
        public void Or()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Or(
                "{a: 1}",
                new BsonDocument("b", 2));

            Assert(filter, "{$or: [{a: 1}, {b: 2}]}");
        }

        [Test]
        public void Or_should_flatten_nested_ors()
        {
            var subject = CreateSubject<BsonDocument>();
            var filter = subject.Or(
                "{$or: [{a: 1}, {b: 2}]}",
                new BsonDocument("c", 3));

            Assert(filter, "{$or: [{a: 1}, {b: 2}, {c: 3}]}");
        }

        [Test]
        public void Regex()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Regex("x", "/abc/"), "{x: /abc/}");
        }

        [Test]
        public void Regex_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Regex(x => x.FirstName, "/abc/"), "{fn: /abc/}");
            Assert(subject.Regex("FirstName", "/abc/"), "{FirstName: /abc/}");
        }

        [Test]
        public void Size()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Size("x", 10), "{x: {$size: 10}}");
        }

        [Test]
        public void Size_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Size(x => x.FavoriteColors, 10), "{colors: {$size: 10}}");
            Assert(subject.Size("FavoriteColors", 10), "{FavoriteColors: {$size: 10}}");
        }

        [Test]
        public void Type()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Type("x", BsonType.String), "{x: {$type: 2}}");
        }

        [Test]
        public void Type_Typed()
        {
            var subject = CreateSubject<Person>();
            Assert(subject.Type(x => x.FirstName, BsonType.String), "{fn: {$type: 2}}");
            Assert(subject.Type("FirstName", BsonType.String), "{FirstName: {$type: 2}}");
        }

        private void Assert<TDocument>(Filter<TDocument> filter, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedFilter = filter.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedFilter.Should().Be(expectedJson);
        }

        private FilterBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new FilterBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("colors")]
            public string[] FavoriteColors { get; set; }

            [BsonElement("age")]
            public int Age { get; set; }

            [BsonElement("pets")]
            public Pet[] Pets { get; set; }
        }

        private class Pet
        {
            [BsonElement("name")]
            public string Name { get; set; }
        }
    }
}
