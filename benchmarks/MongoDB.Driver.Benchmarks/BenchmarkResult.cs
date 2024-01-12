/* Copyright 2010-present MongoDB Inc.
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

using System.Collections.Generic;
using BenchmarkDotNet.Reports;
using MongoDB.Benchmarks.Bson;

namespace MongoDB.Benchmarks
{
    public sealed class BenchmarkResult
    {
        public HashSet<string> Categories { get; }
        public string Name { get; }
        public double Score { get; }

        public BenchmarkResult(BenchmarkReport benchmarkReport)
        {
            int dataSetSize;
            if (benchmarkReport.BenchmarkCase.Descriptor.HasCategory(DriverBenchmarkCategory.BsonBench))
            {
                var bsonBenchmarkData = (BsonBenchmarkData)benchmarkReport.BenchmarkCase.Parameters["BenchmarkData"];
                Name = bsonBenchmarkData.DataSetName + benchmarkReport.BenchmarkCase.Descriptor.Type.Name;
                dataSetSize = bsonBenchmarkData.DataSetSize;
            }
            else
            {
                Name = benchmarkReport.BenchmarkCase.Descriptor.Type.Name;
                dataSetSize = (int)benchmarkReport.BenchmarkCase.Parameters["BenchmarkDataSetSize"];
            }

            Categories = new HashSet<string>(benchmarkReport.BenchmarkCase.Descriptor.Categories);

            // change the median from nanoseconds to seconds for calculating the score.
            // since dataSetSize is in bytes, divide the score to convert to MB/s
            Score = (dataSetSize / (benchmarkReport.ResultStatistics.Median / 1_000_000_000D)) / 1_000_000D;
        }
    }
}
