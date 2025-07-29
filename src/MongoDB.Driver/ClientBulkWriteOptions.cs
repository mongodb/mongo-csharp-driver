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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Options for a bulk write operation.
    /// </summary>
    public sealed class ClientBulkWriteOptions
    {
        private TimeSpan? _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteOptions"/> class.
        /// </summary>
        public ClientBulkWriteOptions()
        {
            IsOrdered = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteOptions"/> class.
        /// </summary>
        /// <param name="isOrdered"></param>
        /// <param name="bypassDocumentValidation"></param>
        /// <param name="verboseResult"></param>
        /// <param name="writeConcern"></param>
        /// <param name="let"></param>
        /// <param name="comment"></param>
        public ClientBulkWriteOptions(
            bool isOrdered,
            bool? bypassDocumentValidation,
            bool verboseResult,
            WriteConcern writeConcern,
            BsonDocument let,
            BsonValue comment)
        {
            IsOrdered = isOrdered;
            BypassDocumentValidation = bypassDocumentValidation;
            VerboseResult = verboseResult;
            WriteConcern = writeConcern;
            Let = let;
            Comment = comment;
        }

        /// <summary>
        /// Bypass document validation.
        /// </summary>
        public bool? BypassDocumentValidation { get; set; }

        /// <summary>
        /// Comment.
        /// </summary>
        public BsonValue Comment { get; set; }

        /// <summary>
        /// A value indicating is bulk requests are fulfilled in order.
        /// </summary>
        public bool IsOrdered { get; set; }

        /// <summary>
        /// Let document.
        /// </summary>
        public BsonDocument Let { get; set; }

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        // TODO: SCOT: Make it public when CSOT will be ready for GA
        internal TimeSpan? Timeout
        {
            get => _timeout;
            set => _timeout = Ensure.IsNullOrValidTimeout(value, nameof(Timeout));
        }

        /// <summary>
        /// Whether detailed results for each successful operation should be included in the returned results.
        /// </summary>
        public bool VerboseResult { get; set; }

        /// <summary>
        /// The write concern to use for this bulk write.
        /// </summary>
        public WriteConcern WriteConcern { get; set; }
    }
}
