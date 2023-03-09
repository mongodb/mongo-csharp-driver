/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4517Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_with_comparison_of_different_types_should_throw()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Where(x => x.Id == 1);

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because operand types are not compatible with each other");
        }

        [Fact]
        public void Expression_with_comparison_of_different_types_should_throw()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Select(x => new { R = x.Id == 1 });

            var exception = Record.Exception(() => Translate(collection, queryable));

            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("because operand types are not compatible with each other");
        }

        private IMongoCollection<MyDocument> CreateCollection()
        {
            var collection = GetCollection<MyDocument>("test");
            return collection;
        }

        public class MyDocument
        {
            public MyId Id { get; set; }
            public string Name { get; set; }
        }

        [BsonSerializer(typeof(MyIdSerializer))]
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
        public class MyId
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
        {

            public MyId(int id)
            {
                Id = id;
            }

            public int Id { get; }

            public static bool operator ==(int id, MyId other) => id == other.Id;
            public static bool operator ==(MyId id, int other) => id.Id == other;
            public static bool operator !=(int id, MyId other) => !(id == other);
            public static bool operator !=(MyId id, int other) => !(id == other);
        }

        public class MyIdSerializer : SerializerBase<MyId>
        {
            public override MyId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
            {
                return new MyId(context.Reader.ReadInt32());
            }

            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MyId value)
            {
                context.Writer.WriteInt32(value.Id);
            }
        }
    }
}
