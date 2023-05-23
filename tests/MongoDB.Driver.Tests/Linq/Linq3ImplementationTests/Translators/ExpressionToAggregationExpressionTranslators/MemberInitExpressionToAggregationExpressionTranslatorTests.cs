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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Translators.ExpressionToAggregationExpressionTranslators
{
    public class MemberInitExpressionToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        private readonly IMongoCollection<MyData> _collection;

        public MemberInitExpressionToAggregationExpressionTranslatorTests()
        {
            _collection = CreateCollection(LinqProvider.V3);
        }

        [Fact]
        public void Should_project_via_parameterless_constructor()
        {
            var queryable = _collection.AsQueryable()
                .Select(x => new SpawnDataParameterless
                {
                    Identifier = x.Id,
                    SpawnDate = x.Date,
                    SpawnText = x.Text
                });

            var stages = Translate(_collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact]
        public void Should_project_via_constructor()
        {
            var queryable = _collection.AsQueryable()
                .Select(x => new SpawnData(x.Id, x.Date)
                {
                    SpawnText = x.Text
                });

            var stages = Translate(_collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact]
        public void Should_project_via_constructor_with_inheritance()
        {
            var queryable = _collection.AsQueryable()
                .Select(x => new InheritedSpawnData(x.Id, x.Date)
                {
                    SpawnText = x.Text
                });

            var stages = Translate(_collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        private IMongoCollection<MyData> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<MyData>("data", linqProvider);

            CreateCollection(
                collection,
                new MyData { Id = 1, Date = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc), Text = "data text" });

            return collection;
        }

        public class MyData
        {
            public int Id { get; set; }
            public DateTime Date;
            public string Text;
        }

        public class SpawnDataParameterless
        {
            public int Identifier;
            public DateTime SpawnDate;
            public string SpawnText;
        }

        public class SpawnData
        {
            public readonly int Identifier;
            public DateTime SpawnDate;
            private string spawnText;

            public SpawnData(int identifier, DateTime spawnDate)
            {
                Identifier = identifier;
                SpawnDate = spawnDate;
            }

            public string SpawnText
            {
                get => spawnText;
                set => spawnText = value;
            }
        }

        public class InheritedSpawnData : SpawnData
        {
            public InheritedSpawnData(int identifier, DateTime spawnDate)
                : base(identifier, spawnDate)
            {
            }
        }
    }
}
