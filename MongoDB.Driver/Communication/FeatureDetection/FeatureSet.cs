/* Copyright 2010-2014 MongoDB Inc.
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

namespace MongoDB.Driver.Communication.FeatureDetection
{
    /// <summary>
    /// Represents a set of features that are supported by a server instance.
    /// </summary>
    internal class FeatureSet
    {
        // private fields
        private readonly HashSet<FeatureId> _features = new HashSet<FeatureId>();

        // public methods
        /// <summary>
        /// Checks whether a feature is supported.
        /// </summary>
        /// <param name="featureId">The feature Id.</param>
        /// <returns>True if the feature is supported; otherwise, false.</returns>
        public bool IsSupported(FeatureId featureId)
        {
            return _features.Contains(featureId);
        }

        // internal methods
        internal void AddFeature(FeatureId featureId)
        {
            _features.Add(featureId);
        }
    }
}
