/* Copyright 2010-2016 MongoDB Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the arguments to the GeoNear method.
    /// </summary>
    public class GeoNearArgs
    {
        // private fields
        private Collation _collation;
        private double? _distanceMultiplier;
        private bool? _includeLocs;
        private int? _limit;
        private double? _maxDistance;
        private TimeSpan? _maxTime;
        private GeoNearPoint _near;
        private IMongoQuery _query;
        private bool? _spherical;
        private bool? _uniqueDocs;

        // public properties
        /// <summary>
        /// Gets or sets the collation.
        /// </summary>
        public Collation Collation
        {
            get { return _collation; }
            set { _collation = value; }
        }

        /// <summary>
        /// Gets or sets the distance multiplier.
        /// </summary>
        public double? DistanceMultiplier
        {
            get { return _distanceMultiplier; }
            set { _distanceMultiplier = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to include locations in the results.
        /// </summary>
        public bool? IncludeLocs
        {
            get { return _includeLocs; }
            set { _includeLocs = value; }
        }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        public int? Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        /// <summary>
        /// Gets or sets the max distance.
        /// </summary>
        public double? MaxDistance
        {
            get { return _maxDistance; }
            set { _maxDistance = value; }
        }

        /// <summary>
        /// Gets or sets the max time.
        /// </summary>
        public TimeSpan? MaxTime
        {
            get { return _maxTime; }
            set { _maxTime = value; }
        }

        /// <summary>
        /// Gets or sets the location near which to search.
        /// </summary>
        public GeoNearPoint Near
        {
            get { return _near; }
            set { _near = value; }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        public IMongoQuery Query
        {
            get { return _query; }
            set { _query = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the search is on a spherical surface.
        /// </summary>
        public bool? Spherical
        {
            get { return _spherical; }
            set { _spherical = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to only return a document once even if matches multiple times.
        /// </summary>
        public bool? UniqueDocs
        {
            get { return _uniqueDocs; }
            set { _uniqueDocs = value; }
        }
    }
}
