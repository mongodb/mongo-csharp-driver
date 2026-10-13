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
    /// Handle for managing stream processors in a workspace. Obtained from
    /// <see cref="StreamProcessingClient.StreamProcessorsView"/>.
    /// </summary>
    public sealed class StreamProcessors
    {
        private readonly IMongoDatabase _admin;

        internal StreamProcessors(IMongoDatabase admin)
        {
            _admin = admin ?? throw new ArgumentNullException(nameof(admin));
        }

        /// <summary>Creates a new stream processor.</summary>
        public void Create(string name, IEnumerable<BsonDocument> pipeline, CreateStreamProcessorOptions options = null, CancellationToken cancellationToken = default)
        {
            _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(BuildCreateCommand(name, pipeline, options)), cancellationToken: cancellationToken);
        }

        /// <summary>Creates a new stream processor asynchronously.</summary>
        public Task CreateAsync(string name, IEnumerable<BsonDocument> pipeline, CreateStreamProcessorOptions options = null, CancellationToken cancellationToken = default)
        {
            return _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(BuildCreateCommand(name, pipeline, options)), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns a handle for the named processor. Does not imply that the
        /// processor currently exists on the server.
        /// </summary>
        public StreamProcessor Get(string name)
        {
            return new StreamProcessor(_admin, name);
        }

        /// <summary>Returns information about a single stream processor.</summary>
        public StreamProcessorInfo GetInfo(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            var response = _admin.RunCommand(new BsonDocumentCommand<BsonDocument>(new BsonDocument("getStreamProcessor", name)), cancellationToken: cancellationToken);
            return new StreamProcessorInfo(UnwrapResult(response));
        }

        /// <summary>Returns information about a single stream processor asynchronously.</summary>
        public async Task<StreamProcessorInfo> GetInfoAsync(string name, CancellationToken cancellationToken = default)
        {
            ValidateName(name);
            var response = await _admin.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(new BsonDocument("getStreamProcessor", name)), cancellationToken: cancellationToken).ConfigureAwait(false);
            return new StreamProcessorInfo(UnwrapResult(response));
        }

        private static BsonDocument BuildCreateCommand(string name, IEnumerable<BsonDocument> pipeline, CreateStreamProcessorOptions options)
        {
            ValidateName(name);
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

            var pipelineArray = new BsonArray();
            foreach (var stage in pipeline)
            {
                if (stage != null) pipelineArray.Add(stage);
            }

            var cmd = new BsonDocument
            {
                { "createStreamProcessor", name },
                { "pipeline", pipelineArray }
            };

            if (options == null) return cmd;

            var sub = new BsonDocument();
            if (options.Dlq != null) sub["dlq"] = options.Dlq;
            if (options.StreamMetaFieldName != null) sub["streamMetaFieldName"] = options.StreamMetaFieldName;
            if (options.Tier != null) sub["tier"] = options.Tier;
            if (options.Failover.HasValue) sub["failover"] = options.Failover.Value;
            if (sub.ElementCount > 0) cmd["options"] = sub;

            return cmd;
        }

        private static BsonDocument UnwrapResult(BsonDocument response)
        {
            // Dev-server deviation: some server builds wrap the processor
            // document in a top-level "result" key. Unwrap if present so the
            // caller sees a flat document matching the spec.
            if (response != null && response.TryGetValue("result", out var resultValue) && resultValue.IsBsonDocument)
            {
                return resultValue.AsBsonDocument;
            }
            return response ?? new BsonDocument();
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name must be non-empty.", nameof(name));
            }
        }
    }
}
