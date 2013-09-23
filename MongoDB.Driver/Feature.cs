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

using System;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a feature not supported in all versions of the server.
    /// </summary>
    public class Feature
    {
        // private fields
        private readonly FeatureId _id;
        private readonly bool _isSupported;

        // constructors
        internal Feature(FeatureId id, bool isSupported)
        {
            _id = id;
            _isSupported = isSupported;
        }

        // public properties
        /// <summary>
        /// Gets the feature id.
        /// </summary>
        /// <value>
        /// The feature id.
        /// </value>
        public FeatureId Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets a value indicating whether this feature is supported by the current server instance.
        /// </summary>
        /// <value>
        /// <c>true</c> if this feature is supported; otherwise, <c>false</c>.
        /// </value>
        public bool IsSupported
        {
            get { return _isSupported; }
        }
    }
}
