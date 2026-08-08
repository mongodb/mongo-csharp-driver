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
using MongoDB.Bson;

namespace MongoDB.Driver.StreamProcessing
{
    /// <summary>
    /// Handle for a specific named stream processor.
    /// </summary>
    /// <remarks>
    /// Holding a handle does not imply the processor currently exists on the
    /// server. Obtained via <see cref="StreamProcessors.Get"/>.
    /// </remarks>
    public sealed class StreamProcessor
    {
        private static readonly HashSet<string> ValidFailoverModes = new HashSet<string>(StringComparer.Ordinal) { "GRACEFUL", "FORCED" };

        private readonly IMongoDatabase _admin;
        private readonly string _name;

        internal StreamProcessor(IMongoDatabase admin, string name)
        {
            if (admin == null) throw new ArgumentNullException(nameof(admin));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("name must be non-empty.", nameof(name));

            _admin = admin;
            _name = name;
        }

        /// <summary>The processor name.</summary>
        public string Name => _name;

        /// <summary>Starts the processor.</summary>
        public void Start(StartStreamProcessorOptions options = null, CancellationToken cancellationToken = default)
        {
            _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(BuildStartCommand(options)), cancellationToken: cancellationToken);
        }

        /// <summary>Starts the processor asynchronously.</summary>
        public Task StartAsync(StartStreamProcessorOptions options = null, CancellationToken cancellationToken = default)
        {
            return _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(BuildStartCommand(options)), cancellationToken: cancellationToken);
        }

        /// <summary>Stops the processor. The processor remains in STOPPED and can be restarted.</summary>
        public void Stop(CancellationToken cancellationToken = default)
        {
            _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(new BsonDocument("stopStreamProcessor", _name)), cancellationToken: cancellationToken);
        }

        /// <summary>Stops the processor asynchronously.</summary>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(new BsonDocument("stopStreamProcessor", _name)), cancellationToken: cancellationToken);
        }

        /// <summary>Drops the processor permanently. A dropped processor cannot be recovered.</summary>
        public void Drop(CancellationToken cancellationToken = default)
        {
            _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(new BsonDocument("dropStreamProcessor", _name)), cancellationToken: cancellationToken);
        }

        /// <summary>Drops the processor asynchronously.</summary>
        public Task DropAsync(CancellationToken cancellationToken = default)
        {
            return _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(new BsonDocument("dropStreamProcessor", _name)), cancellationToken: cancellationToken);
        }

        /// <summary>Returns runtime statistics for the processor. The processor must be in STARTED.</summary>
        public BsonDocument Stats(GetStreamProcessorStatsOptions options = null, CancellationToken cancellationToken = default)
        {
            return _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(BuildStatsCommand(options)), cancellationToken: cancellationToken);
        }

        /// <summary>Returns runtime statistics asynchronously.</summary>
        public Task<BsonDocument> StatsAsync(GetStreamProcessorStatsOptions options = null, CancellationToken cancellationToken = default)
        {
            return _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(BuildStatsCommand(options)), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Retrieves a batch of sampled documents.
        /// </summary>
        /// <remarks>
        /// Routes to <c>startSampleStreamProcessor</c> when no CursorId is supplied (or
        /// CursorId is 0); otherwise routes to <c>getMoreSampleStreamProcessor</c> with
        /// the supplied cursor id. The caller MUST stop iterating when the
        /// returned <see cref="StreamProcessorSamples.CursorId"/> is 0.
        /// </remarks>
        public StreamProcessorSamples Samples(GetStreamProcessorSamplesOptions options = null, CancellationToken cancellationToken = default)
        {
            var cursorId = options?.CursorId ?? 0;
            if (cursorId == 0)
            {
                var doc = _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(BuildStartSampleCommand(options)), cancellationToken: cancellationToken);
                return new StreamProcessorSamples(ReadCursorId(doc), new List<BsonDocument>());
            }

            var more = _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(BuildGetMoreSampleCommand(cursorId, options)), cancellationToken: cancellationToken);
            return BuildSamplesResult(more);
        }

        /// <summary>Retrieves a batch of sampled documents asynchronously.</summary>
        public async Task<StreamProcessorSamples> SamplesAsync(GetStreamProcessorSamplesOptions options = null, CancellationToken cancellationToken = default)
        {
            var cursorId = options?.CursorId ?? 0;
            if (cursorId == 0)
            {
                var doc = await _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(BuildStartSampleCommand(options)), cancellationToken: cancellationToken).ConfigureAwait(false);
                return new StreamProcessorSamples(ReadCursorId(doc), new List<BsonDocument>());
            }

            var more = await _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(BuildGetMoreSampleCommand(cursorId, options)), cancellationToken: cancellationToken).ConfigureAwait(false);
            return BuildSamplesResult(more);
        }

        private BsonDocument BuildStartCommand(StartStreamProcessorOptions options)
        {
            var cmd = new BsonDocument("startStreamProcessor", _name);
            if (options == null)
            {
                return cmd;
            }

            if (options.Workers.HasValue)
            {
                cmd["workers"] = options.Workers.Value;
            }

            // Spec puts these option fields under a nested "options" document.
            var sub = new BsonDocument();
            if (options.ClearCheckpoints.HasValue) sub["clearCheckpoints"] = options.ClearCheckpoints.Value;
            if (options.StartAtOperationTime != null) sub["startAtOperationTime"] = options.StartAtOperationTime;
            if (options.Tier != null) sub["tier"] = options.Tier;
            if (options.EnableAutoScaling.HasValue) sub["enableAutoScaling"] = options.EnableAutoScaling.Value;
            if (sub.ElementCount > 0) cmd["options"] = sub;

            if (options.Failover != null)
            {
                if (string.IsNullOrEmpty(options.Failover.Region))
                {
                    throw new ArgumentException("Failover.Region is required when Failover is set.", nameof(options));
                }

                if (options.Failover.Mode != null && !ValidFailoverModes.Contains(options.Failover.Mode))
                {
                    throw new ArgumentException(
                        $"Invalid Failover.Mode \"{options.Failover.Mode}\"; expected one of: GRACEFUL, FORCED.",
                        nameof(options));
                }

                var f = new BsonDocument("region", options.Failover.Region);
                if (options.Failover.Mode != null) f["mode"] = options.Failover.Mode;
                if (options.Failover.DryRun.HasValue) f["dryRun"] = options.Failover.DryRun.Value;
                cmd["failover"] = f;
            }

            return cmd;
        }

        private BsonDocument BuildStatsCommand(GetStreamProcessorStatsOptions options)
        {
            var cmd = new BsonDocument("getStreamProcessorStats", _name);
            if (options?.Verbose.HasValue == true)
            {
                cmd["options"] = new BsonDocument("verbose", options.Verbose.Value);
            }
            return cmd;
        }

        private BsonDocument BuildStartSampleCommand(GetStreamProcessorSamplesOptions options)
        {
            var cmd = new BsonDocument("startSampleStreamProcessor", _name);
            if (options?.Limit.HasValue == true)
            {
                cmd["limit"] = options.Limit.Value;
            }
            return cmd;
        }

        private BsonDocument BuildGetMoreSampleCommand(long cursorId, GetStreamProcessorSamplesOptions options)
        {
            var cmd = new BsonDocument
            {
                { "getMoreSampleStreamProcessor", _name },
                { "cursorId", cursorId }
            };
            if (options?.BatchSize.HasValue == true)
            {
                cmd["batchSize"] = options.BatchSize.Value;
            }
            return cmd;
        }

        private static long ReadCursorId(BsonDocument doc)
        {
            if (doc == null || !doc.TryGetValue("cursorId", out var value))
            {
                throw new InvalidOperationException("startSampleStreamProcessor did not return a cursorId.");
            }
            return value.ToInt64();
        }

        private static StreamProcessorSamples BuildSamplesResult(BsonDocument response)
        {
            var cursorId = response.TryGetValue("cursorId", out var cidValue) ? cidValue.ToInt64() : 0L;

            // Dev-server deviation: some server builds use "messages" instead of
            // "nextBatch". Prefer the spec-defined "nextBatch" but fall back to
            // "messages" when present.
            BsonValue batchValue;
            if (!response.TryGetValue("nextBatch", out batchValue))
            {
                response.TryGetValue("messages", out batchValue);
            }

            var documents = new List<BsonDocument>();
            if (batchValue != null && batchValue.IsBsonArray)
            {
                foreach (var item in batchValue.AsBsonArray)
                {
                    if (item.IsBsonDocument)
                    {
                        documents.Add(item.AsBsonDocument);
                    }
                }
            }

            return new StreamProcessorSamples(cursorId, documents);
        }
    }
}
