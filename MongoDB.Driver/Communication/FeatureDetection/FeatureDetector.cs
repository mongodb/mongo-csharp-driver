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

namespace MongoDB.Driver.Communication.FeatureDetection
{
    internal class FeatureDetector : IFeatureDetector
    {
        // private fields
        private readonly IEnumerable<IFeatureDependency> _dependencies;
        private readonly Feature _isNotSupportedInstance;
        private readonly Feature _isSupportedInstance;

        // constructors
        public FeatureDetector(FeatureId featureId, params IFeatureDependency[] dependencies)
        {
            _dependencies = dependencies;
            _isNotSupportedInstance = new Feature(featureId, false);
            _isSupportedInstance = new Feature(featureId, true);
        }

        // public methods
        public Feature DetectFeature(FeatureContext context)
        {
            foreach (var dependency in _dependencies)
            {
                if (!dependency.IsMet(context))
                {
                    return _isNotSupportedInstance;
                }
            }

            return _isSupportedInstance;
        }
    }
}
