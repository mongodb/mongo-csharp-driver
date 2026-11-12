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

namespace MongoDB.Driver.StreamProcessing
{
    /// <summary>
    /// Options for <see cref="StreamProcessor.Samples(GetStreamProcessorSamplesOptions, System.Threading.CancellationToken)"/>.
    /// </summary>
    public sealed class GetStreamProcessorSamplesOptions
    {
        /// <summary>
        /// The cursor id from a previous call. If null or zero, a new sample
        /// cursor is opened via <c>startSampleStreamProcessor</c>. If non-zero,
        /// the next batch is fetched via <c>getMoreSampleStreamProcessor</c>.
        /// </summary>
        public long? CursorId { get; set; }

        /// <summary>
        /// Maximum number of documents to sample. Only sent on the initial
        /// call (when CursorId is null or zero). Ignored on subsequent calls.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Number of documents to return per batch. Only sent on subsequent
        /// calls (when CursorId is non-zero). Ignored on the initial call.
        /// </summary>
        public int? BatchSize { get; set; }
    }
}
