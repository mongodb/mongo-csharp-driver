/* Copyright 2010-2011 10gen Inc.
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Bson.Serialization.Attributes {
    /// <summary>
    /// Specifies the default value for a field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonDefaultValueAttribute : Attribute {
        #region private fields
        private object defaultValue;
        private bool serializeDefaultValue = true;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonDefaultValueAttribute class.
        /// </summary>
        /// <param name="defaultValue">The default value.</param>
        public BsonDefaultValueAttribute(
            object defaultValue
        ) {
            this.defaultValue = defaultValue;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public object DefaultValue {
            get { return defaultValue; }
        }

        /// <summary>
        /// Gets or sets whether to serialize the default value.
        /// </summary>
        public bool SerializeDefaultValue {
            get { return serializeDefaultValue; }
            set { serializeDefaultValue = value; }
        }
        #endregion
    }
}
