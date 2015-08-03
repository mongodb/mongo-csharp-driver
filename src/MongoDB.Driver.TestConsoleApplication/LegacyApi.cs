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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events.Diagnostics;

namespace MongoDB.Driver.TestConsoleApplication
{
    public class LegacyApi
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void Run(int numConcurrentWorkers, Action<ClusterBuilder> configurator)
        {
            var settings = new MongoClientSettings();
            settings.ClusterConfigurator = configurator;

            var client = new MongoClient(settings);
#pragma warning disable 618
            var server = client.GetServer();
#pragma warning restore 618

            var db = server.GetDatabase("foo");
            var collection = db.GetCollection<BsonDocument>("bar");

            Console.WriteLine("Press Enter to begin");
            Console.ReadLine();

            Console.WriteLine("Clearing Data");
            ClearData(collection);
            Console.WriteLine("Inserting Seed Data");
            InsertData(collection);

            Console.WriteLine("Running CRUD (errors will show up as + (query error) or * (insert/update error))");
            for (int i = 0; i < numConcurrentWorkers; i++)
            {
                ThreadPool.QueueUserWorkItem(_ => DoWork(collection));
            }

            Console.WriteLine("Press Enter to shutdown");
            Console.ReadLine();

            _cancellationTokenSource.Cancel();

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private void DoWork(MongoCollection<BsonDocument> collection)
        {
            var rand = new Random();
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var i = rand.Next(0, 10000);
                List<BsonDocument> docs;
                try
                {
                    docs = collection.Find(Query.EQ("i", i))
                        .ToList();
                }
                catch
                {
                    Console.Write("+");
                    continue;
                }

                if (docs.Count == 0)
                {
                    try
                    {
                        collection.Insert(new BsonDocument("i", i));
                    }
                    catch
                    {
                        Console.Write("*");
                    }
                }
                else
                {
                    try
                    {
                        var filter = new QueryDocument("_id", docs[0]["_id"]);
                        var update = new UpdateDocument("$set", new BsonDocument("i", i + 1));
                        collection.Update(filter, update);
                        //Console.Write(".");
                    }
                    catch (Exception)
                    {
                        Console.Write("*");
                    }
                }
            }
        }

        private void ClearData(MongoCollection<BsonDocument> collection)
        {
            collection.Drop();
        }

        private void InsertData(MongoCollection<BsonDocument> collection)
        {
            for (int i = 0; i < 100; i++)
            {
                collection.Insert(new BsonDocument("i", i));
            }
        }


    }
}
