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
    /// Options for replacing a single document.
    /// </summary>
    public sealed class ReplaceOptions<T>
    {
        // properties
        /// <summary>
        /// Gets or sets a value indicating whether to bypass document validation.
        /// </summary>
        public bool? BypassDocumentValidation { get; set; }

        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation { get; set; }

        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public BsonValue Comment { get; set; }

        /// <summary>
        /// Gets or sets the hint.
        /// </summary>
        public BsonValue Hint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to insert the document if it doesn't already exist.
        /// </summary>
        public bool IsUpsert { get; set; }

        /// <summary>
        /// Gets or sets the let document.
        /// </summary>
        public BsonDocument Let { get; set; }

        /// <summary>
        /// Gets or sets the sort definition.
        /// </summary>
        public SortDefinition<T> Sort { get; set; }

        /// <summary>
        /// Gets or sets the operation timeout.
        /// </summary>
        // TODO: CSOT: Make it public when CSOT will be ready for GA
        internal TimeSpan? Timeout
        {
            get;
            set => field = Ensure.IsNullOrValidTimeout(value, nameof(Timeout));
        }
    }
}
