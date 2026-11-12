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

using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Driver.StreamProcessing
{
    /// <summary>
    /// Information about a single stream processor, returned by the
    /// getStreamProcessor command.
    /// </summary>
    /// <remarks>
    /// Fields the spec marks as optional may be absent depending on server
    /// version; the corresponding accessors return <c>null</c> in that case.
    /// Drivers MUST surface unknown state values as-is rather than mapping them
    /// to a closed set, so <see cref="State"/> is a plain string.
    /// </remarks>
    public sealed class StreamProcessorInfo
    {
        private readonly BsonDocument _raw;

        internal StreamProcessorInfo(BsonDocument raw)
        {
            _raw = raw ?? new BsonDocument();
        }

        /// <summary>The raw response document.</summary>
        public BsonDocument Raw => _raw;

        /// <summary>Processor id. Optional: not returned by all server versions.</summary>
        public string Id => _raw.GetValue("id", BsonNull.Value).IsBsonNull ? null : _raw["id"].AsString;

        /// <summary>Processor name.</summary>
        public string Name => _raw.GetValue("name", BsonNull.Value).IsBsonNull ? null : _raw["name"].AsString;

        /// <summary>Current state.</summary>
        public string State => _raw.GetValue("state", BsonNull.Value).IsBsonNull ? null : _raw["state"].AsString;

        /// <summary>Aggregation pipeline of the processor.</summary>
        public IReadOnlyList<BsonDocument> Pipeline
        {
            get
            {
                if (!_raw.TryGetValue("pipeline", out var v) || !v.IsBsonArray)
                {
                    return new List<BsonDocument>();
                }
                var array = v.AsBsonArray;
                var list = new List<BsonDocument>(array.Count);
                foreach (var item in array)
                {
                    if (item.IsBsonDocument)
                    {
                        list.Add(item.AsBsonDocument);
                    }
                }
                return list;
            }
        }

        /// <summary>Pipeline version. Optional: not returned by all server versions.</summary>
        public int? PipelineVersion => _raw.TryGetValue("pipelineVersion", out var v) && v.IsInt32 ? v.AsInt32 : (int?)null;

        /// <summary>Compute tier.</summary>
        public string Tier => _raw.GetValue("tier", BsonNull.Value).IsBsonNull ? null : _raw["tier"].AsString;

        /// <summary>Dead letter queue configuration.</summary>
        public BsonDocument Dlq => _raw.TryGetValue("dlq", out var v) && v.IsBsonDocument ? v.AsBsonDocument : null;

        /// <summary>Field name used for stream metadata.</summary>
        public string StreamMetaFieldName => _raw.GetValue("streamMetaFieldName", BsonNull.Value).IsBsonNull ? null : _raw["streamMetaFieldName"].AsString;

        /// <summary>Whether auto-scaling is enabled.</summary>
        public bool AutoScalingEnabled => _raw.GetValue("enableAutoScaling", false).ToBoolean();

        /// <summary>Whether failover is enabled.</summary>
        public bool FailoverEnabled => _raw.GetValue("failoverEnabled", false).ToBoolean();

        /// <summary>Active region for the processor.</summary>
        public string ActiveRegion => _raw.GetValue("activeRegion", BsonNull.Value).IsBsonNull ? null : _raw["activeRegion"].AsString;

        /// <summary>Workspace default region.</summary>
        public string WorkspaceDefaultRegion => _raw.GetValue("workspaceDefaultRegion", BsonNull.Value).IsBsonNull ? null : _raw["workspaceDefaultRegion"].AsString;

        /// <summary>Timestamp of the last state change.</summary>
        public BsonValue LastStateChange => _raw.TryGetValue("lastStateChange", out var v) ? v : null;

        /// <summary>Timestamp of the last modification.</summary>
        public BsonValue LastModifiedAt => _raw.TryGetValue("lastModifiedAt", out var v) ? v : null;

        /// <summary>User who last modified the processor.</summary>
        public string ModifiedBy => _raw.GetValue("modifiedBy", BsonNull.Value).IsBsonNull ? null : _raw["modifiedBy"].AsString;

        /// <summary>Whether the processor has been started at least once.</summary>
        public bool HasStarted => _raw.GetValue("hasStarted", false).ToBoolean();

        /// <summary>Error message. Always present; empty string indicates no error has occurred.</summary>
        public string ErrorMessage => _raw.GetValue("errorMsg", "").AsString;

        /// <summary>Whether the most recent error is retryable.</summary>
        public bool ErrorRetryable => _raw.GetValue("errorRetryable", false).ToBoolean();

        /// <summary>Error code from the most recent error.</summary>
        public int? ErrorCode => _raw.TryGetValue("errorCode", out var v) && v.IsInt32 ? v.AsInt32 : (int?)null;
    }
}
