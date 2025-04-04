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

using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents options for the $geoNear stage.
    /// </summary>
    public class GeoNearOptions<TInputDocument, TOutputDocument>
    {
        /// <summary>
        /// Gets or sets the output field that contains the calculated distance. Required if querying a time-series collection.
        /// Optional for non-time series collections in MongoDB 8.1+
        /// </summary>
        public FieldDefinition<TOutputDocument> DistanceField { get; set; }

        /// <summary>
        /// Gets or sets the factor to multiply all distances returned by the query.
        /// </summary>
        public double? DistanceMultiplier { get; set; }

        /// <summary>
        /// Gets or sets the output field that identifies the location used to calculate the distance.
        /// </summary>
        public FieldDefinition<TOutputDocument> IncludeLocs { get; set; }

        /// <summary>
        /// Gets or sets the geospatial indexed field used when calculating the distance.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the max distance from the center point that the documents can be.
        /// </summary>
        public double? MaxDistance { get; set; }

        /// <summary>
        /// Gets or sets the min distance from the center point that the documents can be.
        /// </summary>
        public double? MinDistance { get; set; }

        /// <summary>
        /// Gets or sets the output serializer.
        /// </summary>
        public IBsonSerializer<TOutputDocument> OutputSerializer { get; set; }

        /// <summary>
        /// Gets or sets the query that limits the results to the documents that match the query.
        /// </summary>
        public FilterDefinition<TInputDocument> Query { get; set; }

        /// <summary>
        /// Gets or sets the spherical option which determines how to calculate the distance between two points.
        /// </summary>
        public bool? Spherical { get; set; }
    }
}