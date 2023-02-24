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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4538Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Project_with_nullable_value_should_work()
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = CreateCollection();

            var find = collection
                .Find(_ => true)
                .Project(x => x.D);

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ _v : '$D', _id : 0  }");

            var results = find.ToList();
            results.Should().Equal(1.0, null, null);
        }

        [Fact]
        public void Project_with_anonymous_class_with_nullable_value_should_work()
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = CreateCollection();

            var find = collection
                .Find(_ => true)
                .Project(x => new { V = x.D });

            var projection = TranslateFindProjection(collection, find);
            projection.Should().Be("{ V : '$D', _id : 0  }");

            var results = find.ToList();
            results.Select(x => x.V).Should().Equal(1.0, null, null);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            var bsonDocumentCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            CreateCollection(
                bsonDocumentCollection,
                new BsonDocument { { "_id", 1 }, { "D", 1.0 } },
                new BsonDocument { { "_id", 2 }, { "D", BsonNull.Value } },
                new BsonDocument { { "_id", 3 } });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public double? D { get; set; }
        }
    }
}
