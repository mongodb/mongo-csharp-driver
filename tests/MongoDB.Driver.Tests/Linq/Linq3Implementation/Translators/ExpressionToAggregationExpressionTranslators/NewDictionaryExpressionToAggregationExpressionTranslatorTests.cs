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

#if NET6_0_OR_GREATER || NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    public class NewDictionaryExpressionToAggregationExpressionTranslatorTests : LinqIntegrationTest<NewDictionaryExpressionToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public NewDictionaryExpressionToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void NewDictionary_with_KeyValuePairs_should_translate()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(d => new Dictionary<string, string>(
                    new[] { new KeyValuePair<string, string>("A", d.A), new KeyValuePair<string, string>("B", d.B) }));

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { A : '$A', B: '$B' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(new Dictionary<string, string>{ ["A"] = "a", ["B"] = "b" });
        }

        [Fact]
        public void NewDictionary_with_KeyValuePairs_Create_should_translate()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(d => new Dictionary<string, string>(
                    new[] { KeyValuePair.Create("A", d.A), KeyValuePair.Create("B", d.B) }));

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { A : '$A', B: '$B' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(new Dictionary<string, string>{ ["A"] = "a", ["B"] = "b" });
        }

        [Fact]
        public void NewDictionary_with_KeyValuePairs_should_translate_Guid_as_string_key()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(d => new Dictionary<Guid, string>(
                    new[] { new KeyValuePair<Guid, string>(d.GuidAsString, d.A) }));

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $arrayToObject : [[{ k : '$GuidAsString', v : '$A' }]] }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(new Dictionary<Guid, string>{ [Guid.Parse("3E9AE467-9705-4C17-9655-EE7730BCC2EE")] = "a" });
        }


        [Fact]
        public void NewDictionary_with_KeyValuePairs_should_translate_dynamic_array()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(d => new Dictionary<string, string>(
                    d.Items.Select(i => new KeyValuePair<string, string>(i.P, i.W))));

            var stages = Translate(collection, queryable);

            AssertStages(stages, "{ $project : { _v : { $arrayToObject : { $map: { input: '$Items', as: 'i', in: { k: '$$i.P', v: '$$i.W' } } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(new Dictionary<string, string>{ ["x"] = "y" });
        }

        [Fact]
        public void NewDictionary_with_KeyValuePairs_throws_on_non_string_key()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(d => new Dictionary<int, string>(
                    new[] { new KeyValuePair<int, string>(42, d.A) }));

            var exception = Record.Exception(() => queryable.ToList());

            exception.Should().NotBeNull();
            exception.Should().BeOfType<ExpressionNotSupportedException>();
        }

        public class C
        {
            public string A { get; set; }

            public string B { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Guid GuidAsString { get; set; }

            public Item[] Items { get; set; }
        }

        public class Item
        {
            public string P { get; set; }

            public string W { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C
                {
                    A = "a",
                    B = "b",
                    GuidAsString = Guid.Parse("3E9AE467-9705-4C17-9655-EE7730BCC2EE"),
                    Items = [ new Item { P = "x", W = "y" } ]
                },
            ];
        }
    }
}
#endif
