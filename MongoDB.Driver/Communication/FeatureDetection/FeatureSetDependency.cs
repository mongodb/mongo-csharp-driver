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
    internal class FeatureSetDependency
    {
        // private fields
        private readonly IFeatureDependency _dependency;
        private readonly IEnumerable<FeatureId> _featureIds;

        // constructors
        public FeatureSetDependency(IFeatureDependency dependency, params FeatureId[] featureIds)
        {
            _dependency = dependency;
            _featureIds = featureIds;
        }

        // public properties
        public IFeatureDependency Dependency
        {
            get { return _dependency; }
        }

        public IEnumerable<FeatureId> FeatureIds
        {
            get { return _featureIds; }
        }
    }
}
