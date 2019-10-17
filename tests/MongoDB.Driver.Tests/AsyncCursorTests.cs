/* Copyright 2018-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Common;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AsyncCursorTests
    {
        //public methods
        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_not_throw_exception_after_double_close([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.KillCursorsCommand);

            string testCollectionName = "test";
            string testDatabaseName = "test";
            var client = CreateClient();
            DropCollection(client, testDatabaseName, testCollectionName);
            var collection = client.GetDatabase(testDatabaseName).GetCollection<BsonDocument>(testCollectionName);
            collection.InsertOne(new BsonDocument("key", "value1"));
            collection.InsertOne(new BsonDocument("key", "value2"));

            var cursor = collection.Find(FilterDefinition<BsonDocument>.Empty, new FindOptions { BatchSize = 1 }).ToCursor().As<AsyncCursor<BsonDocument>>();
            if (async)
            {
                cursor.CloseAsync().Wait();
                cursor.CloseAsync().Wait();
            }
            else
            {
                cursor.Close();
                cursor.Close();
            }
        }

        [SkippableFact]
        public void KillCursor_should_actually_work()
        {
            RequireServer.Check().Supports(Feature.KillCursorsCommand);
            var eventCapturer = new EventCapturer().Capture<CommandSucceededEvent>(x => x.CommandName.Equals("killCursors"));
            using (var client = DriverTestConfiguration.CreateDisposableClient(eventCapturer))
            {
                IAsyncCursor<BsonDocument> cursor;
                var database = client.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>(GetType().Name);
                var documents = new List<BsonDocument>();
                for (int i = 0; i < 1000; i++)
                {
                    documents.Add(new BsonDocument("x", i));
                }

                collection.InsertMany(documents);
                cursor = collection.FindSync("{}");
                cursor.MoveNext();

                var cursorId = ((AsyncCursor<BsonDocument>)cursor)._cursorId();
                cursorId.Should().NotBe(0);
                cursor.Dispose();

                var desiredResult = BsonDocument.Parse($"{{ \"cursorsKilled\" : [{cursorId}], \"cursorsNotFound\" : [], " +
                    $"\"cursorsAlive\" : [], \"cursorsUnknown\" : [], \"ok\" : 1.0 }}");
                var result = ((CommandSucceededEvent)eventCapturer.Events[0]).Reply;
                result.IsSameOrEqualTo(desiredResult);
            }
        }

        [SkippableFact]
        public void Tailable_cursor_should_be_able_to_be_cancelled_from_a_different_thread_with_expected_result()
        {
            RequireServer.Check().Supports(Feature.TailableCursor);

            string testCollectionName = "test";
            string testDatabaseName = "test";

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(testDatabaseName);
            var collection = database.GetCollection<BsonDocument>(testCollectionName);

            DropCollection(client, testDatabaseName, testCollectionName);
            var createCollectionOptions = new CreateCollectionOptions()
            {
                Capped = true,
                MaxSize = 1000
            };
            database.CreateCollection(testCollectionName, createCollectionOptions);
            collection.InsertOne(new BsonDocument()); // tailable cursors don't work on an empty collection

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var findOptions = new FindOptions<BsonDocument>()
                {
                    BatchSize = 1,
                    CursorType = CursorType.TailableAwait
                };
                var cursor = collection.FindSync(FilterDefinition<BsonDocument>.Empty, findOptions);
                var enumerator = cursor.ToEnumerable(cancellationTokenSource.Token).GetEnumerator();

                var semaphore = new SemaphoreSlim(0);
                var thread = new Thread(() =>
                {
                    semaphore.Wait();
                    cancellationTokenSource.Cancel();
                });
                thread.Start();

                var exception = Record.Exception((Action)(() =>
                {
                    while (true)
                    {
                        _ = enumerator.MoveNext();
                        semaphore.Release(1);
                    }
                }));

                exception.Should().BeAssignableTo<OperationCanceledException>();
            }
        }

        //private methods
        private IMongoClient CreateClient()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            return new MongoClient(connectionString);
        }

        private void DropCollection(IMongoClient client, string databaseName, string collectionName)
        {
            client.GetDatabase(databaseName).DropCollection(collectionName);
        }
    }

    public static class AsyncCursorReflector
    {
        public static long _cursorId(this AsyncCursor<BsonDocument> obj) =>
            (long)Reflector.GetFieldValue(obj, nameof(_cursorId));
    }
}
