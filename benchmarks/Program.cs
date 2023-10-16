using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using benchmarks.BSON;
using benchmarks.Multi_Doc;
using benchmarks.Single_Doc;

namespace benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            var summary = BenchmarkRunner.Run<GridFsDownloadBenchmark>();

            // Statistics resultStatistics = summary.Reports.First().ResultStatistics;
            // if (resultStatistics != null)
            // {
            //     var measurements = resultStatistics.OriginalValues.Count;
            //     Console.WriteLine($"Total measurements run: {measurements}");
            // }

            // Use this to select benchmarks from the console:
            // var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
