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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3946Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_with_constant_limit_should_work()
        {
            RequireServer.Check().Supports(Feature.FilterLimit);

            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => x.IA.Where(i => i >= 2, 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$IA', as : 'i', cond : { $gte : ['$$i', 2] }, limit : 2 } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Should().Equal(2, 3);
        }

        [Fact]
        public void Where_with_limit_computed_server_side_should_work()
        {
            RequireServer.Check().Supports(Feature.FilterLimit);

            var collection = CreateCollection();
            var queryable = collection.AsQueryable()
                .Select(x => x.IA.Where(i => i >= 2, x.Id + 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$IA', as : 'i', cond : { $gte : ['$$i', 2] }, limit : { $add : ['$_id', 1] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Should().Equal(2, 3);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>();
            CreateCollection(
                collection,
                new C { Id = 1, IA = new[] { 1, 2, 3, 4 } });
            return collection;
        }

        public class C
        {
            public int Id { get; set; }
            public int[] IA { get; set; }
        }
    }
}
