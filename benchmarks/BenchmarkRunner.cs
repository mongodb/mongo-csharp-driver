using NDesk.Options;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using benchmarks.CustomExporters;

namespace benchmarks
{
    public class BenchmarkRunner
    {
        public static void Main(string[] args)
        {
            bool exportingToEvergreen = false;
            string outputFile = "evergreen-results.json";

            var parser = new OptionSet
            {
                {
                    "evergreen", v => exportingToEvergreen = v != null
                },
                {
                    "o|output-file=", v => outputFile = v
                }
            };

            // the parser will try to parse the options defined above and will return any extra options
            string[] benchmarkSwitcherArgs = parser.Parse(args).ToArray();

            var config = DefaultConfig.Instance.WithOption(ConfigOptions.JoinSummary, true).AddExporter(new LocalExporter());

            if (exportingToEvergreen)
            {
                config = config.AddExporter(new EvergreenExporter(outputFile));
            }

            BenchmarkSwitcher.FromAssembly(typeof(BenchmarkRunner).Assembly).Run(benchmarkSwitcherArgs, config);
        }
    }
}
