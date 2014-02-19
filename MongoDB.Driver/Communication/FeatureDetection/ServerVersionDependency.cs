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

using System;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Communication.FeatureDetection
{
    internal class ServerVersionDependency : IFeatureDependency
    {
        // private fields
        private readonly Version _min;
        private readonly Version _max;

        // constructors
        public ServerVersionDependency(int major, int minor, int build)
            : this(new Version(major, minor, build))
        {
        }

        public ServerVersionDependency(Version min)
            : this(min, null)
        {
        }

        public ServerVersionDependency(Version min, Version max)
        {
            _min = min;
            _max = max;
        }

        // public methods
        public bool IsMet(FeatureContext context)
        {
            if (_min != null && context.BuildInfo.Version < _min)
            {
                return false;
            }

            if (_max != null && context.BuildInfo.Version > _max)
            {
                return false;
            }

            return true;
        }
    }
}
