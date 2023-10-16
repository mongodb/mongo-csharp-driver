using System;
using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

namespace benchmarks
{
    [Config(typeof(Config))]
    public class TestBench
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(
                    Job.Dry
                        .WithMinIterationTime(TimeInterval.FromMinutes(1))
                        .WithIterationTime(TimeInterval.FromMinutes(5))
                        .WithId("FlatBsonEncoding"));
            }
        }

        [Benchmark]
        public void Scenario1()
        {
            // Implement your benchmark here
        }
    }
}
