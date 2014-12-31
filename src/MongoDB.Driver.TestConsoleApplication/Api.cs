﻿using System;
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
    public class Api
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void Run(int numConcurrentWorkers, Action<ClusterBuilder> configurator)
        {
            try
            {
                RunAsync(numConcurrentWorkers, configurator).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        public async Task RunAsync(int numConcurrentWorkers, Action<ClusterBuilder> configurator)
        {
            var settings = new MongoClientSettings();
            settings.ClusterConfigurator = configurator;

            var client = new MongoClient(settings);

            var db = client.GetDatabase("foo");
            var collection = db.GetCollection<BsonDocument>("bar");

            Console.WriteLine("Press Enter to begin");
            Console.ReadLine();

            Console.WriteLine("Clearing Data");
            await ClearData(collection);
            Console.WriteLine("Inserting Seed Data");
            await InsertData(collection);

            Console.WriteLine("Running CRUD (errors will show up as + (query error) or * (insert/update error))");
            var tasks = new List<Task>();
            for (int i = 0; i < numConcurrentWorkers; i++)
            {
                tasks.Add(DoWork(collection));
            }

            Console.WriteLine("Press Enter to shutdown");
            Console.ReadLine();

            _cancellationTokenSource.Cancel();
            Task.WaitAll(tasks.ToArray());
        }

        private async Task DoWork(IMongoCollection<BsonDocument> collection)
        {
            var rand = new Random();
            while(!_cancellationTokenSource.IsCancellationRequested)
            {
                var i = rand.Next(0, 10000);
                List<BsonDocument> docs;
                try
                {
                    docs = await collection.Find(Query.EQ("i", i))
                        .ToListAsync(_cancellationTokenSource.Token);
                }
                catch
                {
                    Console.Write("+");
                    continue;
                }

                if(docs.Count == 0)
                {
                    try
                    {
                        await collection.InsertOneAsync(new BsonDocument("i", i), _cancellationTokenSource.Token);
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
                        await collection.UpdateOneAsync(filter, update, cancellationToken: _cancellationTokenSource.Token);
                        //Console.Write(".");
                    }
                    catch (Exception)
                    {
                        Console.Write("*");
                    }
                }
            }
        }

        private Task ClearData(IMongoCollection<BsonDocument> collection)
        {
            return collection.DeleteManyAsync("{}", _cancellationTokenSource.Token);
        }

        private async Task InsertData(IMongoCollection<BsonDocument> collection)
        {
            for (int i = 0; i < 100; i++)
            {
                await collection.InsertOneAsync(new BsonDocument("i", i), _cancellationTokenSource.Token);
            }
        }


    }
}
