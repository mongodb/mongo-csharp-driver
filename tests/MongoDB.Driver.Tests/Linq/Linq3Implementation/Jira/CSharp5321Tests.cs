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
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5321Tests : LinqIntegrationTest<CSharp5321Tests.ClassFixture>
    {
        public CSharp5321Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Client_side_projection_should_fetch_only_needed_fields()
        {
            var collection = Fixture.Collection;
            var translationOptions = GetTranslationOptions();

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => Add(x.X, 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _snippets : ['$X'], _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(2);
        }

        [Fact]
        public void Client_side_projection_should_compute_sum_server_side()
        {
            var collection = Fixture.Collection;
            var translationOptions = GetTranslationOptions();

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => Add(x.A.Sum(), 4));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _snippets : [{ $sum : '$A' }], _id : 0 } }");

            var result = queryable.Single();
            result.Should().Be(10);
        }

        private ExpressionTranslationOptions GetTranslationOptions()
        {
            var wireVersion = CoreTestConfiguration.MaxWireVersion;
            var compatibilityLevel = Feature.FindProjectionExpressions.IsSupported(wireVersion) ? (ServerVersion?)null : ServerVersion.Server42;
            return new ExpressionTranslationOptions
            {
                EnableClientSideProjections = true,
                CompatibilityLevel = compatibilityLevel,
            };
        }

        private static int Add(int x, int y) => x + y;

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int[] A { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, X = 1, A = [1, 2, 3] }
            ];
        }
    }
}
