/* Copyright 2017-present MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class ChangeStreamExamples
    {
        [Fact]
        public void ChangeStreamExample1()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("ChangeStreamExamples");
            database.DropCollection("inventory");
            var inventory = database.GetCollection<BsonDocument>("inventory");

            var document = new BsonDocument("x", 1);
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                inventory.InsertOne(document);
            })
            .Start();

            // Start Changestream Example 1
            var cursor = inventory.Watch();
            while (cursor.MoveNext() && cursor.Current.Count() == 0) { } // keep calling MoveNext until we've read the first batch
            var next = cursor.Current.First();
            cursor.Dispose();
            // End Changestream Example 1

            next.FullDocument.Should().Be(document);
        }

        [Fact]
        public void ChangeStreamExample2()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("ChangeStreamExamples");
            database.DropCollection("inventory");
            var inventory = database.GetCollection<BsonDocument>("inventory");

            var document = new BsonDocument("x", 1);
            inventory.InsertOne(document);
            new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                var filter = new BsonDocument("_id", document["_id"]);
                var update = "{ $set : { x : 2 } }";
                inventory.UpdateOne(filter, update);
            })
            .Start();

            // Start Changestream Example 2
            var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
            var cursor = inventory.Watch(options);
            while (cursor.MoveNext() && cursor.Current.Count() == 0) { } // keep calling MoveNext until we've read the first batch
            var next = cursor.Current.First();
            cursor.Dispose();
            // End Changestream Example 2

            var expectedFullDocument = document.Set("x", 2);
            next.FullDocument.Should().Be(expectedFullDocument);
        }

        [Fact]
        public void ChangeStreamExample3()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("ChangeStreamExamples");
            database.DropCollection("inventory");
            var inventory = database.GetCollection<BsonDocument>("inventory");

            var documents = new[]
            {
                new BsonDocument("x", 1),
                new BsonDocument("x", 2)
            };

            IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> previousCursor;
            {
                new Thread(() =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    inventory.InsertMany(documents);
                })
                .Start();

                previousCursor = inventory.Watch(new ChangeStreamOptions { BatchSize = 1 });
                while (previousCursor.MoveNext() && previousCursor.Current.Count() == 0) { } // keep calling MoveNext until we've read the first batch
            }

            {
                // Start Changestream Example 3
                var resumeToken = previousCursor.GetResumeToken();
                var options = new ChangeStreamOptions { ResumeAfter = resumeToken };
                var cursor = inventory.Watch(options);
                cursor.MoveNext();
                var next = cursor.Current.First();
                cursor.Dispose();
                // End Changestream Example 3

                next.FullDocument.Should().Be(documents[1]);
            }
        }

        [Fact]
        public void ChangestreamExample4()
        {
            RequireServer.Check();

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("ChangeStreamExamples");
            database.DropCollection("inventory");

            var cancelationTokenSource = new CancellationTokenSource();
            try
            {
                var document = new BsonDocument("username", "alice");

                Task.Run(() =>
                {
                    var inventoryCollection = database.GetCollection<BsonDocument>("inventory");

                    while (!cancelationTokenSource.IsCancellationRequested)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        document["_id"] = ObjectId.GenerateNewId();
                        inventoryCollection.InsertOne(document);
                    }
                }, cancelationTokenSource.Token);

                // Start Changestream Example 4
                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                    .Match(change =>
                        change.FullDocument["username"] == "alice" ||
                        change.OperationType == ChangeStreamOperationType.Delete)
                    .AppendStage<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>, BsonDocument>(
                        "{ $addFields : { newField : 'this is an added field!' } }");

                var collection = database.GetCollection<BsonDocument>("inventory");
                using (var cursor = collection.Watch(pipeline))
                {
                    while (cursor.MoveNext() && cursor.Current.Count() == 0) { } // keep calling MoveNext until we've read the first batch
                    var next = cursor.Current.First();
                }
                // End Changestream Example 4
            }
            finally
            {
                cancelationTokenSource.Cancel();
            }
        }
    }
}
