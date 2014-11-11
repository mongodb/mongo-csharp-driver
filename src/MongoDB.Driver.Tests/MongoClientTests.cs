/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Operations;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class MongoClientTests
    {
        [Test]
        public void UsesSameMongoServerForIdenticalSettings()
        {
            var client1 = new MongoClient("mongodb://localhost");
#pragma warning disable 618
            var server1 = client1.GetServer();
#pragma warning restore

            var client2 = new MongoClient("mongodb://localhost");
#pragma warning disable 618
            var server2 = client2.GetServer();
#pragma warning restore

            Assert.AreSame(server1, server2);
        }

        [Test]
        public void UsesSameMongoServerWhenReadPreferenceTagsAreTheSame()
        {
            var client1 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
#pragma warning disable 618
            var server1 = client1.GetServer();
#pragma warning restore

            var client2 = new MongoClient("mongodb://localhost/?readPreference=secondary;readPreferenceTags=dc:ny");
#pragma warning disable 618
            var server2 = client2.GetServer();
#pragma warning restore

            Assert.AreSame(server1, server2);
        }

        [Test]
        public async Task DropDatabaseAsync_should_invoke_the_correct_operation()
        {
            var operationExecutor = new MockOperationExecutor();
            var client = new MongoClient(operationExecutor);
            await client.DropDatabaseAsync("awesome");

            var call = operationExecutor.GetWriteCall<BsonDocument>();

            call.Operation.Should().BeOfType<DropDatabaseOperation>();
            ((DropDatabaseOperation)call.Operation).DatabaseNamespace.Should().Be(new DatabaseNamespace("awesome"));
        }

        [Test]
        public async Task ListDatabaseNamesAsync_should_invoke_the_correct_operation()
        {
            var operationExecutor = new MockOperationExecutor();
            var client = new MongoClient(operationExecutor);
            await client.GetDatabaseNamesAsync();

            var call = operationExecutor.GetReadCall<IReadOnlyList<string>>();

            call.Operation.Should().BeOfType<ListDatabaseNamesOperation>();
        }
    }
}
