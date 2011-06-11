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

namespace MongoDB.Bson {
    /// <summary>
    /// Represents the byte order to use when representing a Guid as a byte array.
    /// </summary>
    public enum GuidByteOrder {
        /// <summary>
        /// The byte order for Guids is unspecified, so conversion between byte arrays and Guids is not possible.
        /// </summary>
        Unspecified,
        /// <summary>
        /// Use Microsoft's internal little endian format (this is the default for historical reasons, but is different from how the Java and other drivers store UUIDs).
        /// </summary>
        LittleEndian,
        /// <summary>
        /// Use the standard external high endian format (this is compatible with how the Java and other drivers store UUIDs).
        /// </summary>
        BigEndian,
        /// <summary>
        /// Use the byte order used by older versions of the Java driver (two 8 byte halves in little endian order).
        /// </summary>
        JavaHistorical
    }
}
