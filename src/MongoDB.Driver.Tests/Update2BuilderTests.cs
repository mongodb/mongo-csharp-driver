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

using System;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class Update2BuilderTests
    {
        [Test]
        public void BitwiseAnd()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitwiseAnd("a", 1), "{$bit: {a: {and: 1}}}");
        }

        [Test]
        public void BitwiseAnd_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitwiseAnd(x => x.Age, 1), "{$bit: {age: {and: 1}}}");
            Assert(subject.BitwiseAnd("Age", 1), "{$bit: {Age: {and: 1}}}");
        }

        [Test]
        public void BitwiseOr()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitwiseOr("a", 1), "{$bit: {a: {or: 1}}}");
        }

        [Test]
        public void BitwiseOr_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitwiseOr(x => x.Age, 1), "{$bit: {age: {or: 1}}}");
            Assert(subject.BitwiseOr("Age", 1), "{$bit: {Age: {or: 1}}}");
        }

        [Test]
        public void BitwiseXor()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitwiseXor("a", 1), "{$bit: {a: {xor: 1}}}");
        }

        [Test]
        public void BitwiseXor_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitwiseXor(x => x.Age, 1), "{$bit: {age: {xor: 1}}}");
            Assert(subject.BitwiseXor("Age", 1), "{$bit: {Age: {xor: 1}}}");
        }

        [Test]
        public void Combine()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine(
                "{$set: {a: 1, b: 2}}",
                "{$inc: {c: 1}}");

            Assert(update, "{$set: {a: 1, b: 2}, $inc: {c: 1}}");
        }

        [Test]
        public void Combine_with_overlapping_operators()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine(
                "{$set: {a: 1, b: 2}}",
                "{$set: {c: 3}}");

            Assert(update, "{$set: {a: 1, b: 2, c: 3}}");
        }

        [Test]
        public void Combine_with_overlapping_operators_and_duplicate_elements()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine(
                "{$set: {a: 1, b: 2}}",
                "{$set: {a: 4}}");

            Assert(update, "{$set: {a: 4, b: 2}}");
        }

        [Test]
        public void CurrentDate()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.CurrentDate("a"), "{$currentDate: {a: true}}");
        }

        [Test]
        public void CurrentDate_with_date_type()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.CurrentDate("a", Update2CurrentDateType.Date), "{$currentDate: {a: {$type: 'date'}}}");
        }

        [Test]
        public void CurrentDate_with_timestamp_type()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.CurrentDate("a", Update2CurrentDateType.Timestamp), "{$currentDate: {a: {$type: 'timestamp'}}}");
        }

        [Test]
        public void CurrentDate_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.CurrentDate(x => x.LastUpdated), "{$currentDate: {last_updated: true}}");
            Assert(subject.CurrentDate("LastUpdated"), "{$currentDate: {LastUpdated: true}}");
        }

        [Test]
        public void CurrentDate_Typed_with_date_type()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.CurrentDate(x => x.LastUpdated, Update2CurrentDateType.Date), "{$currentDate: {last_updated: {$type: 'date'}}}}");
            Assert(subject.CurrentDate("LastUpdated", Update2CurrentDateType.Date), "{$currentDate: {LastUpdated: {$type: 'date'}}}}");
        }

        [Test]
        public void CurrentDate_Typed_with_timestamp_type()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.CurrentDate(x => x.LastUpdated, Update2CurrentDateType.Timestamp), "{$currentDate: {last_updated: {$type: 'timestamp'}}}}");
            Assert(subject.CurrentDate("LastUpdated", Update2CurrentDateType.Timestamp), "{$currentDate: {LastUpdated: {$type: 'timestamp'}}}}");
        }

        [Test]
        public void Increment()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Increment("a", 1), "{$inc: {a: 1}}");
        }

        [Test]
        public void Increment_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Increment(x => x.Age, 1), "{$inc: {age: 1}}");
            Assert(subject.Increment("Age", 1), "{$inc: {Age: 1}}");
        }

        [Test]
        public void Max()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Max("a", 1), "{$max: {a: 1}}");
        }

        [Test]
        public void Max_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Max(x => x.Age, 1), "{$max: {age: 1}}");
            Assert(subject.Max("Age", 1), "{$max: {Age: 1}}");
        }

        [Test]
        public void Min()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Min("a", 1), "{$min: {a: 1}}");
        }

        [Test]
        public void Min_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Min(x => x.Age, 1), "{$min: {age: 1}}");
            Assert(subject.Min("Age", 1), "{$min: {Age: 1}}");
        }

        [Test]
        public void Multiply()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Multiply("a", 2), "{$mul: {a: 2}}");
        }

        [Test]
        public void Multiply_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Multiply(x => x.Age, 2), "{$mul: {age: 2}}");
            Assert(subject.Multiply("Age", 2), "{$mul: {Age: 2}}");
        }

        [Test]
        public void PopFirst()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.PopFirst("a"), "{$pop: {a: -1}}");
        }

        [Test]
        public void PopFirst_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.PopFirst(x => x.FavoriteColors), "{$pop: {colors: -1}}");
            Assert(subject.PopFirst("FavoriteColors"), "{$pop: {FavoriteColors: -1}}");
        }

        [Test]
        public void PopLast()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.PopLast("a"), "{$pop: {a: 1}}");
        }

        [Test]
        public void PopLast_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.PopLast(x => x.FavoriteColors), "{$pop: {colors: 1}}");
            Assert(subject.PopLast("FavoriteColors"), "{$pop: {FavoriteColors: 1}}");
        }

        [Test]
        public void Rename()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Rename("a", "b"), "{$rename: {a: 'b'}}");
        }

        [Test]
        public void Rename_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Rename(x => x.Age, "birthDate"), "{$rename: {age: 'birthDate'}}");
            Assert(subject.Rename("Age", "birthDate"), "{$rename: {Age: 'birthDate'}}");
        }

        [Test]
        public void Set()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Set("a", 1), "{$set: {a: 1}}");
        }

        [Test]
        public void Set_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Set(x => x.Age, 1), "{$set: {age: 1}}");
            Assert(subject.Set("Age", 1), "{$set: {Age: 1}}");
        }

        [Test]
        public void SetOnInsert()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.SetOnInsert("a", 1), "{$setOnInsert: {a: 1}}");
        }

        [Test]
        public void SetOnInsert_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.SetOnInsert(x => x.Age, 1), "{$setOnInsert: {age: 1}}");
            Assert(subject.SetOnInsert("Age", 1), "{$setOnInsert: {Age: 1}}");
        }

        [Test]
        public void Unset()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Unset("a"), "{$unset: {a: 1}}");
        }

        [Test]
        public void Unset_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Unset(x => x.Age), "{$unset: {age: 1}}");
            Assert(subject.Unset("Age"), "{$unset: {Age: 1}}");
        }

        private void Assert<TDocument>(Update2<TDocument> update, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedUpdate = update.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedUpdate.Should().Be(expectedJson);
        }

        private Update2Builder<TDocument> CreateSubject<TDocument>()
        {
            return new Update2Builder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("colors")]
            public string[] FavoriteColors { get; set; }

            [BsonElement("age")]
            public int Age { get; set; }

            [BsonElement("last_updated")]
            public DateTime LastUpdated { get; set; }
        }
    }
}
