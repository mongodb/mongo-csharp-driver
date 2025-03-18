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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5527Tests : LinqIntegrationTest<CSharp5527Tests.ClassFixture>
    {
        public CSharp5527Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Sigmoid_should_work()
        {
            RequireServer.Check().Supports(Feature.Sigmoid);
            
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => MongoDBMath.Sigmoid(x.X));
        
            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sigmoid : '$X' }, _id : 0 } }");

            var result = queryable.ToList();
            result.Should().BeEquivalentTo(new[] { 0.7310585786300049, 0.9933071490757153, 0.999997739675702, 0.9999999992417439});
        }

        public class C
        {
            public double X { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new() { X = 1.0 },
                new() { X = 5.0 },
                new() { X = 13.0 },
                new() { X = 21.0 },
            ];
        }
    }
}