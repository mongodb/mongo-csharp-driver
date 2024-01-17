using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace MongoDB.Libmongocrypt.Benchmarks
{
    public class BenchmarkRunner
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            BenchmarkSwitcher.FromAssembly(typeof(BenchmarkRunner).Assembly).Run(args, config);
        }
    }
}
