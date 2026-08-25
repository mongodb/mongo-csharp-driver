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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.StreamProcessing;
using Xunit;

namespace MongoDB.Driver.Tests.StreamProcessing
{
    /// <summary>
    /// Functional smoke test for Atlas Stream Processing. Skipped unless
    /// <c>MONGODB_STREAM_PROCESSING_URI</c> is set to a workspace endpoint
    /// (<c>atlas-stream-*.&lt;region&gt;.a.query.mongodb{,-stage}.net</c>)
    /// with valid credentials.
    /// </summary>
    [Trait("Category", "Integration")]
    public class StreamProcessingLifecycleTests
    {
        private static string WorkspaceUri => Environment.GetEnvironmentVariable("MONGODB_STREAM_PROCESSING_URI");

        private static bool ShouldRun
        {
            get
            {
                var uri = WorkspaceUri;
                return !string.IsNullOrEmpty(uri) && StreamProcessingClient.IsWorkspaceUri(uri);
            }
        }

        [Fact]
        public async Task Lifecycle_create_start_stats_sample_stop_drop()
        {
            if (!ShouldRun)
            {
                // MONGODB_STREAM_PROCESSING_URI not configured to a workspace endpoint; skip.
                return;
            }

            var client = new StreamProcessingClient(WorkspaceUri);
            var processors = client.StreamProcessorsView;
            var name = "csharpdriver_test_" + ObjectId.GenerateNewId();

            var pipeline = new List<BsonDocument>
            {
                new BsonDocument("$source", new BsonDocument("connectionName", "sample_stream_solar")),
                new BsonDocument("$emit", new BsonDocument
                {
                    { "connectionName", "__testLog" },
                    { "topic", "csharp-driver-demo" }
                })
            };

            try
            {
                // create
                await processors.CreateAsync(name, pipeline).ConfigureAwait(false);
                var info = await processors.GetInfoAsync(name).ConfigureAwait(false);
                info.Name.Should().Be(name);
                new[] { "CREATED", "VALIDATING", "CREATING" }.Should().Contain(info.State);

                // start
                var processor = processors.Get(name);
                await processor.StartAsync().ConfigureAwait(false);

                var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
                string state;
                do
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    state = (await processors.GetInfoAsync(name).ConfigureAwait(false)).State;
                } while (state != "STARTED" && DateTime.UtcNow < deadline);

                state.Should().Be("STARTED");

                // stats
                var stats = await processor.StatsAsync().ConfigureAwait(false);
                stats.Should().NotBeNull();

                // sample: open + fetch one batch
                var opened = await processor.SamplesAsync(new GetStreamProcessorSamplesOptions { Limit = 5 }).ConfigureAwait(false);
                opened.CursorId.Should().BeGreaterThan(0);
                opened.Documents.Should().BeEmpty();

                var batch = await processor.SamplesAsync(new GetStreamProcessorSamplesOptions
                {
                    CursorId = opened.CursorId,
                    BatchSize = 5
                }).ConfigureAwait(false);
                batch.CursorId.Should().BeGreaterOrEqualTo(0);

                // stop + drop
                await processor.StopAsync().ConfigureAwait(false);
                await processor.DropAsync().ConfigureAwait(false);
            }
            catch
            {
                // best-effort cleanup
                try
                {
                    await processors.Get(name).DropAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
                throw;
            }
        }
    }
}
