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

using System;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Tests;
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
            var enumerator = inventory.Watch().ToEnumerable().GetEnumerator();
            enumerator.MoveNext();
            var next = enumerator.Current;
            enumerator.Dispose();
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
            var enumerator = inventory.Watch(options).ToEnumerable().GetEnumerator();
            enumerator.MoveNext();
            var next = enumerator.Current;
            enumerator.Dispose();
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

            ChangeStreamDocument<BsonDocument> lastChangeStreamDocument;
            {
                new Thread(() =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    inventory.InsertMany(documents);
                })
                .Start();

                var enumerator = inventory.Watch().ToEnumerable().GetEnumerator();
                enumerator.MoveNext();
                lastChangeStreamDocument = enumerator.Current;
            }

            {
                // Start Changestream Example 3
                var resumeToken = lastChangeStreamDocument.ResumeToken;
                var options = new ChangeStreamOptions { ResumeAfter = resumeToken };
                var enumerator = inventory.Watch(options).ToEnumerable().GetEnumerator();
                enumerator.MoveNext();
                var next = enumerator.Current;
                enumerator.Dispose();
                // End Changestream Example 3

                next.FullDocument.Should().Be(documents[1]);
            }
        }
    }
}
