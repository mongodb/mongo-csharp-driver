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

using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents update operation result in the scope of BulkWrite.
    /// </summary>
    public class BulkWriteUpdateResult
    {
        /// <summary>
        /// The number of documents that matched the filter.
        /// </summary>
        public long MatchedCount { get; init; }

        /// <summary>
        /// The number of documents that were modified.
        /// </summary>
        public long ModifiedCount { get; init; }

        /// <summary>
        /// The _id field of the upserted document if an upsert occurred.
        /// </summary>
        public BsonValue UpsertedId { get; init; }
    }
}
