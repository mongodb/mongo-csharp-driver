using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.TestConsoleApplication
{
    public static class Program
    {
        private static string _database = "foo";
        private static string _collection = "bar";
        private static int _numConcurrentWorkers = 100;

        public static void Main(string[] args)
        {
            try
            {
                using (var cluster = CreateCluster())
                {
                    MainAsync(cluster).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception:");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press Enter to exit");
                Console.ReadLine();
            }
        }

        private static ICluster CreateCluster()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var file = Path.Combine(desktop, "log.txt");
            var streamWriter = new StreamWriter(file);
            var writer = TextWriter.Synchronized(streamWriter);

            return new ClusterBuilder()
                .ConfigureWithConnectionString("mongodb://localhost:30000,localhost:30001")
                .ConfigureServer(s => s
                    .WithHeartbeatInterval(TimeSpan.FromMinutes(1))
                    .WithHeartbeatTimeout(TimeSpan.FromMinutes(1)))
                .ConfigureConnection(s => s
                    .WithMaxLifeTime(TimeSpan.FromSeconds(30)))
                .AddListener(new LogListener(writer, LogLevel.Debug))
                .UsePerformanceCounters("test", true)
                .BuildCluster();
        }

        private static async Task MainAsync(ICluster cluster)
        {
            Console.WriteLine("Press Enter to begin");
            Console.ReadLine();
            Console.WriteLine("Clearing Data");
            await ClearData(cluster).ConfigureAwait(false);
            Console.WriteLine("Inserting Seed Data");
            await InsertData(cluster).ConfigureAwait(false);

            var cancellationTokenSource = new CancellationTokenSource();
            Console.WriteLine("Running CRUD (errors will show up as + (query error) or * (insert/update error))");
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < _numConcurrentWorkers; i++)
            {
                tasks.Add(DoWork(cluster, cancellationTokenSource.Token));
            }

            Console.WriteLine("Press Enter to shutdown");
            Console.ReadLine();
            cancellationTokenSource.Cancel();
            Task.WaitAll(tasks.ToArray());
        }

        private async static Task ClearData(ICluster cluster)
        {
            var binding = new CurrentPrimaryBinding(cluster);
            var commandOp = new DropDatabaseOperation(_database);
            await commandOp.ExecuteAsync(binding);
        }

        private async static Task InsertData(ICluster cluster)
        {
            var binding = new CurrentPrimaryBinding(cluster);
            for (int i = 0; i < 100; i++)
            {
                await Insert(binding, new BsonDocument("i", i)).ConfigureAwait(false);
            }
        }

        private async static Task DoWork(ICluster cluster, CancellationToken cancellationToken)
        {
            var rand = new Random();
            var binding = new ConsistentPrimaryBinding(cluster);
            while (!cancellationToken.IsCancellationRequested)
            {
                var i = rand.Next(0, 10000);
                IReadOnlyList<BsonDocument> docs;
                Cursor<BsonDocument> result = null;
                try
                {
                    result = await Query(binding, new BsonDocument("i", i)).ConfigureAwait(false);
                    if (await result.MoveNextAsync().ConfigureAwait(false))
                    {
                        docs = result.Current;
                    }
                    else
                    {
                        docs = null;
                    }
                    //Console.Write(".");
                }
                catch (Exception)
                {
                    Console.Write("+");
                    continue;
                }
                finally
                {
                    if (result != null)
                    {
                        result.Dispose();
                    }
                }

                if (docs == null || docs.Count == 0)
                {
                    try
                    {
                        await Insert(binding, new BsonDocument().Add("i", i)).ConfigureAwait(false);
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
                        var query = new BsonDocument("_id", docs[0]["_id"]);
                        var update = new BsonDocument("$set", new BsonDocument("i", i + 1));
                        await Update(binding, query, update).ConfigureAwait(false);
                        //Console.Write(".");
                    }
                    catch (Exception)
                    {
                        Console.Write("*");
                    }
                }
            }
        }

        private static Task Insert(IWriteBinding binding, BsonDocument document)
        {
            var insertOp = new InsertOpcodeOperation<BsonDocument>(
                _database,
                _collection,
                BsonDocumentSerializer.Instance,
                new BatchableSource<BsonDocument>(new[] { document }));

            return insertOp.ExecuteAsync(binding);
        }

        private static Task<Cursor<BsonDocument>> Query(IReadBinding binding, BsonDocument query)
        {
            var queryOp = new FindOperation<BsonDocument>(
                _database,
                _collection,
                BsonDocumentSerializer.Instance,
                query).WithLimit(1);

            return queryOp.ExecuteAsync(binding);
        }

        private static Task Update(IWriteBinding binding, BsonDocument query, BsonDocument update)
        {
            var updateOp = new UpdateOpcodeOperation(
                _database,
                _collection,
                query,
                update);

            return updateOp.ExecuteAsync(binding);
        }
    }
}