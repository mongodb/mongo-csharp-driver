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

using BenchmarkDotNet.Reports;
using System.Collections.Generic;

namespace MongoDB.Benchmarks
{
    public sealed class BenchmarkResult
    {
        public string Name { get; }
        public double Score { get; }
        public IEnumerable<string> Categories { get; }

        public BenchmarkResult(BenchmarkReport benchmarkReport, string name, int datasetSize)
        {
            Name = name;
            Categories = benchmarkReport.BenchmarkCase.Descriptor.Categories;
            Score = datasetSize / (benchmarkReport.ResultStatistics.Median / 1000);
        }
    }
}
