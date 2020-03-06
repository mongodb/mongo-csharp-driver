/* Copyright 2020-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class MongoIndexManagerTests
    {
        [Theory]
        [ParameterAttributeData]
        public void List_should_return_expected_result(
            [Values("{ singleIndex : 1 }", "{ compoundIndex1 : 1, compoundIndex2 : 1 }")] string key,
            [Values(false, true)] bool unique,
            [Values(false, true)] bool async)
        {
            var indexKeyDocument = BsonDocument.Parse(key);
            var collectionName = DriverTestConfiguration.CollectionNamespace.CollectionName;
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            database.DropCollection(collectionName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            var subject = collection.Indexes;

            try
            {
                subject.CreateOne(new CreateIndexModel<BsonDocument>(indexKeyDocument, new CreateIndexOptions() { Unique = unique }));

                var indexesCursor =
                    async
                        ? subject.ListAsync().GetAwaiter().GetResult()
                        : subject.List();
                var indexes = indexesCursor.ToList();

                indexes.Count.Should().Be(2);
                AssertIndex(collection.CollectionNamespace, indexes[0], "_id_");
                var indexName = IndexNameHelper.GetIndexName(indexKeyDocument);
                AssertIndex(collection.CollectionNamespace, indexes[1], indexName, expectedUnique: unique);

                void AssertIndex(CollectionNamespace collectionNamespace, BsonDocument index, string expectedName, bool expectedUnique = false)
                {
                    index["name"].AsString.Should().Be(expectedName);

                    if (expectedUnique)
                    {
                        index["unique"].AsBoolean.Should().BeTrue();
                    }
                    else
                    {
                        index.Contains("unique").Should().BeFalse();
                    }

                    if (CoreTestConfiguration.ServerVersion < new SemanticVersion(4, 3, 0))
                    {
                        index["ns"].AsString.Should().Be(collectionNamespace.ToString());
                    }
                    else
                    {
                        // the server doesn't return ns anymore
                        index.Contains("ns").Should().BeFalse();
                    }
                }
            }
            finally
            {
                // make sure that index has been removed
                database.DropCollection(collectionName);
            }
        }
    }
}
