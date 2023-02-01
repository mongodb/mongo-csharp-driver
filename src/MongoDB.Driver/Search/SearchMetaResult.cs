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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Driver.Search
{
    /// <summary>
    /// A search count result set.
    /// </summary>
    public sealed class SearchMetaCountResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchMetaCountResult"/> class.
        /// </summary>
        /// <param name="lowerBound">Lower bound for this result set.</param>
        /// <param name="total">Total for this result set.</param>
        public SearchMetaCountResult(long? lowerBound, long? total)
        {
            LowerBound = lowerBound;
            Total = total;
        }

        /// <summary>
        /// Gets the lower bound for this result set.
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("lowerBound")]
        public long? LowerBound { get; }

        /// <summary>
        /// Gets the total for this result set.
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("total")]
        public long? Total { get; }
    }

    /// <summary>
    /// A search facet bucket result set.
    /// </summary>
    public sealed class SearchMetaFacetBucketResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchMetaFacetBucketResult"/> class.
        /// </summary>
        /// <param name="count">count of documents in this facet bucket.</param>
        /// <param name="id">Unique identifier that identifies this facet bucket.</param>
        public SearchMetaFacetBucketResult(long count, BsonValue id)
        {
            Count = count;
            Id = id;
        }

        /// <summary>
        /// Gets the count of documents in this facet bucket.
        /// </summary>
        [BsonElement("count")]
        public long Count { get; }

        /// <summary>
        /// Gets the unique identifier that identifies this facet bucket.
        /// </summary>
        [BsonId]
        public BsonValue Id { get; }
    }

    /// <summary>
    /// A search facet result set.
    /// </summary>
    public sealed class SearchMetaFacetResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchMetaFacetResult"/> class.
        /// </summary>
        /// <param name="buckets">An array of bucket result sets.</param>
        public SearchMetaFacetResult(SearchMetaFacetBucketResult[] buckets)
        {
            Buckets = buckets;
        }

        /// <summary>
        /// Gets an array of bucket result sets.
        /// </summary>
        [BsonElement("buckets")]
        public SearchMetaFacetBucketResult[] Buckets { get; }
    }

    /// <summary>
    /// A result set for a search metadata query.
    /// </summary>
    public sealed class SearchMetaResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchMetaResult"/> class.
        /// </summary>
        /// <param name="count">Count result set.</param>
        /// <param name="facet">Facet result sets.</param>
        public SearchMetaResult(SearchMetaCountResult count, IReadOnlyDictionary<string, SearchMetaFacetResult> facet)
        {
            Count = count;
            Facet = facet;
        }

        /// <summary>
        /// Gets the count result set.
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("count")]
        public SearchMetaCountResult Count { get; }

        /// <summary>
        /// Gets the facet result sets.
        /// </summary>
        [BsonDefaultValue(null)]
        [BsonElement("facet")]
        public IReadOnlyDictionary<string, SearchMetaFacetResult> Facet { get; }
    }
}
