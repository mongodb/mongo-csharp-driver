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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4821Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Where_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Status == Status.Open);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { Status : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void Select_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.ToConversionOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => new { Result = (int)x.Version });

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { Result : '$Version', _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { Result : { $toInt : '$Version' }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Select(x => x.Result).Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_followed_by_Select_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.ToConversionOperators);
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Where(x => x.Status == Status.Open)
                .Select(x => new { Result = (int)x.Version });

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(
                    stages,
                    "{ $match : { Status : 1 } }",
                    "{ $project : { Result : '$Version', _id : 0 } }");
            }
            else
            {
                AssertStages(
                    stages,
                    "{ $match : { Status : 1 } }",
                    "{ $project : { Result : { $toInt : '$Version' }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Select(x => x.Result).Should().Equal(1);
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, Status = Status.Open, Version = 1L },
                new C { Id = 2, Status = Status.Closed, Version = 2L });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public Status Status { get; set; }
            public long Version { get; set; }
        }

#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
        public enum Status { Closed, Open };
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
    }
}
