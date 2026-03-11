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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class IsMatchMethodToAggregationExpressionTranslatorTests : LinqIntegrationTest<IsMatchMethodToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public IsMatchMethodToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture, server => server.Supports(Feature.RegexMatch))
        {
        }

        [Fact]
        public void Should_translate_instance_regex_isMatch()
        {
            var collection = Fixture.Collection;
            var regex = new Regex(@"\dB.*0");
            var queryable = collection.AsQueryable()
                .Where(i => regex.IsMatch(i.A + i.B));

            var stages = Translate(collection, queryable);
            AssertStages(stages, @"{ '$match' : { '$expr' : { '$regexMatch' : { 'input' : { '$concat' : ['$A', '$B'] }, 'regex' : '\\dB.*0', 'options' : '' } } } }");

            var result = queryable.Single();
            result.Id.Should().Be(2);
        }

        [Fact]
        public void Should_translate_static_regex_isMatch()
        {
            RequireServer.Check();

            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(i => Regex.IsMatch(i.A + i.B, @"\dB.*0"));

            var stages = Translate(collection, queryable);
            AssertStages(stages, @"{ '$match' : { '$expr' : { '$regexMatch' : { 'input' : { '$concat' : ['$A', '$B'] }, 'regex' : '\\dB.*0', 'options' : '' } } } }");

            var result = queryable.Single();
            result.Id.Should().Be(2);
        }

        [Fact]
        public void Should_translate_static_regex_isMatch_with_options()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Where(i => Regex.IsMatch(i.A + i.B, @"\dB.*0", RegexOptions.IgnoreCase));

            var stages = Translate(collection, queryable);
            AssertStages(stages, @"{ '$match' : { '$expr' : { '$regexMatch' : { 'input' : { '$concat' : ['$A', '$B'] }, 'regex' : '\\dB.*0', 'options' : 'i' } } } }");

            var result = queryable.Single();
            result.Id.Should().Be(2);
        }

        public class Data
        {
            public int Id { get; set; }
            public string A { get; set; }
            public string B { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Data>
        {
            protected override IEnumerable<Data> InitialData =>
            [
                new Data { Id = 1, A = "ABC", B = "1" },
                new Data { Id = 2, A = "1Br", B = "0" }
            ];
        }
    }
}
