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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson {
    /// <summary>
    /// A static class containing methods to convert to and from Guids and byte arrays in various byte orders.
    /// </summary>
    public static class GuidConverter {
        /// <summary>
        /// Converts a byte array to a Guid.
        /// </summary>
        /// <param name="bytes">The byte array.</param>
        /// <param name="byteOrder">The byte order of the Guid in the byte array.</param>
        /// <returns>A Guid.</returns>
        public static Guid FromBytes(
            byte[] bytes,
            GuidByteOrder byteOrder
        ) {
            bytes = (byte[]) bytes.Clone();
            switch (byteOrder) {
                case GuidByteOrder.BigEndian:
                    if (BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                    break;
                case GuidByteOrder.LittleEndian:
                    if (!BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                    break;
                case GuidByteOrder.JavaHistorical:
                    Array.Reverse(bytes, 0, 8);
                    Array.Reverse(bytes, 8, 8);
                    if (BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                    break;
                case GuidByteOrder.Unspecified:
                    throw new InvalidOperationException("Unable to convert byte array to Guid because GuidByteOrder is Unspecified.");
                default:
                    throw new BsonInternalException("Unexpected GuidByteOrder.");
            }
            return new Guid(bytes);
        }

        /// <summary>
        /// Converts a Guid to a byte array.
        /// </summary>
        /// <param name="guid">The Guid.</param>
        /// <param name="byteOrder">The byte order of the Guid in the byte array.</param>
        /// <returns>A byte array.</returns>
        public static byte[] ToBytes(
            Guid guid,
            GuidByteOrder byteOrder
        ) {
            var bytes = (byte[]) guid.ToByteArray().Clone();
            switch (byteOrder) {
                case GuidByteOrder.BigEndian:
                     if (BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                   break;
                case GuidByteOrder.LittleEndian:
                    if (!BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                    break;
                case GuidByteOrder.JavaHistorical:
                    if (BitConverter.IsLittleEndian) {
                        Array.Reverse(bytes, 0, 4);
                        Array.Reverse(bytes, 4, 2);
                        Array.Reverse(bytes, 6, 2);
                    }
                    Array.Reverse(bytes, 0, 8);
                    Array.Reverse(bytes, 8, 8);
                    break;
                case GuidByteOrder.Unspecified:
                    throw new InvalidOperationException("Unable to convert Guid to byte array because GuidByteOrder is Unspecified.");
                default:
                    throw new BsonInternalException("Unexpected GuidByteOrder.");
            }
            return bytes;
        }
    }
}
