using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.TestConsoleApplication
{
    public static class Program
    {
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

            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
        }

        private static ICluster CreateCluster()
        {
            //var clusterSettings = new ClusterSettings();
            //var serverSettings = new ServerSettings();
            //var connectionSettings = new ConnectionSettings();
            //var streamSettings = new TcpStreamSettings();
            //var streamFactory = new TcpStreamFactory(streamSettings);
            //var messageListener = (IMessageListener)null;
            //var connectionFactory = new BinaryConnectionFactory(connectionSettings, streamFactory, messageListener);
            //var connectionPoolSettings = new ConnectionPoolSettings();
            //var connectionPoolFactory = new ConnectionPoolFactory(connectionFactory, connectionPoolSettings);
            //var serverListener = (IServerListener)null;
            //var serverFactory = new ServerFactory(serverSettings, connectionPoolFactory, serverListener);
            //var clusterListener = (IClusterListener)null;
            //var clusterFactory = new ClusterFactory(clusterSettings, serverFactory, clusterListener);
            //var cluster = clusterFactory.Create();

            var logger = new Logger(Console.Out, new StreamWriter("log.txt"));

            return new ClusterBuilder()
                .ConfigureWithConnectionString("mongodb://localhost")
                .ConfigureServer(s => s
                    .WithHeartbeatInterval(TimeSpan.FromMinutes(1))
                    .WithHeartbeatTimeout(TimeSpan.FromMinutes(1)))
                .SetClusterListener(logger)
                .SetServerListener(logger)
                .SetMessageListener(logger)
                .BuildCluster();
        }

        private static async Task MainAsync(ICluster cluster)
        {
            var binding = new CurrentPrimaryBinding(cluster);
            var findOperation = new FindOperation("test", "test");
            var cursor = await findOperation.ExecuteAsync(binding);
            while (await cursor.MoveNextAsync())
            {
                var batch = cursor.Current;
                foreach (var document in batch)
                {
                    Console.WriteLine(document.ToJson());
                }
            }
        }
    }
}
