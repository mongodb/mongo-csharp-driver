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

using System;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.transactions
{
    [Trait("Category", "Integration")]
    public class TransactionsProseTests
    {
        private string _collectionName = "txn-test-col";
        private string _databaseName = "txn-test";

        // https://github.com/mongodb/specifications/blob/fc7996db26d0ea92091a5034c6acb287ef7282fe/source/transactions/tests/README.md#10-write-concern-not-inherited-from-collection-object-inside-transaction
        [Theory]
        [ParameterAttributeData]
        public async void Ensure_write_concern_is_not_inherited_from_collection_object_inside_transaction([Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.LoadBalanced, ClusterType.ReplicaSet, ClusterType.Sharded);

            using var client = DriverTestConfiguration.CreateMongoClient();
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);
            database.DropCollection(_collectionName);

            var collection = client.GetDatabase(_databaseName).GetCollection<BsonDocument>(_collectionName)
                .WithWriteConcern(WriteConcern.Unacknowledged);

            Exception exception;
            using (var session = client.StartSession())
            {
                session.StartTransaction();

                if (async)
                {
                    exception = await Record.ExceptionAsync( async () =>
                    {
                        await collection.InsertOneAsync(new BsonDocument("n", 1));
                        await session.CommitTransactionAsync();
                    });
                }
                else
                {
                    exception = Record.Exception(() =>
                    {
                        collection.InsertOne(new BsonDocument("n", 1));
                        session.CommitTransaction();
                    });
                }
            }

            exception.Should().BeNull();
            collection.Find(new BsonDocument("n", 1)).First().Should().NotBeNull().And.Subject["n"].AsInt32.Should().Be(1);
        }
    }
}