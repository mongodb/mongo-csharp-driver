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

namespace MongoDB.Driver.Core.TestConsoleApplication
{
    public static class Program
    {
        private static CollectionNamespace __collection = new CollectionNamespace("foo", "bar");
        private static MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static int __numConcurrentWorkers = 8;

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
            }

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();
        }

        private static ICluster CreateCluster()
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var file = Path.Combine(desktop, "log.txt");
            var streamWriter = new StreamWriter(file);
            var writer = TextWriter.Synchronized(streamWriter);

            return new ClusterBuilder()
                .ConfigureWithConnectionString("mongodb://localhost")
                .ConfigureServer(s => s
                    .WithHeartbeatInterval(TimeSpan.FromMinutes(1))
                    .WithHeartbeatTimeout(TimeSpan.FromMinutes(1)))
                .ConfigureConnection(s => s
                    .WithMaxLifeTime(TimeSpan.FromSeconds(30)))
                .AddListener(new LogListener(writer, LogLevel.Debug))
                // .UsePerformanceCounters("test", true)
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
            for (int i = 0; i < __numConcurrentWorkers; i++)
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
            using (var binding = new WritableServerBinding(cluster))
            {
                var commandOp = new DropDatabaseOperation(__collection.DatabaseNamespace, __messageEncoderSettings);
                await commandOp.ExecuteAsync(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);
            }
        }

        private async static Task InsertData(ICluster cluster)
        {
            using (var binding = new WritableServerBinding(cluster))
            {
                for (int i = 0; i < 100; i++)
                {
                    await Insert(binding, new BsonDocument("i", i)).ConfigureAwait(false);
                }
            }
        }

        private async static Task DoWork(ICluster cluster, CancellationToken cancellationToken)
        {
            var rand = new Random();
            using (var binding = new WritableServerBinding(cluster))
            while (!cancellationToken.IsCancellationRequested)
            {
                var i = rand.Next(0, 10000);
                IReadOnlyList<BsonDocument> docs;
                IAsyncCursor<BsonDocument> enumerator = null;
                try
                {
                    enumerator = await Query(binding, new BsonDocument("i", i)).ConfigureAwait(false);
                    if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                    {
                        docs = enumerator.Current.ToList();
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
                    if (enumerator != null)
                    {
                        enumerator.Dispose();
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
                        var criteria = new BsonDocument("_id", docs[0]["_id"]);
                        var update = new BsonDocument("$set", new BsonDocument("i", i + 1));
                        await Update(binding, criteria, update).ConfigureAwait(false);
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
            var documentSource = new BatchableSource<BsonDocument>(new[] { document });
            var insertOp = new InsertOpcodeOperation<BsonDocument>(__collection, documentSource, BsonDocumentSerializer.Instance, __messageEncoderSettings);

            return insertOp.ExecuteAsync(binding);
        }

        private static Task<IAsyncCursor<BsonDocument>> Query(IReadBinding binding, BsonDocument query)
        {
            var findOp = new FindOperation<BsonDocument>(__collection, BsonDocumentSerializer.Instance, __messageEncoderSettings)
            {
                Criteria = query,
                Limit = -1
            };

            return findOp.ExecuteAsync(binding, Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        private static Task Update(IWriteBinding binding, BsonDocument criteria, BsonDocument update)
        {
            var updateOp = new UpdateOpcodeOperation(
                __collection,
                new UpdateRequest(UpdateType.Update, criteria, update),
                __messageEncoderSettings);

            return updateOp.ExecuteAsync(binding);
        }
    }
}