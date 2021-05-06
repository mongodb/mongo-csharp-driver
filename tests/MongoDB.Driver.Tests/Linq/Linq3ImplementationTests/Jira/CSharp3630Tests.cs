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
using MongoDB.Driver.Linq.Linq3Implementation;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToExecutableQueryTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3630Tests
    {
        [Fact]
        public void GroupBy_with_key_selector_and_result_selector_with_first_should_translate_as_expected()
        {
            var subject = CreateSubject();

            var queryable = subject.GroupBy(c => c.A, (k, g) => new { Result = g.Select(x => x).First() });
            var pipeline = Translate(queryable);

            // note: the expected pipeline will be different once the AstPipelineOptimizer is implemented
            var expectedPipeline = new[]
            {
                "{ $group : { _id : '$A', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { Result : { $arrayElemAt : [{ $map : { input : '$_elements', as : 'x', in : '$$x' } }, 0] }, _id : 0 } }"
            };
            pipeline.Should().Equal(expectedPipeline.Select(json => BsonDocument.Parse(json)));
        }

        [Fact]
        public void GroupBy_followed_by_select_with_first_should_translate_as_expected()
        {
            var subject = CreateSubject();

            var queryable = subject.GroupBy(c => c.A).Select(g => new { Result = g.Select(x => x).First() });
            var pipeline = Translate(queryable);

            // note: the expected pipeline will be different once the AstPipelineOptimizer is implemented
            var expectedPipeline = new[]
            {
                "{ $group : { _id : '$A', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { Result : { $arrayElemAt : [{ $map : { input : '$_elements', as : 'x', in : '$$x' } }, 0] }, _id : 0 } }"
            };
            pipeline.Should().Equal(expectedPipeline.Select(json => BsonDocument.Parse(json)));
        }

        [Fact]
        public void GroupBy_with_key_selector_and_result_selector_with_last_should_translate_as_expected()
        {
            var subject = CreateSubject();

            var queryable = subject.GroupBy(c => c.A, (k, g) => new { Result = g.Select(x => x).Last() });
            var pipeline = Translate(queryable);

            // note: the expected pipeline will be different once the AstPipelineOptimizer is implemented
            var expectedPipeline = new[]
            {
                "{ $group : { _id : '$A', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { Result : { $arrayElemAt : [{ $map : { input : '$_elements', as : 'x', in : '$$x' } }, -1] }, _id : 0 } }"
            };
            pipeline.Should().Equal(expectedPipeline.Select(json => BsonDocument.Parse(json)));
        }

        [Fact]
        public void GroupBy_followed_by_select_with_last_should_translate_as_expected()
        {
            var subject = CreateSubject();

            var queryable = subject.GroupBy(c => c.A).Select(g => new { Result = g.Select(x => x).Last() });
            var pipeline = Translate(queryable);

            // note: the expected pipeline will be different once the AstPipelineOptimizer is implemented
            var expectedPipeline = new[]
            {
                "{ $group : { _id : '$A', _elements : { $push : '$$ROOT' } } }",
                "{ $project : { Result : { $arrayElemAt : [{ $map : { input : '$_elements', as : 'x', in : '$$x' } }, -1] }, _id : 0 } }"
            };
            pipeline.Should().Equal(expectedPipeline.Select(json => BsonDocument.Parse(json)));
        }

        private IQueryable<C> CreateSubject()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<C>("test");
            return collection.AsQueryable3();
        }

        private BsonDocument[] Translate<T>(IQueryable<T> queryable)
        {
            var queryProvider = (MongoQueryProvider<C>)queryable.Provider;
            var executableQuery = ExpressionToExecutableQueryTranslator.Translate<C, IGrouping<int, C>>(queryProvider, queryable.Expression);
            return executableQuery.Pipeline.Render().AsBsonArray.Cast<BsonDocument>().ToArray();
        }

        public class C
        {
            public int Id { get; set; }
            public int A { get; set; }
        }
    }
}
