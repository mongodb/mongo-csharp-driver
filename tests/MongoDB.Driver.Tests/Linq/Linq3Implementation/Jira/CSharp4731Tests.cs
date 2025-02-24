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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4731Tests : LinqIntegrationTest<CSharp4731Tests.ClassFixture>
    {
        public CSharp4731Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Select_setting_IList_from_List_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new P { IList = x.List })
                .Where(x => x.IList.Contains(E.A));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { IList : '$List', _id : 0 } }",
                "{ $match : { IList : 'A' } }");

            var result = queryable.Single();
            result.IList.Should().Equal(E.A, E.B);
        }

        [Fact]
        public void Select_setting_IReadOnlyList_from_List_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection
                .AsQueryable()
                .Select(x => new Q { IReadOnlyList = x.List })
                .Where(x => x.IReadOnlyList.Contains(E.A));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { IReadOnlyList : '$List', _id : 0 } }",
                "{ $match : { IReadOnlyList : 'A' } }");

            var result = queryable.Single();
            result.IReadOnlyList.Should().Equal(E.A, E.B);
        }

        public class Test
        {
            public int Id { get; set; }
            [BsonRepresentation(BsonType.String)]
            public List<E> List { get; set; }
        }

        public class P
        {
            public IList<E> IList { get; set; }
        }

        public class Q
        {
            public IReadOnlyList<E> IReadOnlyList { get; set; }
        }

        public enum E { A, B, C, D }

        public sealed class ClassFixture : MongoCollectionFixture<Test>
        {
            protected override IEnumerable<Test> InitialData =>
            [
                new Test { Id = 1, List = new List<E> { E.A, E.B } },
                new Test { Id = 2, List = new List<E> { E.C, E.D } }
            ];
        }
    }
}
