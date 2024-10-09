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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5295Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Subtract_two_decimals_with_decimal_representation_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.D2 - x.D1);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $subtract : ['$D2', '$D1'] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(1.0M);
        }

        [Fact]
        public void Subtract_two_decimals_with_left_string_representation_should_throw()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.DS2 - x.D1);

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Be("Expression not supported: (x.DS2 - x.D1) because x.DS2 uses a non-numeric representation: String.");
        }

        [Fact]
        public void Subtract_two_decimals_with_right_string_representation_should_throw()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => x.D2 - x.DS1);

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Be("Expression not supported: (x.D2 - x.DS1) because x.DS1 uses a non-numeric representation: String.");
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, D1 = 1.0M, D2 = 2.0M, DS1 = 3.0M, DS2 = 4.0M });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal D1 { get; set; }
            [BsonRepresentation(BsonType.Decimal128)] public decimal D2 { get; set; }
            [BsonRepresentation(BsonType.String)] public decimal DS1 { get; set; }
            [BsonRepresentation(BsonType.String)] public decimal DS2 { get; set; }
        }
    }
}
