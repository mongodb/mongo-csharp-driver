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

using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Options {
    /// <summary>
    /// Represents serialization options for a Guid value.
    /// </summary>
    public class GuidSerializationOptions : IBsonSerializationOptions {
        #region private static fields
        private static GuidSerializationOptions defaults = new GuidSerializationOptions(GuidByteOrder.LittleEndian);
        private static GuidSerializationOptions littleEndian = new GuidSerializationOptions(GuidByteOrder.LittleEndian);
        private static GuidSerializationOptions bigEndian = new GuidSerializationOptions(GuidByteOrder.BigEndian);
        private static GuidSerializationOptions stringInstance = new GuidSerializationOptions(BsonType.String);
        #endregion

        #region private fields
        private BsonType representation = BsonType.Binary;
        private GuidByteOrder byteOrder = GuidByteOrder.LittleEndian;
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the GuidSerializationOptions class.
        /// </summary>
        public GuidSerializationOptions() {
        }

        /// <summary>
        /// Initializes a new instance of the GuidSerializationOptions class.
        /// </summary>
        /// <param name="representation">The external representation.</param>
        public GuidSerializationOptions(
            BsonType representation
        ) {
            this.representation = representation;
        }

        /// <summary>
        /// Initializes a new instance of the GuidSerializationOptions class.
        /// </summary>
        /// <param name="byteOrder">The byte order to use when representing the Guid as a byte array.</param>
        public GuidSerializationOptions(
            GuidByteOrder byteOrder
        ) {
            this.representation = BsonType.Binary;
            this.byteOrder = byteOrder;
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets or sets the default Guid serialization options.
        /// </summary>
        public static GuidSerializationOptions Defaults {
            get { return defaults; }
            set { defaults = value; }
        }

        /// <summary>
        /// Gets an instance of GuidSerializationOptions with Binary representation and Microsoft's internal little endian byte order.
        /// </summary>
        public static GuidSerializationOptions LittleEndian {
            get { return littleEndian; }
        }

        /// <summary>
        /// Gets an instance of GuidSerializationOptions with Binary representation and big endian byte order.
        /// </summary>
        public static GuidSerializationOptions BigEndian {
            get { return bigEndian; }
        }

        /// <summary>
        /// Gets an instance of GuidSerializationOptions with String representation.
        /// </summary>
        public static GuidSerializationOptions String {
            get { return stringInstance; }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Gets the byte order to use when representing the Guid as a byte array.
        /// </summary>
        public GuidByteOrder ByteOrder {
            get { return byteOrder; }
        }

        /// <summary>
        /// Gets the external representation.
        /// </summary>
        public BsonType Representation {
            get { return representation; }
        }
        #endregion
    }
}
