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

namespace MongoDB.Benchmarks;

public sealed class BenchmarkResult
{
    public HashSet<string> Categories { get; }
    public string Name { get; }
    public double Score { get; }
    public string Unit { get; }
    public string MetricName { get; }

    public BenchmarkResult(BenchmarkReport benchmarkReport)
    {
        var benchmarkCaseDescriptor = benchmarkReport.BenchmarkCase.Descriptor;
        var methodName = benchmarkCaseDescriptor.WorkloadMethod.Name;
        Categories = new HashSet<string>(benchmarkCaseDescriptor.Categories);

        if (Categories.Contains(DriverBenchmarkCategory.BsonBench))
        {
            var bsonBenchmarkData = (BsonBenchmarkData)benchmarkReport.BenchmarkCase.Parameters["BenchmarkData"];
            Name = bsonBenchmarkData.DataSetName + benchmarkCaseDescriptor.Type.Name;

            var dataSetSize = bsonBenchmarkData.DataSetSize;
            // dataSetSize is in bytes, median is in nanoseconds — score is MB/s
            Score = (dataSetSize / (benchmarkReport.ResultStatistics.Median / 1_000_000_000D)) / 1_000_000D;
            Unit = "MB/s";
            MetricName = "megabytes_per_second";
        }
        else if (Categories.Contains(DriverBenchmarkCategory.LinqBench))
        {
            Name = methodName;
            // median is in nanoseconds — score is translations/second
            Score = 1_000_000_000D / benchmarkReport.ResultStatistics.Median;
            Unit = "translations/s";
            MetricName = "translations_per_second";
        }
        else
        {
            Name = Categories.Contains(DriverBenchmarkCategory.BulkWriteBench)
                ? methodName
                : benchmarkCaseDescriptor.Type.Name;

            var dataSetSize = (int)benchmarkReport.BenchmarkCase.Parameters["BenchmarkDataSetSize"];
            // dataSetSize is in bytes, median is in nanoseconds — score is MB/s
            Score = (dataSetSize / (benchmarkReport.ResultStatistics.Median / 1_000_000_000D)) / 1_000_000D;
            Unit = "MB/s";
            MetricName = "megabytes_per_second";
        }

        if (methodName.EndsWith("Poco") || methodName.EndsWith("PocoBenchmark"))
        {
            Name = Name.Replace("Benchmark", "PocoBenchmark");
        }
    }
}
