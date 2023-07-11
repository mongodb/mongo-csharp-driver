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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    public class ConvertExpressionToFilterFieldTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Projection_using_convert_to_derived_class_with_field_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(p => new DerivedClass
                {
                    Id = p.Id,
                    A = ((DerivedClass)p).A.ToUpper()
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', A : { '$toUpper' : '$A' } } }");

            var result = queryable.Single();
            result.A.Should().Be("TEST VALUE");
        }

        [Fact]
        public void Projection_using_convert_to_derived_class_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(p => new DerivedClass()
                {
                    Id = p.Id,
                    A = ((DerivedClass)p).A
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ '$project' : { _id : '$_id', A : '$A' } }");

            var result = queryable.Single();
            result.A.Should().Be("test value");
        }

        [Fact]
        public void Filter_using_string_as_objectid_should_work()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable()
                // do not remove this extra unnecessary convert to string, it's important to reproduce the issue
                .Where(i => ((string)i.Id) == "000000000000000000000001");

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { _id : ObjectId('000000000000000000000001') } }");

            var result = queryable.Single();
            result.Id.Should().Be("000000000000000000000001");
        }

        private IMongoCollection<BaseClass> GetCollection()
        {
            var collection = GetCollection<BaseClass>("test");
            CreateCollection(collection, new DerivedClass()
            {
                Id = "000000000000000000000001",
                A = "test value"
            });
            return collection;
        }

        private class BaseClass
        {
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }
        }

        private class DerivedClass : BaseClass
        {
            public string A { get; set; }
        }
    }
}
