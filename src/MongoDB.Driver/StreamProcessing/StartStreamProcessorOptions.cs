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
    /// Options for <see cref="StreamProcessor.Start(StartStreamProcessorOptions, System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// The spec's <c>startAfter</c> option is RESERVED for future use and is
    /// not yet accepted by the server; the driver MUST NOT send it.
    /// </remarks>
    public sealed class StartStreamProcessorOptions
    {
        /// <summary>Number of workers.</summary>
        public int? Workers { get; set; }

        /// <summary>Clear checkpoints before starting.</summary>
        public bool? ClearCheckpoints { get; set; }

        /// <summary>Resume from a specific operation time.</summary>
        public BsonTimestamp StartAtOperationTime { get; set; }

        /// <summary>Compute tier. Valid values: "SP2", "SP5", "SP10", "SP30", "SP50".</summary>
        public string Tier { get; set; }

        /// <summary>Enable auto-scaling.</summary>
        public bool? EnableAutoScaling { get; set; }

        /// <summary>Failover configuration. The Region property is required when failover is sent.</summary>
        public FailoverOptions Failover { get; set; }
    }
}
