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
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3283Tests
    {
        [Fact]
        public void IndexOf_with_startIndex_should_result_in_expected_filter()
        {
            var subject = CreateSubject();

            var queryable = subject.Where(p => p.Name.IndexOf("John", 3) == 0);
            var pipeline = Translate(queryable);

            var expectedPipeline = new[]
            {
                "{ $match : { Name : /^.{3}John/s } }"
            };
            pipeline.Should().Equal(expectedPipeline.Select(json => BsonDocument.Parse(json)));
        }

        [Fact]
        public void ToLower_StartsWith_impossible_match_should_result_in_expected_filter()
        {
            var subject = CreateSubject();

            var queryable = subject.Where(p => p.Name.ToLower().StartsWith("joHN"));
            var pipeline = Translate(queryable);

            var expectedPipeline = new[]
            {
                "{ $match : { _id : { $type : -1 } } }"
            };
            pipeline.Should().Equal(expectedPipeline.Select(json => BsonDocument.Parse(json)));
        }

        private IQueryable<Person> CreateSubject()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<Person>("test");
            return collection.AsQueryable();
        }

        private BsonDocument[] Translate<T>(IQueryable<T> queryable)
        {
            var queryProvider = (MongoQueryProvider<Person>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<Person, IGrouping<int, Person>>(queryProvider, queryable.Expression, translationOptions: null);
            return executableQuery.Pipeline.AstPipeline.Render().AsBsonArray.Cast<BsonDocument>().ToArray();
        }

        public class Person
        {
            public int Id { get; set; }
            public String Name { get; set; }
        }
    }
}
