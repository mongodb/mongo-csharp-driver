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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5067Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Where_with_bool_field_represented_as_boolean_should_work(
            [Values(false, true)] bool justField)
        {
            var collection = GetCollection();

            var queryable = justField ?
                collection.AsQueryable().Where(x => x.BoolFieldRepresentedAsBoolean) :
                collection.AsQueryable().Where(x => x.BoolFieldRepresentedAsBoolean == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolFieldRepresentedAsBoolean : true } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_bool_field_represented_as_int32_should_work(
            [Values(false, true)] bool justField)
        {
            var collection = GetCollection();

            var queryable = justField ?
                collection.AsQueryable().Where(x => x.BoolFieldRepresentedAsInt32) :
                collection.AsQueryable().Where(x => x.BoolFieldRepresentedAsInt32 == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolFieldRepresentedAsInt32 : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_bool_field_represented_as_string_should_work(
            [Values(false, true)] bool justField)
        {
            var collection = GetCollection();

            var queryable = justField ?
                collection.AsQueryable().Where(x => x.BoolFieldRepresentedAsString) :
                collection.AsQueryable().Where(x => x.BoolFieldRepresentedAsString == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolFieldRepresentedAsString : 'true' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_array_field_represented_as_booleans_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Where(x => x.BoolArrayFieldRepresentedAsBoolean.Any(p => p));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolArrayFieldRepresentedAsBoolean : true } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_array_field_represented_as_int32s_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Where(x => x.BoolArrayFieldRepresentedAsInt32.Any(p => p));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolArrayFieldRepresentedAsInt32 : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_array_field_represented_as_strings_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Where(x => x.BoolArrayFieldRepresentedAsString.Any(p => p));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolArrayFieldRepresentedAsString : 'true' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_bool_property_represented_as_boolean_should_work(
            [Values(false, true)] bool justProperty)
        {
            var collection = GetCollection();

            var queryable = justProperty ?
                collection.AsQueryable().Where(x => x.BoolPropertyRepresentedAsBoolean) :
                collection.AsQueryable().Where(x => x.BoolPropertyRepresentedAsBoolean == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolPropertyRepresentedAsBoolean : true } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_bool_property_represented_as_int32_should_work(
            [Values(false, true)] bool justProperty)
        {
            var collection = GetCollection();

            var queryable = justProperty ?
                collection.AsQueryable().Where(x => x.BoolPropertyRepresentedAsInt32) :
                collection.AsQueryable().Where(x => x.BoolPropertyRepresentedAsInt32 == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolPropertyRepresentedAsInt32 : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_bool_property_represented_as_string_should_work(
            [Values(false, true)] bool justProperty)
        {
            var collection = GetCollection();

            var queryable = justProperty ?
                collection.AsQueryable().Where(x => x.BoolPropertyRepresentedAsString) :
                collection.AsQueryable().Where(x => x.BoolPropertyRepresentedAsString == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolPropertyRepresentedAsString : 'true' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_array_property_represented_as_booleans_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsBoolean.Any(p => p));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolArrayPropertyRepresentedAsBoolean : true } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_array_property_represented_as_int32s_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsInt32.Any(p => p));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolArrayPropertyRepresentedAsInt32 : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_array_property_represented_as_strings_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsString.Any(p => p));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolArrayPropertyRepresentedAsString : 'true' } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Theory]
        [InlineData("property[0]", "{ $match : { 'BoolArrayPropertyRepresentedAsBoolean.0' : true } }", new[] { 2 })]
        [InlineData("property[0] == false", "{ $match : { 'BoolArrayPropertyRepresentedAsBoolean.0' : false } }", new[] { 1 })]
        [InlineData("property[0] == true", "{ $match : { 'BoolArrayPropertyRepresentedAsBoolean.0' : true } }", new[] { 2 })]
        [InlineData("!property[0]", "{ $match : { 'BoolArrayPropertyRepresentedAsBoolean.0' : { $ne : true } } }", new[] { 1 })]
        [InlineData("!(property[0] == false)", "{ $match : { 'BoolArrayPropertyRepresentedAsBoolean.0' : { $ne : false } } }", new[] { 2 })]
        [InlineData("!(property[0] == true)", "{ $match : { 'BoolArrayPropertyRepresentedAsBoolean.0' : { $ne : true } } }", new[] { 1 })]
        public void Where_with_expression_using_bool_array_property_represented_as_booleans_should_work(
            string expression,
            string expectedStage,
            int[] expectedResults)
        {
            var collection = GetCollection();

            var queryable = expression switch
            {
                "property[0]" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsBoolean[0]),
                "property[0] == false" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsBoolean[0] == false),
                "property[0] == true" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsBoolean[0] == true),
                "!property[0]" => collection.AsQueryable().Where(x => !x.BoolArrayPropertyRepresentedAsBoolean[0]),
                "!(property[0] == false)" => collection.AsQueryable().Where(x => !(x.BoolArrayPropertyRepresentedAsBoolean[0] == false)),
                "!(property[0] == true)" => collection.AsQueryable().Where(x => !(x.BoolArrayPropertyRepresentedAsBoolean[0] == true)),
                _ => throw new Exception()
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData("property[0]", "{ $match : { 'BoolArrayPropertyRepresentedAsInt32.0' : 1 } }", new[] { 2 })]
        [InlineData("property[0] == false", "{ $match : { 'BoolArrayPropertyRepresentedAsInt32.0' : { $ne : 1 } } }", new[] { 1 })]
        [InlineData("property[0] == true", "{ $match : { 'BoolArrayPropertyRepresentedAsInt32.0' : 1 } }", new[] { 2 })]
        [InlineData("!property[0]", "{ $match : { 'BoolArrayPropertyRepresentedAsInt32.0' : { $ne : 1 } } }", new[] { 1 })]
        [InlineData("!(property[0] == false)", "{ $match : { 'BoolArrayPropertyRepresentedAsInt32.0' : 1 } }", new[] { 2 })]
        [InlineData("!(property[0] == true)", "{ $match : { 'BoolArrayPropertyRepresentedAsInt32.0' : { $ne : 1 } } }", new[] { 1 })]
        public void Where_with_expression_using_bool_array_property_represented_as_int32s_should_work(
            string expression,
            string expectedStage,
            int[] expectedResults)
        {
            var collection = GetCollection();

            var queryable = expression switch
            {
                "property[0]" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsInt32[0]),
                "property[0] == false" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsInt32[0] == false),
                "property[0] == true" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsInt32[0] == true),
                "!property[0]" => collection.AsQueryable().Where(x => !x.BoolArrayPropertyRepresentedAsInt32[0]),
                "!(property[0] == false)" => collection.AsQueryable().Where(x => !(x.BoolArrayPropertyRepresentedAsInt32[0] == false)),
                "!(property[0] == true)" => collection.AsQueryable().Where(x => !(x.BoolArrayPropertyRepresentedAsInt32[0] == true)),
                _ => throw new Exception()
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData("property[0]", "{ $match : { 'BoolArrayPropertyRepresentedAsString.0' : 'true' } }", new[] { 2 })]
        [InlineData("property[0] == false", "{ $match : { 'BoolArrayPropertyRepresentedAsString.0' : { $ne : 'true' } } }", new[] { 1 })]
        [InlineData("property[0] == true", "{ $match : { 'BoolArrayPropertyRepresentedAsString.0' : 'true' } }", new[] { 2 })]
        [InlineData("!property[0]", "{ $match : { 'BoolArrayPropertyRepresentedAsString.0' : { $ne : 'true' } } }", new[] { 1 })]
        [InlineData("!(property[0] == false)", "{ $match : { 'BoolArrayPropertyRepresentedAsString.0' : 'true' } }", new[] { 2 })]
        [InlineData("!(property[0] == true)", "{ $match : { 'BoolArrayPropertyRepresentedAsString.0' : { $ne : 'true' } } }", new[] { 1 })]
        public void Where_with_expression_using_bool_array_property_represented_as_strings_should_work(
            string expression,
            string expectedStage,
            int[] expectedResults)
        {
            var collection = GetCollection();

            var queryable = expression switch
            {
                "property[0]" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsString[0]),
                "property[0] == false" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsString[0] == false),
                "property[0] == true" => collection.AsQueryable().Where(x => x.BoolArrayPropertyRepresentedAsString[0] == true),
                "!property[0]" => collection.AsQueryable().Where(x => !x.BoolArrayPropertyRepresentedAsString[0]),
                "!(property[0] == false)" => collection.AsQueryable().Where(x => !(x.BoolArrayPropertyRepresentedAsString[0] == false)),
                "!(property[0] == true)" => collection.AsQueryable().Where(x => !(x.BoolArrayPropertyRepresentedAsString[0] == true)),
                _ => throw new Exception()
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(expectedResults);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C
                {
                    Id = 1,
                    BoolArrayFieldRepresentedAsBoolean = new[] { false },
                    BoolArrayFieldRepresentedAsInt32 = new[] { false },
                    BoolArrayFieldRepresentedAsString = new[] { false },
                    BoolArrayPropertyRepresentedAsBoolean = new[] { false },
                    BoolArrayPropertyRepresentedAsInt32 = new[] { false },
                    BoolArrayPropertyRepresentedAsString = new[] { false },
                    BoolFieldRepresentedAsBoolean = false,
                    BoolFieldRepresentedAsInt32 = false,
                    BoolFieldRepresentedAsString = false,
                    BoolPropertyRepresentedAsBoolean = false,
                    BoolPropertyRepresentedAsInt32 = false,
                    BoolPropertyRepresentedAsString = false
                },
                new C {
                    Id = 2,
                    BoolArrayFieldRepresentedAsBoolean = new[] { true },
                    BoolArrayFieldRepresentedAsInt32 = new[] { true },
                    BoolArrayFieldRepresentedAsString = new[] { true },
                    BoolArrayPropertyRepresentedAsBoolean = new[] { true },
                    BoolArrayPropertyRepresentedAsInt32 = new[] { true },
                    BoolArrayPropertyRepresentedAsString = new[] { true },
                    BoolFieldRepresentedAsBoolean = true,
                    BoolFieldRepresentedAsInt32 = true,
                    BoolFieldRepresentedAsString = true,
                    BoolPropertyRepresentedAsBoolean = true,
                    BoolPropertyRepresentedAsInt32 = true,
                    BoolPropertyRepresentedAsString = true
                });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public bool[] BoolArrayFieldRepresentedAsBoolean;
            [BsonRepresentation(BsonType.Int32)] public bool[] BoolArrayFieldRepresentedAsInt32;
            [BsonRepresentation(BsonType.String)] public bool[] BoolArrayFieldRepresentedAsString;
            public bool[] BoolArrayPropertyRepresentedAsBoolean;
            [BsonRepresentation(BsonType.Int32)] public bool[] BoolArrayPropertyRepresentedAsInt32;
            [BsonRepresentation(BsonType.String)] public bool[] BoolArrayPropertyRepresentedAsString;
            public bool BoolFieldRepresentedAsBoolean;
            [BsonRepresentation(BsonType.Int32)] public bool BoolFieldRepresentedAsInt32;
            [BsonRepresentation(BsonType.String)] public bool BoolFieldRepresentedAsString;
            public bool BoolPropertyRepresentedAsBoolean { get; set; }
            [BsonRepresentation(BsonType.Int32)] public bool BoolPropertyRepresentedAsInt32 { get; set; }
            [BsonRepresentation(BsonType.String)] public bool BoolPropertyRepresentedAsString { get; set; }
        }
    }
}
