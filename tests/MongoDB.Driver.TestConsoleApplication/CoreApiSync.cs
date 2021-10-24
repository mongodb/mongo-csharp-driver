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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Operations.ElementNameValidators;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.TestConsoleApplication
{
    public class CoreApiSync
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CollectionNamespace _collection = new CollectionNamespace("foo", "bar");
        private MessageEncoderSettings _messageEncoderSettings = new MessageEncoderSettings();

        public void Run(int numConcurrentWorkers, Action<ClusterBuilder> configurator)
        {
            try
            {
                var clusterBuilder = new ClusterBuilder();
                configurator(clusterBuilder);

                using (var cluster = clusterBuilder.BuildCluster())
                {
                    Run(numConcurrentWorkers, cluster);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private void Run(int numConcurrentWorkers, ICluster cluster)
        {
            Console.WriteLine("Press Enter to begin");
            Console.ReadLine();

            Console.WriteLine("Clearing Data");
            ClearData(cluster);
            Console.WriteLine("Inserting Seed Data");
            InsertData(cluster);

            Console.WriteLine("Running CRUD (errors will show up as + (query error) or * (insert/update error))");
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < numConcurrentWorkers; i++)
            {
                tasks.Add(Task.Run(() => DoWork(cluster)));
            }

            Console.WriteLine("Press Enter to shutdown");
            Console.ReadLine();

            _cancellationTokenSource.Cancel();
            Task.WaitAll(tasks.ToArray());
        }

        private void ClearData(ICluster cluster)
        {
            using (var binding = new WritableServerBinding(cluster, NoCoreSession.NewHandle()))
            {
                var commandOp = new DropDatabaseOperation(_collection.DatabaseNamespace, _messageEncoderSettings);
                commandOp.Execute(binding, CancellationToken.None);
            }
        }

        private void InsertData(ICluster cluster)
        {
            using (var binding = new WritableServerBinding(cluster, NoCoreSession.NewHandle()))
            {
                for (int i = 0; i < 100; i++)
                {
                    Insert(binding, new BsonDocument("i", i));
                }
            }
        }

        private void DoWork(ICluster cluster)
        {
            var rand = new Random();
            using (var binding = new WritableServerBinding(cluster, NoCoreSession.NewHandle()))
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var i = rand.Next(0, 10000);
                    IReadOnlyList<BsonDocument> docs;
                    using (var cursor = Query(binding, new BsonDocument("i", i)))
                    {
                        try
                        {
                            if (cursor.MoveNext(_cancellationTokenSource.Token))
                            {
                                docs = cursor.Current.ToList();
                            }
                            else
                            {
                                docs = null;
                            }
                            //Console.Write(".");
                        }
                        catch
                        {
                            Console.Write("+");
                            continue;
                        }
                    }


                    if (docs == null || docs.Count == 0)
                    {
                        try
                        {
                            Insert(binding, new BsonDocument().Add("i", i));
                            //Console.Write(".");
                        }
                        catch (Exception)
                        {
                            Console.Write("*");
                        }
                    }
                    else
                    {
                        try
                        {
                            var filter = new BsonDocument("_id", docs[0]["_id"]);
                            var update = new BsonDocument("$set", new BsonDocument("i", i + 1));
                            Update(binding, filter, update);
                            //Console.Write(".");
                        }
                        catch (Exception)
                        {
                            Console.Write("*");
                        }
                    }
                }
            }
        }

        private void Insert(IWriteBinding binding, BsonDocument document)
        {
            var insertOp = new InsertOpcodeOperation<BsonDocument>(_collection, new[] { document }, BsonDocumentSerializer.Instance, _messageEncoderSettings);

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _cancellationTokenSource.Token))
            {
                insertOp.Execute(binding, linked.Token);
            }
        }

        private IAsyncCursor<BsonDocument> Query(IReadBinding binding, BsonDocument filter)
        {
            var findOp = new FindOperation<BsonDocument>(_collection, BsonDocumentSerializer.Instance, _messageEncoderSettings)
            {
                Filter = filter,
                Limit = -1
            };

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _cancellationTokenSource.Token))
            {
                return findOp.Execute(binding, linked.Token);
            }
        }

        private void Update(IWriteBinding binding, BsonDocument filter, BsonDocument update)
        {
            var updateOp = new UpdateOpcodeOperation(
                _collection,
                new UpdateRequest(UpdateType.Update, filter, update),
                _messageEncoderSettings);

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, _cancellationTokenSource.Token))
            {
                updateOp.Execute(binding, linked.Token);
            }
        }
    }
}
