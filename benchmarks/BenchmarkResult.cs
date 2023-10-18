using System.Collections.Generic;
using BenchmarkDotNet.Reports;

namespace benchmarks;

public class BenchmarkResult
{
    private int _datasetSize;
    private readonly Dictionary<int, double> _percentiles;

    public string Name { get; }
    public double Score { get; }

    public BenchmarkResult(BenchmarkReport benchmarkReport, int datasetSize)
    {
        Name = benchmarkReport.BenchmarkCase.Descriptor.WorkloadMethodDisplayInfo;

        _datasetSize = datasetSize;
        _percentiles = new Dictionary<int, double>();

        foreach (int percentile in new[]{10, 25, 50, 75, 80, 90, 95, 98, 99})
        {
            _percentiles[percentile] = benchmarkReport.ResultStatistics!.Percentiles.Percentile(percentile);
        }

        Score = datasetSize / (_percentiles[50] / 1000);
    }

    public double GetPercentile(int percentile)
    {
        return _percentiles[percentile];
    }
}
