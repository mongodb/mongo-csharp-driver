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

using MongoDB.Bson;

namespace MongoDB.Driver.StreamProcessing
{
    /// <summary>
    /// Options for <see cref="StreamProcessors.Create(string, System.Collections.Generic.IEnumerable{BsonDocument}, CreateStreamProcessorOptions, System.Threading.CancellationToken)"/>.
    /// </summary>
    public sealed class CreateStreamProcessorOptions
    {
        /// <summary>Dead letter queue configuration.</summary>
        public BsonDocument Dlq { get; set; }

        /// <summary>Field name used for stream metadata.</summary>
        public string StreamMetaFieldName { get; set; }

        /// <summary>Compute tier (e.g. "SP2", "SP5", "SP10", "SP30", "SP50").</summary>
        public string Tier { get; set; }

        /// <summary>Whether failover is enabled.</summary>
        public bool? Failover { get; set; }
    }
}
