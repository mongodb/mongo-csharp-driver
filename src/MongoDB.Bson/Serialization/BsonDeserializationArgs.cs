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

using System;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Represents args common to all serializers.
    /// </summary>
    public struct BsonDeserializationArgs
    {
        // private fields
        private Type _nominalType;
        private object _targetInstance;

        // public properties
        /// <summary>
        /// Gets or sets the nominal type.
        /// </summary>
        /// <value>
        /// The nominal type.
        /// </value>
        public Type NominalType
        {
            get { return _nominalType; }
            set { _nominalType = value; }
        }

        /// <summary>
        /// Gets or sets the target instance.
        /// </summary>
        /// <value>
        /// The target instance or null.
        /// </value>
        public object TargetInstance
        {
            get { return _targetInstance; }
            set { _targetInstance = value; }
        }
    }
}