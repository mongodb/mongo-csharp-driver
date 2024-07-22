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

using System.CommandLine;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using MongoDB.Benchmarks.Exporters;

namespace MongoDB.Benchmarks
{
    public class BenchmarkRunner
    {
        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand("CSharp Driver benchmarks runner");
            rootCommand.TreatUnmatchedTokensAsErrors = false;
            var evergreenOption = new Option<bool>("--evergreen", () => false);
            rootCommand.AddOption(evergreenOption);
            var driverBenchmarksOption = new Option<bool>("--driverBenchmarks", () => false);
            rootCommand.AddOption(driverBenchmarksOption);
            var evergreenOutputFileOption = new Option<string>(["--o", "--output-file"], () => "evergreen-results.json");
            rootCommand.AddOption(evergreenOutputFileOption);

            rootCommand.SetHandler(invocationContext =>
            {
                var evergreenValue = invocationContext.ParseResult.GetValueForOption(evergreenOption);
                var driverBenchmarksValue = invocationContext.ParseResult.GetValueForOption(driverBenchmarksOption);
                var evergreenOutputFileValue = invocationContext.ParseResult.GetValueForOption(evergreenOutputFileOption);

                var config = DefaultConfig.Instance;

                // use a modified config if running driver benchmarks
                if (driverBenchmarksValue)
                {
                    config = config
                        .WithOption(ConfigOptions.JoinSummary, true)
                        .AddExporter(new LocalExporter())
                        .HideColumns("BenchmarkDataSetSize");
                }

                if (evergreenValue)
                {
                    config = config.AddExporter(new EvergreenExporter(evergreenOutputFileValue));
                }

                BenchmarkSwitcher.FromAssembly(typeof(BenchmarkRunner).Assembly).Run(invocationContext.ParseResult.UnmatchedTokens.ToArray(), config);
            });

            return rootCommand.Invoke(args);
        }
    }
}
