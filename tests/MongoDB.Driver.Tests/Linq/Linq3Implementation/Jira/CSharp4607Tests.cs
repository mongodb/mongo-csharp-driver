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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4607Tests : LinqIntegrationTest<CSharp4607Tests.ClassFixture>
    {
        public CSharp4607Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Constant_All_Enumerable_Contains_should_work()
        {
            var collection = Fixture.Collection;
            IList<int> values = new int[] { 1, 2, 3 };

            var filter = Builders<C>.Filter.Where(c => values.All(e => c.A.Contains(e)));

            var translatedFilter = TranslateFilter(collection, filter);
            translatedFilter.Should().Be("{ A : { $all : [1, 2, 3] } }");
        }

        [Fact]
        public void Constant_All_IList_Contains_should_work()
        {
            var collection = Fixture.Collection;
            IList<int> values = new int[] { 1, 2, 3 };

            var filter = Builders<C>.Filter.Where(c => values.All(e => c.L.Contains(e)));

            var translatedFilter = TranslateFilter(collection, filter);
            translatedFilter.Should().Be("{ L : { $all : [1, 2, 3] } }");
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public IList<int> L { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData => null;
        }
    }
}
