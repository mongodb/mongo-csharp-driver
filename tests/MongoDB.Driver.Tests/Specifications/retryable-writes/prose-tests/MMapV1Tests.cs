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

using System;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes.prose_tests
{
    public class MMapV1Tests
    {
        [SkippableTheory]
        [ParameterAttributeData]
        public void Write_operation_should_throw_when_retry_writes_is_true_and_storage_engine_is_MMMAPv1(
            [Values(false, true)] bool async)
        {
            RequireServer.Check()
                .VersionGreaterThanOrEqualTo("3.6.0")
                .ClusterType(ClusterType.ReplicaSet)
                .StorageEngine("mmapv1");

            using (var client = CreateDisposableMongoClient())
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);
                database.DropCollection(collection.CollectionNamespace.CollectionName);

                var document = new BsonDocument("_id", 1);
                Exception exception;
                if (async)
                {
                    exception = Record.Exception(() => collection.InsertOneAsync(document).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => collection.InsertOne(document));
                }

                var e = exception.Should().BeOfType<MongoCommandException>().Subject;
                e.Message.Should().Be("This MongoDB deployment does not support retryable writes. Please add retryWrites=false to your connection string.");
            }
        }

        // private methods
        private DisposableMongoClient CreateDisposableMongoClient()
        {
            return DriverTestConfiguration.CreateDisposableClient(s => s.RetryWrites = true);
        }
    }
}
