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
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4126Tests : LinqIntegrationTest<CSharp4126Tests.ClassFixture>
    {
        public CSharp4126Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Test()
        {
            var collection = Fixture.Collection;
            var queryable = collection.AsQueryable()
                .Select(_p0 => new { Id = _p0.Id, A = _p0.A.Select(_p1 => Math.Abs(_p1)) });

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _id : '$_id', A : { $map : { input : '$A', as : 'v__0', in : { $abs : '$$v__0' } } } } }");

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

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, A = new[] { 1, -2, -3 } }
            ];
        }
    }
}
