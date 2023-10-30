using System.Linq;
using System.Collections.Generic;

namespace benchmarks
{
    public class CompositeScore
    {
        private readonly List<BenchmarkResult> _benchmarkResults;

        public CompositeScore(List<BenchmarkResult> benchmarkResults)
        {
            _benchmarkResults = benchmarkResults;
        }

        public double GetScore(IReadOnlyCollection<string> filterGroup)
        {
            var identifiedBenchmarksScores = _benchmarkResults
                .Where(benchmark => filterGroup.Contains(benchmark.Name))
                .Select(benchmark => benchmark.Score).ToList();

            if (identifiedBenchmarksScores.Any())
            {
                return identifiedBenchmarksScores.Average();
            }

            return 0;
        }
    }
}
