using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace BenchmarkLibMongoCrypt
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance;
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
