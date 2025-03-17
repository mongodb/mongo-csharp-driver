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
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5403Tests : LinqIntegrationTest<CSharp5403Tests.ClassFixture>
    {
        public CSharp5403Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Project_Id_should_work()
        {
            var collection = Fixture.Collection;

            var find = collection
                .Find(Builders<GridFSFileInfo>.Filter.Where(x => x.Id == ObjectId.Parse("111111111111111111111111")))
                .Project(x => x.Id);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ _id : { $oid : '111111111111111111111111' } }");

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var result = find.Single();
            result.Should().Be(ObjectId.Parse("111111111111111111111111"));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void Project_IdAsBsonValue_should_work()
        {
            var collection = Fixture.Collection;

            var find = collection
                .Find(Builders<GridFSFileInfo>.Filter.Where(x => x.IdAsBsonValue == new BsonObjectId(ObjectId.Parse("111111111111111111111111"))))
                .Project(x => x.IdAsBsonValue);

            var filter = TranslateFindFilter(collection, find);
            filter.Should().Be("{ _id : { $oid : '111111111111111111111111' } }");

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _id : 1 }");

            var result = find.Single();
            result.Should().Be(ObjectId.Parse("111111111111111111111111"));
        }
#pragma warning restore CS0618 // Type or member is obsolete

        public sealed class ClassFixture : MongoCollectionFixture<GridFSFileInfo, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
            [
                BsonDocument.Parse("{ _id : { $oid : '111111111111111111111111' }, filename : 'One' }"),
                BsonDocument.Parse("{ _id : { $oid : '222222222222222222222222' }, filename : 'Two' }")
            ];
        }
    }
}
