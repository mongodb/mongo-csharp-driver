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
    /// The result of a call to <see cref="StreamProcessor.Samples"/> or
    /// <see cref="StreamProcessor.SamplesAsync"/>.
    /// </summary>
    /// <remarks>
    /// Callers MUST stop iterating when <see cref="CursorId"/> is 0 — the
    /// cursor is exhausted and no further calls should be made.
    /// </remarks>
    public sealed class StreamProcessorSamples
    {
        internal StreamProcessorSamples(long cursorId, IReadOnlyList<BsonDocument> documents)
        {
            CursorId = cursorId;
            Documents = documents ?? new List<BsonDocument>();
        }

        /// <summary>The cursor id to pass to the next call. 0 means the cursor is exhausted.</summary>
        public long CursorId { get; }

        /// <summary>The batch of sampled documents.</summary>
        public IReadOnlyList<BsonDocument> Documents { get; }

        /// <summary>Whether the cursor is exhausted (cursor id is 0).</summary>
        public bool IsExhausted => CursorId == 0;
    }
}
