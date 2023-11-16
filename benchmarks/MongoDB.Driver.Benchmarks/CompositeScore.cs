/* Copyright 2021-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Linq;
using System.Collections.Generic;

namespace MongoDB.Benchmarks
{
    public class CompositeScore
    {
        private readonly List<BenchmarkResult> _benchmarkResults;

        public CompositeScore(List<BenchmarkResult> benchmarkResults)
        {
            _benchmarkResults = benchmarkResults;
        }

        public double GetScore(string benchmarkCategory)
        {
            var identifiedBenchmarksScores = _benchmarkResults
                .Where(benchmark => benchmark.Categories.Contains(benchmarkCategory))
                .Select(benchmark => benchmark.Score).ToList();

            if (identifiedBenchmarksScores.Any())
            {
                return identifiedBenchmarksScores.Average();
            }

            return 0;
        }
    }
}
