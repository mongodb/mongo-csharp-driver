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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2134Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void All_with_predicate_should_work(bool includeNullId, bool expectedResult)
        {
            var collection = CreateCollection(includeNullId);
            var queryable = collection.AsQueryable();

            var result = queryable.All(x => x.Id != null); // AllAsync is not implemented

            AssertStages(
                queryable.GetLoggedStages(),
                "{ $match : { _id : null } }",
                "{ $limit : 1 }",
                "{ $project : { _id : 0, _v : null } }");
            result.Should().Be(expectedResult);
        }

        private IMongoCollection<C> CreateCollection(bool includeNullId)
        {
            var collection = GetCollection<C>("C");

            var documents = new List<C>
            {
                new C { Id = 1 },
                new C { Id = 2 },
                new C { Id = 3 }
            };
            if (includeNullId)
            {
                documents.Add(new C { Id = null });
            }

            CreateCollection(collection, documents);

            return collection;
        }

        private class C
        {
            public int? Id { get; set; }
        }
    }
}
