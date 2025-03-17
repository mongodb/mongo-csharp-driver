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
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4622Tests : LinqIntegrationTest<CSharp4622Tests.ClassFixture>
    {
        public CSharp4622Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void ReplaceOne()
        {
            var collection = Fixture.Collection;
            var options = new ReplaceOptions { IsUpsert = true };
            var data = new Data { Id = 8, Text = "updated" };

            var result = collection.ReplaceOne(d => true, data, options);

            result.UpsertedId.Should().Be(8);
        }

        public class Data
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Data>
        {
            protected override IEnumerable<Data> InitialData => null;
        }
    }
}
