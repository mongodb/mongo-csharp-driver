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
using MongoDB.Bson.ObjectModel;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Contains extensions methods for <see cref="BsonBinaryData"/>.
    /// </summary>
    public static class BsonBinaryDataExtensions
    {
        /// <summary>
        /// Converts <see cref="BsonBinaryData"/> to <see cref="BsonVector{T}"/>.
        /// </summary>
        /// <typeparam name="T">Data type of the Bson vector.</typeparam>
        /// <param name="binaryData">The binary data.</param>
        /// <returns>A <see cref="BsonVector{T}"/> instance.</returns>
        public static BsonVector<T> ToBsonVector<T>(this BsonBinaryData binaryData)
            where T : struct
        {
            EnsureBsonVectorDataType(binaryData);

            return BsonVectorReader.ReadBsonVector<T>(binaryData.Bytes);
        }

        /// <summary>
        /// Extracts Bson vector data from <see cref="BsonBinaryData"/> as bytes, padding and data type.
        /// The result bytes should be interpreted according to the vector data type.
        /// </summary>
        /// <param name="binaryData">The binary data.</param>
        /// <returns>Vector bytes, padding and datatype</returns>
        public static (ReadOnlyMemory<byte> VectorDataBytes, byte Padding, BsonVectorDataType VectorDataType) ToBsonVectorAsBytes(this BsonBinaryData binaryData)
        {
            EnsureBsonVectorDataType(binaryData);

            return BsonVectorReader.ReadBsonVectorAsBytes(binaryData.Bytes);
        }

        internal static (T[] Elements, byte Padding, BsonVectorDataType vectorDataType) ToBsonVectorAsArray<T>(this BsonBinaryData binaryData)
            where T : struct
        {
            EnsureBsonVectorDataType(binaryData);

            return BsonVectorReader.ReadBsonVectorAsArray<T>(binaryData.Bytes);
        }

        private static void EnsureBsonVectorDataType(BsonBinaryData binaryData)
        {
            if (binaryData.SubType != BsonBinarySubType.Vector)
            {
                throw new InvalidOperationException($"Binary Vector subtype is expected, found {binaryData.SubType} instead.");
            }
        }
    }
}
