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

using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.transactions
{
    [Trait("Category", "Integration")]
    public class TransactionsProseTests : LoggableTestClass
    {
        private const string CollectionName = "txn-test-col";
        private const string DatabaseName = "txn-test";

        public TransactionsProseTests(ITestOutputHelper output) : base(output)
        {
        }

        // https://github.com/mongodb/specifications/blob/fc7996db26d0ea92091a5034c6acb287ef7282fe/source/transactions/tests/README.md#10-write-concern-not-inherited-from-collection-object-inside-transaction
        [Theory]
        [ParameterAttributeData]
        public async Task Ensure_write_concern_is_not_inherited_from_collection_object_inside_transaction([Values(false, true)] bool async)
        {
            RequireServer.Check().ClusterTypes(ClusterType.LoadBalanced, ClusterType.ReplicaSet, ClusterType.Sharded);

            using var client = DriverTestConfiguration.CreateMongoClient();
            var database = client.GetDatabase(DatabaseName).WithWriteConcern(WriteConcern.WMajority);
            database.DropCollection(CollectionName);

            var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>(CollectionName)
                .WithWriteConcern(WriteConcern.Unacknowledged);

            using (var session = client.StartSession())
            {
                session.StartTransaction();

                if (async)
                {
                    await collection.InsertOneAsync(new BsonDocument("n", 1));
                    await session.CommitTransactionAsync();
                }
                else
                {
                    collection.InsertOne(new BsonDocument("n", 1));
                    session.CommitTransaction();
                }
            }

            collection.Find(new BsonDocument("n", 1)).First().Should().NotBeNull().And.Subject["n"].AsInt32.Should().Be(1);
        }
    }
}