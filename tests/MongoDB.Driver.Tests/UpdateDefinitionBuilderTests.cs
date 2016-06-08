/* Copyright 2010-2016 MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class UpdateDefinitionBuilderTests
    {
        [Fact]
        public void AddToSet()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.AddToSet("a", 1), "{$addToSet: {a: 1}}");
            Assert(subject.AddToSet("a", new[] { 1, 2 }), "{$addToSet: {a: [1, 2]}}");
        }

        [Fact]
        public void AddToSet_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.AddToSet(x => x.FavoriteColors, "green"), "{$addToSet: {colors: 'green'}}");
            Assert(subject.AddToSet("FavoriteColors", "green"), "{$addToSet: {colors: 'green'}}");
        }

        [Fact]
        public void AddToSetEach()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.AddToSetEach("a", new[] { 1, 2 }), "{$addToSet: {a: {$each: [1, 2]}}}");
            Assert(subject.AddToSetEach("a", new[] { new[] { 1, 2 }, new[] { 3, 4 } }), "{$addToSet: {a: {$each: [[1, 2], [3, 4]]}}}");
        }

        [Fact]
        public void AddToSetEach_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.AddToSetEach(x => x.FavoriteColors, new[] { "green", "violet" }), "{$addToSet: {colors: {$each: ['green', 'violet']}}}");
            Assert(subject.AddToSetEach("FavoriteColors", new[] { "green", "violet" }), "{$addToSet: {colors: {$each: ['green', 'violet']}}}");
        }

        [Fact]
        public void BitwiseAnd()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitwiseAnd("a", 1), "{$bit: {a: {and: 1}}}");
        }

        [Fact]
        public void BitwiseAnd_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitwiseAnd(x => x.Age, 1), "{$bit: {age: {and: 1}}}");
            Assert(subject.BitwiseAnd("Age", 1), "{$bit: {age: {and: 1}}}");
        }

        [Fact]
        public void BitwiseOr()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitwiseOr("a", 1), "{$bit: {a: {or: 1}}}");
        }

        [Fact]
        public void BitwiseOr_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitwiseOr(x => x.Age, 1), "{$bit: {age: {or: 1}}}");
            Assert(subject.BitwiseOr("Age", 1), "{$bit: {age: {or: 1}}}");
        }

        [Fact]
        public void BitwiseXor()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.BitwiseXor("a", 1), "{$bit: {a: {xor: 1}}}");
        }

        [Fact]
        public void BitwiseXor_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.BitwiseXor(x => x.Age, 1), "{$bit: {age: {xor: 1}}}");
            Assert(subject.BitwiseXor("Age", 1), "{$bit: {age: {xor: 1}}}");
        }

        [Fact]
        public void Combine()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine(
                "{$set: {a: 1, b: 2}}",
                "{$inc: {c: 1}}");

            Assert(update, "{$set: {a: 1, b: 2}, $inc: {c: 1}}");
        }

        [Fact]
        public void Combine_with_overlapping_operators()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine(
                "{$set: {a: 1, b: 2}}",
                "{$set: {c: 3}}");

            Assert(update, "{$set: {a: 1, b: 2, c: 3}}");
        }

        [Fact]
        public void Combine_with_overlapping_operators_and_duplicate_elements()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine(
                "{$set: {a: 1, b: 2}}",
                "{$set: {a: 4}}");

            Assert(update, "{$set: {a: 4, b: 2}}");
        }

        [Fact]
        public void Combine_with_overlapping_operators_and_duplicate_elements_using_extension_methods()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Set("a", 1).Set("b", 2).Set("a", 4);

            Assert(update, "{$set: {a: 4, b: 2}}");
        }

        [Fact]
        public void Combine_with_no_updates()
        {
            var subject = CreateSubject<BsonDocument>();

            var update = subject.Combine();

            Assert(update, "{ }");
        }

        [Fact]
        public void CurrentDate()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.CurrentDate("a"), "{$currentDate: {a: true}}");
        }

        [Fact]
        public void CurrentDate_with_date_type()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.CurrentDate("a", UpdateDefinitionCurrentDateType.Date), "{$currentDate: {a: {$type: 'date'}}}");
        }

        [Fact]
        public void CurrentDate_with_timestamp_type()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.CurrentDate("a", UpdateDefinitionCurrentDateType.Timestamp), "{$currentDate: {a: {$type: 'timestamp'}}}");
        }

        [Fact]
        public void CurrentDate_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.CurrentDate(x => x.LastUpdated), "{$currentDate: {last_updated: true}}");
            Assert(subject.CurrentDate("LastUpdated"), "{$currentDate: {last_updated: true}}");
        }

        [Fact]
        public void CurrentDate_Typed_with_date_type()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.CurrentDate(x => x.LastUpdated, UpdateDefinitionCurrentDateType.Date), "{$currentDate: {last_updated: {$type: 'date'}}}");
            Assert(subject.CurrentDate("LastUpdated", UpdateDefinitionCurrentDateType.Date), "{$currentDate: {last_updated: {$type: 'date'}}}");
        }

        [Fact]
        public void CurrentDate_Typed_with_timestamp_type()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.CurrentDate(x => x.LastUpdated, UpdateDefinitionCurrentDateType.Timestamp), "{$currentDate: {last_updated: {$type: 'timestamp'}}}");
            Assert(subject.CurrentDate("LastUpdated", UpdateDefinitionCurrentDateType.Timestamp), "{$currentDate: {last_updated: {$type: 'timestamp'}}}");
        }

        [Fact]
        public void Inc()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Inc("a", 1), "{$inc: {a: 1}}");
        }

        [Fact]
        public void Inc_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Inc(x => x.Age, 1), "{$inc: {age: 1}}");
            Assert(subject.Inc("Age", 1), "{$inc: {age: 1}}");
        }

        [Fact]
        public void Indexed_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Set(x => x.FavoriteColors[2], "yellow"), "{$set: {'colors.2': 'yellow'}}");
            Assert(subject.Set(x => x.Pets[2].Name, "Fluffencutters"), "{$set: {'pets.2.name': 'Fluffencutters'}}");
            Assert(subject.Set(x => x.Pets.ElementAt(2).Name, "Fluffencutters"), "{$set: {'pets.2.name': 'Fluffencutters'}}");

            var index = 2;
            Assert(subject.Set(x => x.FavoriteColors[index], "yellow"), "{$set: {'colors.2': 'yellow'}}");
            Assert(subject.Set(x => x.Pets[index].Name, "Fluffencutters"), "{$set: {'pets.2.name': 'Fluffencutters'}}");
            Assert(subject.Set(x => x.Pets.ElementAt(index).Name, "Fluffencutters"), "{$set: {'pets.2.name': 'Fluffencutters'}}");
        }

        [Fact]
        public void Indexed_Positional_Typed()
        {
            var subject = CreateSubject<Person>();

#pragma warning disable
            Assert(subject.Set(x => x.FavoriteColors[-1], "yellow"), "{$set: {'colors.$': 'yellow'}}");
#pragma warning restore
            Assert(subject.Set(x => x.Pets[-1].Name, "Fluffencutters"), "{$set: {'pets.$.name': 'Fluffencutters'}}");
            Assert(subject.Set(x => x.Pets.ElementAt(-1).Name, "Fluffencutters"), "{$set: {'pets.$.name': 'Fluffencutters'}}");
        }

        [Fact]
        public void Max()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Max("a", 1), "{$max: {a: 1}}");
        }

        [Fact]
        public void Max_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Max(x => x.Age, 1), "{$max: {age: 1}}");
            Assert(subject.Max("Age", 1), "{$max: {age: 1}}");
        }

        [Fact]
        public void Min()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Min("a", 1), "{$min: {a: 1}}");
        }

        [Fact]
        public void Min_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Min(x => x.Age, 1), "{$min: {age: 1}}");
            Assert(subject.Min("Age", 1), "{$min: {age: 1}}");
        }

        [Fact]
        public void Mul()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Mul("a", 2), "{$mul: {a: 2}}");
        }

        [Fact]
        public void Mul_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Mul(x => x.Age, 2), "{$mul: {age: 2}}");
            Assert(subject.Mul("Age", 2), "{$mul: {age: 2}}");
        }

        [Fact]
        public void PopFirst()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.PopFirst("a"), "{$pop: {a: -1}}");
        }

        [Fact]
        public void PopFirst_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.PopFirst(x => x.FavoriteColors), "{$pop: {colors: -1}}");
            Assert(subject.PopFirst("FavoriteColors"), "{$pop: {colors: -1}}");
        }

        [Fact]
        public void PopLast()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.PopLast("a"), "{$pop: {a: 1}}");
        }

        [Fact]
        public void PopLast_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.PopLast(x => x.FavoriteColors), "{$pop: {colors: 1}}");
            Assert(subject.PopLast("FavoriteColors"), "{$pop: {colors: 1}}");
        }

        [Fact]
        public void Pull()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Pull("a", 1), "{$pull: {a: 1}}");
            Assert(subject.Pull("a", new[] { 1, 2 }), "{$pull: {a: [1, 2]}}");
        }

        [Fact]
        public void Pull_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Pull(x => x.FavoriteColors, "green"), "{$pull: {colors: 'green'}}");
            Assert(subject.Pull("FavoriteColors", "green"), "{$pull: {colors: 'green'}}");
        }

        [Fact]
        public void PullAll()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.PullAll("a", new[] { 1, 2 }), "{$pullAll: {a: [1, 2]}}");
            Assert(subject.PullAll("a", new[] { new[] { 1, 2 }, new[] { 3, 4 } }), "{$pullAll: {a: [[1, 2], [3, 4]]}}");
        }

        [Fact]
        public void PullAll_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.PullAll(x => x.FavoriteColors, new[] { "green", "violet" }), "{$pullAll: {colors: ['green', 'violet']}}");
            Assert(subject.PullAll("FavoriteColors", new[] { "green", "violet" }), "{$pullAll: {colors: ['green', 'violet']}}");
        }

        [Fact]
        public void PullFilter()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.PullFilter<BsonDocument>("a", "{b: {$gt: 1}}"), "{$pull: {a: {b: {$gt: 1}}}}");
        }

        [Fact]
        public void PullFilter_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.PullFilter(x => x.Pets, x => x.Name == "Fluffy"), "{$pull: {pets: {name: 'Fluffy'}}}");
            Assert(subject.PullFilter<Pet>("Pets", "{ Name: 'Fluffy'}"), "{$pull: {pets: {Name: 'Fluffy'}}}");
        }

        [Fact]
        public void Push()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Push("a", 1), "{$push: {a: 1}}");
            Assert(subject.Push("a", new[] { 1, 2 }), "{$push: {a: [1, 2]}}");
        }

        [Fact]
        public void Push_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Push(x => x.FavoriteColors, "green"), "{$push: {colors: 'green'}}");
            Assert(subject.Push("FavoriteColors", "green"), "{$push: {colors: 'green'}}");
        }

        [Theory]
        [ParameterAttributeData]
        public void PushEach(
            [Values(null, 10)] int? slice,
            [Values(null, 20)] int? position,
            [Values(null, "{b: 1}")] string sort)
        {
            var subject = CreateSubject<BsonDocument>();

            var expectedPushValue = BsonDocument.Parse("{$each: [1, 2]}");
            if (slice.HasValue)
            {
                expectedPushValue.Add("$slice", slice.Value);
            }
            if (position.HasValue)
            {
                expectedPushValue.Add("$position", position.Value);
            }
            if (sort != null)
            {
                expectedPushValue.Add("$sort", BsonDocument.Parse(sort));
            }

            var expectedPush = new BsonDocument("$push", new BsonDocument("a", expectedPushValue));

            Assert(subject.PushEach("a", new[] { 1, 2 }, slice, position, sort), expectedPush);
        }

        [Theory]
        [ParameterAttributeData]
        public void PushEach_Typed(
            [Values(null, 10)] int? slice,
            [Values(null, 20)] int? position,
            [Values(null, "{b: 1}")] string sort)
        {
            var subject = CreateSubject<Person>();

            var expectedPushValue = BsonDocument.Parse("{$each: ['green', 'violet']}");
            if (slice.HasValue)
            {
                expectedPushValue.Add("$slice", slice.Value);
            }
            if (position.HasValue)
            {
                expectedPushValue.Add("$position", position.Value);
            }
            if (sort != null)
            {
                expectedPushValue.Add("$sort", BsonDocument.Parse(sort));
            }

            var expectedPush = new BsonDocument("$push", new BsonDocument("colors", expectedPushValue));

            Assert(subject.PushEach(x => x.FavoriteColors, new[] { "green", "violet" }, slice, position, sort), expectedPush);
        }

        [Fact]
        public void Rename()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Rename("a", "b"), "{$rename: {a: 'b'}}");
        }

        [Fact]
        public void Rename_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Rename(x => x.Age, "birthDate"), "{$rename: {age: 'birthDate'}}");
            Assert(subject.Rename("Age", "birthDate"), "{$rename: {age: 'birthDate'}}");
        }

        [Fact]
        public void Set()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Set("a", 1), "{$set: {a: 1}}");
        }

        [Fact]
        public void Set_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Set(x => x.Age, 1), "{$set: {age: 1}}");
            Assert(subject.Set("Age", 1), "{$set: {age: 1}}");
        }

        [Fact]
        public void Set_Typed_with_cast()
        {
            var subject = CreateSubject<Message>();

            Assert(subject.Set(x => ((SmsMessage)x).PhoneNumber, "1234567890"), "{$set: {pn: '1234567890'}}");

            var subject2 = CreateSubject<Person>();

            Assert(subject2.Set(x => ((SmsMessage)x.Message).PhoneNumber, "1234567890"), "{$set: {'m.pn': '1234567890'}}");
        }

        [Fact]
        public void Set_Typed_with_type_as()
        {
            var subject = CreateSubject<Message>();

            Assert(subject.Set(x => (x as SmsMessage).PhoneNumber, "1234567890"), "{$set: {pn: '1234567890'}}");

            var subject2 = CreateSubject<Person>();

            Assert(subject2.Set(x => (x.Message as SmsMessage).PhoneNumber, "1234567890"), "{$set: {'m.pn': '1234567890'}}");
        }

        [Fact]
        public void SetOnInsert()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.SetOnInsert("a", 1), "{$setOnInsert: {a: 1}}");
        }

        [Fact]
        public void SetOnInsert_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.SetOnInsert(x => x.Age, 1), "{$setOnInsert: {age: 1}}");
            Assert(subject.SetOnInsert("Age", 1), "{$setOnInsert: {age: 1}}");
        }

        [Fact]
        public void Unset()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Unset("a"), "{$unset: {a: 1}}");
        }

        [Fact]
        public void Unset_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Unset(x => x.Age), "{$unset: {age: 1}}");
            Assert(subject.Unset("Age"), "{$unset: {age: 1}}");
        }

        private void Assert<TDocument>(UpdateDefinition<TDocument> update, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedUpdate = update.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedUpdate.Should().Be(expected);
        }

        private void Assert<TDocument>(UpdateDefinition<TDocument> update, string expected)
        {
            Assert(update, BsonDocument.Parse(expected));
        }

        private UpdateDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new UpdateDefinitionBuilder<TDocument>();
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
            [BsonElement("pets")]
            public List<Pet> Pets { get; set; }

            [BsonElement("m")]
            public Message Message { get; set; }
        }

        private class Pet
        {
            [BsonElement("name")]
            public string Name { get; set; }
        }

        private abstract class Message
        {
        }

        private class SmsMessage : Message
        {
            [BsonElement("pn")]
            public string PhoneNumber { get; set; }
        }
    }
}
