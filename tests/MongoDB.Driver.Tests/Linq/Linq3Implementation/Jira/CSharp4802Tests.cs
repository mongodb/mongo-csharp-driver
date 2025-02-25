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
    public class CSharp4802Tests : LinqIntegrationTest<CSharp4802Tests.ClassFixture>
    {
        public CSharp4802Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Find_with_projection_of_subfield_should_work()
        {
            var collection = Fixture.Collection;

            var find = collection.Find(d => d.Status == "a").Project(d => d.SubDocument.Id);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ 'SubDocument._id' : 1, _id : 0 }");

            var result = find.Single();
            result.Should().Be(11);
        }

        public class Document
        {
            public int Id { get; set; }
            public string Status { get; set; }
            public SubDocument SubDocument { get; set; }
        }

        public class SubDocument
        {
            public int Id { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Document>
        {
            protected override IEnumerable<Document> InitialData =>
            [
                new Document { Id = 1, Status = "a", SubDocument = new SubDocument { Id = 11 } },
                new Document { Id = 2, Status = "b", SubDocument = new SubDocument { Id = 22 } }
            ];
        }
    }
}
