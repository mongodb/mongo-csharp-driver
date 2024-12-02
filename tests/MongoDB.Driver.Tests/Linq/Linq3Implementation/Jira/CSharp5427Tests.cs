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
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5427Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_Mql_Exists_with_property_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => Mql.Exists(x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : { $exists : true }  } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Where_Mql_Exists_with_shadow_property_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => Mql.Exists(Mql.Field(x, "X", new NullableSerializer<int>(Int32Serializer.Instance))));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : { $exists : true }  } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2, 3);
        }

        [Fact]
        public void Where_Mql_IsMissing_with_property_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => Mql.IsMissing(x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : { $exists : false }  } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Mql_IsMissing_with_shadow_property_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => Mql.IsMissing(Mql.Field(x, "X", new NullableSerializer<int>(Int32Serializer.Instance))));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : { $exists : false }  } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_Mql_IsNullOrMissing_with_property_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => Mql.IsNullOrMissing(x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : null  } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Where_Mql_IsNullOrMissing_with_shadow_property_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => Mql.IsNullOrMissing(Mql.Field(x, "X", new NullableSerializer<int>(Int32Serializer.Instance))));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { X : null } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection.Database.GetCollection<BsonDocument>("test"),
                BsonDocument.Parse("{ _id : 1 }"),
                BsonDocument.Parse("{ _id : 2, X : null }"),
                BsonDocument.Parse("{ _id : 3, X : 3 }"));
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int? X { get; set; }
        }
    }
}
