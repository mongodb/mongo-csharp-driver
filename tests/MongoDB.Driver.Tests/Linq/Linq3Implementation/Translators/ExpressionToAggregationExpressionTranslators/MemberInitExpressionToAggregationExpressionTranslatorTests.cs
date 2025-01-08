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
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    public class MemberInitExpressionToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Should_project_class_via_parameterless_constructor()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new SpawnDataClassParameterless
                {
                    Identifier = x.Id,
                    SpawnDate = x.Date,
                    SpawnText = x.Text
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact]
        public void Should_project_struct_via_parameterless_constructor()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new SpawnDataStructParameterless
                {
                    Identifier = x.Id,
                    SpawnDate = x.Date,
                    SpawnText = x.Text
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact]
        public void Should_project_class_via_constructor()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new SpawnDataClass(x.Id, x.Date)
                {
                    SpawnText = x.Text
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact]
        public void Should_project_struct_via_constructor()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new SpawnDataStruct(x.Id, x.Date)
                {
                    SpawnText = x.Text
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact]
        public void Should_project_via_constructor_with_inheritance()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable()
                .Select(x => new InheritedSpawnData(x.Id, x.Date)
                {
                    SpawnText = x.Text
                });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', SpawnText : '$Text', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.SpawnText.Should().Be("data text");
            results.Identifier.Should().Be(1);
        }

        [Fact] public void Should_project_to_class_with_additional_parameters()
        {
            CreateCollection();
            var collection = GetCollection<MyDataWithExtraField>("data");

            var queryable = collection.AsQueryable()
                .Select(d => new SpawnDataClassWithAdditionalParameter(d.Id, d.Date, d.AdditionalField));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Identifier : '$_id', SpawnDate : '$Date', AdditionalField: '$AdditionalField', _id : 0 } }");

            var results = queryable.Single();

            results.SpawnDate.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results.Identifier.Should().Be(1);
            results.AdditionalField.Should().Be(0);
        }

        private IMongoCollection<MyData> CreateCollection()
        {
            var collection = GetCollection<MyData>("data");

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

        public class MyDataWithExtraField
        {
            public int Id { get; set; }
            public DateTime Date;
            public string Text;

            public int AdditionalField;
        }

        public class SpawnDataClassParameterless
        {
            public int Identifier;
            public DateTime SpawnDate;
            public string SpawnText;
        }

        public struct SpawnDataStructParameterless
        {
            public int Identifier;
            public DateTime SpawnDate;
            public string SpawnText;
        }

        public class SpawnDataClass
        {
            [BsonElement] public readonly int Identifier;
            public DateTime SpawnDate;
            private string spawnText;

            [BsonConstructor]
            public SpawnDataClass(int identifier, DateTime spawnDate)
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

        public class SpawnDataClassWithAdditionalParameter
        {
            [BsonElement] public readonly int Identifier;
            public DateTime SpawnDate;
            public int AdditionalField;

            [BsonConstructor]
            public SpawnDataClassWithAdditionalParameter(int identifier, DateTime spawnDate, int additionalField)
            {
                Identifier = identifier;
                SpawnDate = spawnDate;
                AdditionalField = additionalField;
            }
        }

        public struct SpawnDataStruct
        {
            [BsonElement]
            public readonly int Identifier;
            public DateTime SpawnDate;
            private string spawnText;

            // this constructor is required for the test to compile
            [BsonConstructor]
            public SpawnDataStruct(int identifier, DateTime spawnDate)
            {
                Identifier = identifier;
                SpawnDate = spawnDate;
                spawnText = default;
            }

            // this constructor is required to be able to deserialize instances of this struct
            [BsonConstructor]
            public SpawnDataStruct(int identifier, DateTime spawnDate, string spawnText)
            {
                Identifier = identifier;
                SpawnDate = spawnDate;
                this.spawnText = spawnText;
            }

            public string SpawnText
            {
                get => spawnText;
                set => spawnText = value;
            }
        }

        public class InheritedSpawnData : SpawnDataClass
        {
            [BsonConstructor]
            public InheritedSpawnData(int identifier, DateTime spawnDate)
                : base(identifier, spawnDate)
            {
            }
        }
    }
}
