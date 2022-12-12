// Copyright 2010-present MongoDB Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A result set for a search metadata query.
    /// </summary>
    public sealed class SearchMetaResult
    {
        /// <summary>
        /// Gets or sets the count result set.
        /// </summary>
        [BsonElement("count")]
        public SearchMetaCountResult Count { get; private set; }

        /// <summary>
        /// Gets or sets the facet result sets.
        /// </summary>
        [BsonElement("facet")]
        public Dictionary<string, SearchMetaFacetResult> Facet { get; private set; }
    }

    /// <summary>
    /// A search count result set.
    /// </summary>
    public sealed class SearchMetaCountResult
    {
        /// <summary>
        /// Gets or sets the lower bound for this result set.
        /// </summary>
        [BsonElement("lowerBound")]
        public long? LowerBound { get; private set; }

        /// <summary>
        /// Gets or sets the total for this result set.
        /// </summary>
        [BsonElement("total")]
        public long? Total { get; private set; }
    }

    /// <summary>
    /// A search facet result set.
    /// </summary>
    public sealed class SearchMetaFacetResult
    {
        /// <summary>
        /// Gets or sets a list of bucket result sets.
        /// </summary>
        [BsonElement("buckets")]
        public List<SearchMetaFacetBucketResult> Buckets { get; private set; }
    }

    /// <summary>
    /// A search facet bucket result set.
    /// </summary>
    public sealed class SearchMetaFacetBucketResult
    {
        /// <summary>
        /// Gets or sets the unique identifier that identifies this facet bucket.
        /// </summary>
        [BsonId]
        public BsonValue Id { get; private set; }

        /// <summary>
        /// Gets or sets the count of documents in this facet bucket.
        /// </summary>
        [BsonElement("count")]
        public long Count { get; private set; }
    }
}
