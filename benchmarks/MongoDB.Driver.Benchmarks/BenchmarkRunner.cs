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

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using MongoDB.Benchmarks.Exporters;
using NDesk.Options;

namespace MongoDB.Benchmarks
{
    public class BenchmarkRunner
    {
        public static void Main(string[] args)
        {
            var executingDriverBenchmarks = false;
            var exportingToEvergreen = false;
            var evergreenOutputFile = "evergreen-results.json"; // default output file name

            var parser = new OptionSet
            {
                { "evergreen", v => exportingToEvergreen = v != null },
                { "driverBenchmarks", v => executingDriverBenchmarks = v != null },
                { "o|output-file=", v => evergreenOutputFile = v }
            };

            // the parser will try to parse the options defined above and will return any extra options
            var benchmarkSwitcherArgs = parser.Parse(args).ToArray();

            var config = DefaultConfig.Instance;

            config = executingDriverBenchmarks
                ? config
                    .WithOption(ConfigOptions.JoinSummary, true)
                    .AddExporter(new LocalExporter())
                    .HideColumns("BenchmarkDataSetSize")
                : config;

            config = exportingToEvergreen ? config.AddExporter(new EvergreenExporter(evergreenOutputFile)) : config;

            BenchmarkSwitcher.FromAssembly(typeof(BenchmarkRunner).Assembly).Run(benchmarkSwitcherArgs, config);
        }
    }
}
