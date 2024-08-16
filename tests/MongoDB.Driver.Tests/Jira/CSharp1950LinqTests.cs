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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp1950LinqTests : Linq3IntegrationTest
    {
        [Fact]
        public void StringIn_with_no_arguments_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn()),
                "{ $match : { S : { $in : [] } } }");
        }

        [Fact]
        public void StringIn_with_empty_array_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new StringOrRegularExpression[0])),
                "{ $match : { S : { $in : [] } } }");
        }

        [Fact]
        public void StringIn_with_array_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new BsonRegularExpression("^a"))),
                "{ $match : { S : { $in : [/^a/] } } }",
                "a1", "a2");
        }

        [Fact]
        public void StringIn_with_array_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn("b3")),
                "{ $match : { S : { $in : ['b3'] } } }",
                "b3");
        }

        [Fact]
        public void StringIn_with_array_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new BsonRegularExpression("^a"), "b3")),
                "{ $match : { S : { $in : [/^a/, 'b3'] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void StringIn_with_array_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$"))),
               "{ $match : { S : { $in : [/^a/, /^b3$/] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void StringIn_with_array_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn("a1", "b3")),
                "{ $match : { S : { $in : ['a1', 'b3'] } } }",
                "a1", "b3");
        }

        [Fact]
        public void StringIn_with_empty_list_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new List<StringOrRegularExpression>())),
                "{ $match : { S : { $in : [] } } }");
        }

        [Fact]
        public void StringIn_with_list_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new List<StringOrRegularExpression> { new BsonRegularExpression("^a") })),
                "{ $match : { S : { $in : [/^a/] } } }",
                "a1", "a2");
        }

        [Fact]
        public void StringIn_with_list_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new List<StringOrRegularExpression> { "b3" })),
                "{ $match : { S : { $in : ['b3'] } } }",
                "b3");
        }

        [Fact]
        public void StringIn_with_list_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), "b3" })),
                "{ $match : { S : { $in : [/^a/, 'b3'] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void StringIn_with_list_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$") })),
                "{ $match : { S : { $in : [/^a/, /^b3$/] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void StringIn_with_list_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringIn(new List<StringOrRegularExpression> { "a1", "b3" })),
                "{ $match : { S : { $in : ['a1', 'b3'] } } }",
                "a1", "b3");
        }

        [Fact]
        public void StringNin_with_no_arguments_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin()),
                "{ $match : { S : { $nin : [] } } }",
                "a1", "a2", "b3", "b4");
        }

        [Fact]
        public void StringNin_with_empty_array_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new StringOrRegularExpression[0])),
                "{ $match : { S : { $nin : [] } } }",
                "a1", "a2", "b3", "b4");
        }

        [Fact]
        public void StringNin_with_array_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new BsonRegularExpression("^a"))),
                "{ $match : { S : { $nin : [/^a/] } } }",
                "b3", "b4");
        }

        [Fact]
        public void StringNin_with_array_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin("b3")),
                "{ $match : { S : { $nin : ['b3'] } } }",
                "a1", "a2", "b4");
        }

        [Fact]
        public void StringNin_with_array_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new BsonRegularExpression("^a"), "b3")),
                "{ $match : { S : { $nin : [/^a/, 'b3'] } } }",
                "b4");
        }

        [Fact]
        public void StringNin_with_array_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$"))),
                "{ $match : { S : { $nin : [/^a/, /^b3$/] } } }",
                "b4");
        }

        [Fact]
        public void StringNin_with_array_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin("a1", "b3")),
                "{ $match : { S : { $nin : ['a1', 'b3'] } } }",
                "a2", "b4");
        }

        [Fact]
        public void StringNin_with_empty_list_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new List<StringOrRegularExpression>())),
                "{ $match : { S : { $nin : [] } } }",
                "a1", "a2", "b3", "b4");
        }

        [Fact]
        public void StringNin_with_list_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new List<StringOrRegularExpression> { new BsonRegularExpression("^a") })),
                "{ $match : { S : { $nin : [/^a/] } } }",
                "b3", "b4");
        }

        [Fact]
        public void StringNin_with_list_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new List<StringOrRegularExpression> { "b3" })),
                "{ $match : { S : { $nin : ['b3'] } } }",
                "a1", "a2", "b4");
        }

        [Fact]
        public void StringNin_with_list_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), "b3" })),
                "{ $match : { S : { $nin : [/^a/, 'b3'] } } }",
                "b4");
        }

        [Fact]
        public void StringNin_with_list_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$") })),
                "{ $match : { S : { $nin : [/^a/, /^b3$/] } } }",
                "b4");
        }

        [Fact]
        public void StringNin_with_list_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.S.StringNin(new List<StringOrRegularExpression> { "a1", "b3" })),
                "{ $match : { S : { $nin : ['a1', 'b3'] } } }",
                "a2", "b4");
        }

        [Fact]
        public void AnyStringIn_with_no_arguments_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn()),
                "{ $match : { SA : { $in : [] } } }");
        }

        [Fact]
        public void AnyStringIn_with_empty_array_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new StringOrRegularExpression[0])),
                "{ $match : { SA : { $in : [] } } }");
        }

        [Fact]
        public void AnyStringIn_with_array_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new BsonRegularExpression("^a"))),
                "{ $match : { SA : { $in : [/^a/] } } }",
                "a1", "a2");
        }

        [Fact]
        public void AnyStringIn_with_array_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn("b3")),
                "{ $match : { SA : { $in : ['b3'] } } }",
                "b3");
        }

        [Fact]
        public void AnyStringIn_with_array_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new BsonRegularExpression("^a"), "b3")),
                "{ $match : { SA : { $in : [/^a/, 'b3'] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void AnyStringIn_with_array_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$"))),
                "{ $match : { SA : { $in : [/^a/, /^b3$/] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void AnyStringIn_with_array_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn("a1", "b3")),
                "{ $match : { SA : { $in : ['a1', 'b3'] } } }",
                "a1", "b3");
        }

        [Fact]
        public void AnyStringIn_with_empty_list_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new List<StringOrRegularExpression>())),
                "{ $match : { SA : { $in : [] } } }");
        }

        [Fact]
        public void AnyStringIn_with_list_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new List<StringOrRegularExpression> { new BsonRegularExpression("^a") })),
                "{ $match : { SA : { $in : [/^a/] } } }",
                "a1", "a2");
        }

        [Fact]
        public void AnyStringIn_with_list_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new List<StringOrRegularExpression> { "b3" })),
                "{ $match : { SA : { $in : ['b3'] } } }",
                "b3");
        }

        [Fact]
        public void AnyStringIn_with_list_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), "b3" })),
                "{ $match : { SA : { $in : [/^a/, 'b3'] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void AnyStringIn_with_list_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$") })),
                "{ $match : { SA : { $in : [/^a/, /^b3$/] } } }",
                "a1", "a2", "b3");
        }

        [Fact]
        public void AnyStringIn_with_list_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringIn(new List<StringOrRegularExpression> { "a1", "b3" })),
                "{ $match : { SA : { $in : ['a1', 'b3'] } } }",
                "a1", "b3");
        }

        [Fact]
        public void AnyStringNin_with_no_arguments_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin()),
                "{ $match : { SA : { $nin : [] } } }",
                "a1", "a2", "b3", "b4");
        }

        [Fact]
        public void AnyStringNin_with_empty_array_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new StringOrRegularExpression[0])),
                "{ $match : { SA : { $nin : [] } } }",
                "a1", "a2", "b3", "b4");
        }

        [Fact]
        public void AnyStringNin_with_array_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new BsonRegularExpression("^a"))),
                "{ $match : { SA : { $nin : [/^a/] } } }",
                "b3", "b4");
        }

        [Fact]
        public void AnyStringNin_with_array_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin("b3")),
                "{ $match : { SA : { $nin : ['b3'] } } }",
                "a1", "a2", "b4");
        }

        [Fact]
        public void AnyStringNin_with_array_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new BsonRegularExpression("^a"), "b3")),
                "{ $match : { SA : { $nin : [/^a/, 'b3'] } } }",
                "b4");
        }

        [Fact]
        public void AnyStringNin_with_array_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$"))),
                "{ $match : { SA : { $nin : [/^a/, /^b3$/] } } }",
                "b4");
        }

        [Fact]
        public void AnyStringNin_with_array_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin("a1", "b3")),
                "{ $match : { SA : { $nin : ['a1', 'b3'] } } }",
                "a2", "b4");
        }

        [Fact]
        public void AnyStringNin_with_empty_list_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new List<StringOrRegularExpression>())),
                "{ $match : { SA : { $nin : [] } } }",
                "a1", "a2", "b3", "b4");
        }

        [Fact]
        public void AnyStringNin_with_list_of_one_regex_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new List<StringOrRegularExpression> { new BsonRegularExpression("^a") })),
                "{ $match : { SA : { $nin : [/^a/] } } }",
                "b3", "b4");
        }

        [Fact]
        public void AnyStringNin_with_list_of_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new List<StringOrRegularExpression> { "b3" })),
                "{ $match : { SA : { $nin : ['b3'] } } }",
                "a1", "a2", "b4");
        }

        [Fact]
        public void AnyStringNin_with_list_of_one_regex_and_one_string_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), "b3" })),
                "{ $match : { SA : { $nin : [/^a/, 'b3'] } } }",
                "b4");
        }

        [Fact]
        public void AnyStringNin_with_list_of_two_regexes_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new List<StringOrRegularExpression> { new BsonRegularExpression("^a"), new BsonRegularExpression("^b3$") })),
                "{ $match : { SA : { $nin : [/^a/, /^b3$/] } } }",
                "b4");
        }

        [Fact]
        public void AnyStringNin_with_list_of_two_strings_should_work()
        {
            Assert(
                queryable => queryable.Where(x => x.SA.AnyStringNin(new List<StringOrRegularExpression> { "a1", "b3" })),
                "{ $match : { SA : { $nin : ['a1', 'b3'] } } }",
                "a2", "b4");
        }

        private void Assert(
            Func<IQueryable<C>, IQueryable<C>> queryableFactory,
            string expectedStage,
            params string[] expectedResults)
        {
            var collection = CreateCollection();
            var queryable = queryableFactory(collection.AsQueryable());

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Select(x => x.S).Should().Equal(expectedResults);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();
            CreateCollection(
                collection,
                new C { Id = 1, S = "a1", SA = new[] { "a1" } },
                new C { Id = 2, S = "a2", SA = new[] { "a2" } },
                new C { Id = 3, S = "b3", SA = new[] { "b3" } },
                new C { Id = 4, S = "b4", SA = new[] { "b4" } });
            return collection;
        }

        public class C
        {
            public int Id { get; set; }
            public string S { get; set; }
            public string[] SA { get; set; }
        }
    }
}
