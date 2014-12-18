using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events.Diagnostics;

namespace MongoDB.Driver.TestConsoleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            int numConcurrentWorkers = 8;
            //new CoreApi().Run(numConcurrentWorkers, ConfigureCluster);

            //new Api().Run(numConcurrentWorkers, ConfigureCluster);

            new LegacyApi().Run(numConcurrentWorkers, ConfigureCluster);
        }

        private static void ConfigureCluster(ClusterBuilder cb)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var file = Path.Combine(desktop, "log.txt");
            var streamWriter = new StreamWriter(file);
            var writer = TextWriter.Synchronized(streamWriter);

            cb.AddListener(new LogListener(writer, LogLevel.Error));
            cb.UsePerformanceCounters("test", true);
        }
    }
}