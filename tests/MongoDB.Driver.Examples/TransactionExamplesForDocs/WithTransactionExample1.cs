/* Copyright 2019-present MongoDB Inc.
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

using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Examples.TransactionExamplesForDocs
{
    public class WithTransactionExample1
    {
        [SkippableFact]
        public void Example1()
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded).Supports(Feature.Transactions);

            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            DropCollections(
                connectionString, 
                CollectionNamespace.FromFullName("mydb1.foo"), 
                CollectionNamespace.FromFullName("mydb2.bar"));
            string result = null;

            // Start Transactions withTxn API Example 1
            // For a replica set, include the replica set name and a seedlist of the members in the URI string; e.g.
            // string uri = "mongodb://mongodb0.example.com:27017,mongodb1.example.com:27017/?replicaSet=myRepl";
            // For a sharded cluster, connect to the mongos instances; e.g.
            // string uri = "mongodb://mongos0.example.com:27017,mongos1.example.com:27017:27017/";
            var client = new MongoClient(connectionString);

            // Prereq: Create collections. CRUD operations in transactions must be on existing collections.
            var database1 = client.GetDatabase("mydb1");
            var collection1 = database1.GetCollection<BsonDocument>("foo").WithWriteConcern(WriteConcern.WMajority);
            collection1.InsertOne(new BsonDocument("abc", 0));

            var database2 = client.GetDatabase("mydb2");
            var collection2 = database2.GetCollection<BsonDocument>("bar").WithWriteConcern(WriteConcern.WMajority);
            collection2.InsertOne(new BsonDocument("xyz", 0));

            // Step 1: Start a client session.
            using (var session = client.StartSession())
            {
                // Step 2: Optional. Define options to use for the transaction.
                var transactionOptions = new TransactionOptions(
                    readPreference: ReadPreference.Primary,
                    readConcern: ReadConcern.Local,
                    writeConcern: WriteConcern.WMajority);

                // Step 3: Define the sequence of operations to perform inside the transactions
                var cancellationToken = CancellationToken.None; // normally a real token would be used
                result = session.WithTransaction(
                    (s, ct) =>
                    {
                        collection1.InsertOne(s, new BsonDocument("abc", 1), cancellationToken: ct);
                        collection2.InsertOne(s, new BsonDocument("xyz", 999), cancellationToken: ct);
                        return "Inserted into collections in different databases";
                    },
                    transactionOptions,
                    cancellationToken);
            }
            //End Transactions withTxn API Example 1

            result.Should().Be("Inserted into collections in different databases");

            var collection1Documents = collection1.Find(FilterDefinition<BsonDocument>.Empty).ToList();
            collection1Documents.Count.Should().Be(2);
            collection1Documents[0]["abc"].Should().Be(0);
            collection1Documents[1]["abc"].Should().Be(1);

            var collection2Documents = collection2.Find(FilterDefinition<BsonDocument>.Empty).ToList();
            collection2Documents.Count.Should().Be(2);
            collection2Documents[0]["xyz"].Should().Be(0);
            collection2Documents[1]["xyz"].Should().Be(999);
        }

        // private methods
        private void DropCollections(string connectionString, params CollectionNamespace[] collectionNamespaces)
        {
            var client = new MongoClient(connectionString);
            foreach (var collectionNamespace in collectionNamespaces)
            {
                var database = client.GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName);
                database.DropCollection(collectionNamespace.CollectionName);
            }
        }
    }
}
