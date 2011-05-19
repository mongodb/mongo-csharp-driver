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

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Attributes {
    /// <summary>
    /// Represents serialization options for a Guid field or property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonGuidOptionsAttribute : BsonSerializationOptionsAttribute {
        #region private fields
        private BsonType representation = BsonType.Binary;
        private GuidByteOrder byteOrder = GuidByteOrder.LittleEndian;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonGuidOptionsAttribute class.
        /// </summary>
        public BsonGuidOptionsAttribute() {
        }

        /// <summary>
        /// Initializes a new instance of the BsonGuidOptionsAttribute class.
        /// </summary>
        /// <param name="representation">The external representation to use for Guids.</param>
        public BsonGuidOptionsAttribute(
            BsonType representation
        ) {
            this.representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of the BsonGuidOptionsAttribute class.
        /// </summary>
        /// <param name="byteOrder">The byte order to use when representing the Guid as a byte array.</param>
        public BsonGuidOptionsAttribute(
            GuidByteOrder byteOrder
        ) {
            this.representation = BsonType.Binary;
            this.byteOrder = byteOrder;
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the byte order to use when representing the Guid as a byte array.
        /// </summary>
        public GuidByteOrder ByteOrder {
            get { return byteOrder; }
            set { byteOrder = value; }
        }

        /// <summary>
        /// Gets or sets the external representation.
        /// </summary>
        public BsonType Representation {
            get { return representation; }
            set { representation = value; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Gets the serialization options specified by this attribute.
        /// </summary>
        /// <returns>The serialization options.</returns>
        public override IBsonSerializationOptions GetOptions() {
            if (representation == BsonType.Binary) {
                return new GuidSerializationOptions(byteOrder);
            } else {
                return new GuidSerializationOptions(representation);
            }
        }
        #endregion
    }
}
