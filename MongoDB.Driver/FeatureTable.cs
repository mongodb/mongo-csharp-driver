/* Copyright 2010-2013 10gen Inc.
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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a table of features that are not supported in all versions of the server.
    /// </summary>
    public class FeatureTable
    {
        // private fields
        private readonly Dictionary<FeatureId, Feature> _features = new Dictionary<FeatureId, Feature>();

        // public methods
        /// <summary>
        /// Gets a feature.
        /// </summary>
        /// <param name="featureId">The feature Id.</param>
        /// <returns>The feature.</returns>
        public Feature GetFeature(FeatureId featureId)
        {
            return _features[featureId];
        }

        /// <summary>
        /// Tries to get a feature.
        /// </summary>
        /// <param name="featureId">The feature Id.</param>
        /// <param name="feature">The feature.</param>
        /// <returns>True if the feature was in the feature table; otherwise, false.</returns>
        public bool TryGetFeature(FeatureId featureId, out Feature feature)
        {
            return _features.TryGetValue(featureId, out feature);
        }

        // internal methods
        internal void AddFeature(Feature feature)
        {
            _features.Add(feature.Id, feature);
        }
    }
}
