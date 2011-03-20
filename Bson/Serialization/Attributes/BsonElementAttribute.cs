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
    /// Specifies the element name and related options for a field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonElementAttribute : Attribute {
        #region private fields
        private string elementName;
        private int order = int.MaxValue;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonElementAttribute class.
        /// </summary>
        /// <param name="elementName">The name of the element.</param>
        public BsonElementAttribute(
            string elementName
        ) {
            this.elementName = elementName;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the element name.
        /// </summary>
        public string ElementName {
            get { return elementName; }
        }

        /// <summary>
        /// Gets the element serialization order.
        /// </summary>
        public int Order {
            get { return order; }
            set { order = value; }
        }
        #endregion
    }
}
