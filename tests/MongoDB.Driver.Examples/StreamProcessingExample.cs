/* Copyright 2026-present MongoDB Inc.
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.StreamProcessing;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Examples
{
    /// <summary>
    /// Demonstrates the full lifecycle of an Atlas Stream Processing (ASP)
    /// stream processor: create, start, sample, stop, drop.
    ///
    /// Requirements:
    ///   - An Atlas Stream Processing workspace endpoint.
    ///   - A user with the atlasAdmin role.
    ///   - Two connections registered: sample_stream_solar (source) and __testLog (sink).
    ///
    /// Run with:
    ///   MONGODB_STREAM_PROCESSING_URI='mongodb://user:pass@atlas-stream-….a.query.mongodb.net/' \
    ///     dotnet test tests/MongoDB.Driver.Examples/MongoDB.Driver.Examples.csproj \
    ///     --filter "FullyQualifiedName~StreamProcessingExample"
    ///
    /// Self-skips when the env var is unset.
    /// </summary>
    [Trait("Category", "Integration")]
    public class StreamProcessingExample
    {
        private readonly ITestOutputHelper _output;

        public StreamProcessingExample(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Lifecycle()
        {
            var uri = Environment.GetEnvironmentVariable("MONGODB_STREAM_PROCESSING_URI");
            if (string.IsNullOrEmpty(uri) || !StreamProcessingClient.IsWorkspaceUri(uri))
            {
                _output.WriteLine("Skipping: MONGODB_STREAM_PROCESSING_URI is not configured to a workspace endpoint.");
                return;
            }

            var client = new StreamProcessingClient(uri);
            var processors = client.StreamProcessorsView;
            var name = "csharpdriver_demo_" + ObjectId.GenerateNewId();

            _output.WriteLine($"Workspace: {uri}");
            _output.WriteLine($"Processor: {name}");
            _output.WriteLine("");

            var created = false;
            try
            {
                var pipeline = new List<BsonDocument>
                {
                    new BsonDocument("$source", new BsonDocument("connectionName", "sample_stream_solar")),
                    new BsonDocument("$emit", new BsonDocument
                    {
                        { "connectionName", "__testLog" },
                        { "topic", "csharp-driver-demo" }
                    })
                };

                // 1. create
                _output.WriteLine($"[1/6] create({name})");
                await processors.CreateAsync(name, pipeline).ConfigureAwait(false);
                created = true;
                var info = await processors.GetInfoAsync(name).ConfigureAwait(false);
                _output.WriteLine($"      state={info.State}");
                _output.WriteLine("");

                // 2. start
                _output.WriteLine("[2/6] start()");
                var processor = processors.Get(name);
                await processor.StartAsync().ConfigureAwait(false);

                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
                string state;
                do
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    state = (await processors.GetInfoAsync(name).ConfigureAwait(false)).State;
                } while (state != "STARTED" && DateTime.UtcNow < deadline);

                _output.WriteLine($"      state={state}");
                _output.WriteLine("");
                if (state != "STARTED")
                {
                    throw new InvalidOperationException($"processor did not reach STARTED within 30s (got {state})");
                }

                // 3. stats
                _output.WriteLine("[3/6] stats()");
                var stats = await processor.StatsAsync().ConfigureAwait(false);
                _output.WriteLine($"      {stats.ToJson()}");
                _output.WriteLine("");

                // 4. sample
                _output.WriteLine("[4/6] samples()");
                var opened = await processor.SamplesAsync(new GetStreamProcessorSamplesOptions { Limit = 5 }).ConfigureAwait(false);
                _output.WriteLine($"      open  cursorId={opened.CursorId} docs={opened.Documents.Count}");

                if (!opened.IsExhausted)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    var batch = await processor.SamplesAsync(new GetStreamProcessorSamplesOptions
                    {
                        CursorId = opened.CursorId,
                        BatchSize = 5
                    }).ConfigureAwait(false);
                    _output.WriteLine($"      batch cursorId={batch.CursorId} docs={batch.Documents.Count}");
                    for (var i = 0; i < batch.Documents.Count; i++)
                    {
                        _output.WriteLine($"          [{i}] {batch.Documents[i].ToJson()}");
                    }
                }
                _output.WriteLine("");

                // 5. stop
                _output.WriteLine("[5/6] stop()");
                await processor.StopAsync().ConfigureAwait(false);
                _output.WriteLine($"      state={(await processors.GetInfoAsync(name).ConfigureAwait(false)).State}");
                _output.WriteLine("");

                // 6. drop
                _output.WriteLine("[6/6] drop()");
                await processor.DropAsync().ConfigureAwait(false);
                _output.WriteLine("      dropped");
                _output.WriteLine("");

                _output.WriteLine("OK.");
            }
            catch (Exception e)
            {
                _output.WriteLine("");
                _output.WriteLine($"FAILED: {e.GetType().Name}: {e.Message}");
                if (created)
                {
                    try
                    {
                        await processors.Get(name).DropAsync().ConfigureAwait(false);
                        _output.WriteLine($"(cleaned up processor {name})");
                    }
                    catch
                    {
                        // best-effort
                    }
                }
                throw;
            }
        }
    }
}
