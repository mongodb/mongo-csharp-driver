﻿/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4466Tests : Linq3IntegrationTest
    {
        [Fact]
        public void AppendStage_with_null_resultSerializer_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .AppendStage<C, D>("{ $project : { X : '$_id', _id : 0 } }", resultSerializer: null);

            var stages = Translate(collection, queryable, out var outputSerializer);

            AssertStages(stages, "{ $project : { X : '$_id', _id : 0 } }");
            outputSerializer.ValueType.Should().Be(typeof(D));

            var results = queryable.ToList();
            results.Select(x => x.X).Should().Equal(1, 2);
        }

        [Fact]
        public void AppendStage_with_resultSerializer_should_work()
        {
            var collection = CreateCollection();
            var resultSerializer = BsonSerializer.LookupSerializer<D>();

            var queryable =
                collection.AsQueryable()
                .AppendStage<C, D>("{ $project : { X : '$_id', _id : 0 } }", resultSerializer);

            var stages = Translate(collection, queryable, out var outputSerializer);

            AssertStages(stages, "{ $project : { X : '$_id', _id : 0 } }");
            outputSerializer.Should().BeSameAs(resultSerializer);

            var results = queryable.ToList();
            results.Select(x => x.X).Should().Equal(1, 2);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1 },
                new C { Id = 2 });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
        }

        private class D
        {
            public int X { get; set; }
        }
    }
}
