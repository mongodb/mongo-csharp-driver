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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4126Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Test()
        {
            var collection = GetCollection<C>();
            CreateCollection(
                collection,
                new C { Id = 1, A = new[] { 1, -2, -3 } });

            var queryable = collection.AsQueryable()
                .Select(_p0 => new { Id = _p0.Id, A = _p0.A.Select(_p1 => Math.Abs(_p1)) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { Id : '$_id', A : { $map : { input : '$A', as : 'v__0', in : { $abs : '$$v__0' } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(1);
            results[0].Id.Should().Be(1);
            results[0].A.Should().Equal(1, 2, 3);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
