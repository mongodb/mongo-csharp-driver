/* Copyright 2010-2015 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoClientTests
    {
        [Test]
        public void UsesSameClusterForIdenticalSettings()
        {
            var client1 = new MongoClient("mongodb://localhost");
            var cluster1 = client1.Cluster;

            var client2 = new MongoClient("mongodb://localhost");
            var cluster2 = client2.Cluster;

            Assert.AreSame(cluster1, cluster2);
        }

        [Test]
        public void UsesSameClusterWhenReadPreferenceTagsAreTheSame()
        {
            var client1 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
            var cluster1 = client1.Cluster;

            var client2 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
            var cluster2 = client2.Cluster;

            Assert.AreSame(cluster1, cluster2);
        }

        [Test]
        public void DropDatabase_should_invoke_the_correct_operation(
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var client = new MongoClient(operationExecutor);

            if (async)
            {
                client.DropDatabaseAsync("awesome").GetAwaiter().GetResult();
            }
            else
            {
                client.DropDatabase("awesome");
            }

            var call = operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropDatabaseOperation>();
            ((DropDatabaseOperation)call.Operation).DatabaseNamespace.Should().Be(new DatabaseNamespace("awesome"));
        }

        [Test]
        public void ListDatabases_should_invoke_the_correct_operation(
            [Values(false, true)] bool async)
        {
            var operationExecutor = new MockOperationExecutor();
            var client = new MongoClient(operationExecutor);

            if (async)
            {
                client.ListDatabasesAsync().GetAwaiter().GetResult();
            }
            else
            {
                client.ListDatabases();
            }

            var call = operationExecutor.GetReadCall<IAsyncCursor<BsonDocument>>();

            call.Operation.Should().BeOfType<ListDatabasesOperation>();
        }
    }
}
