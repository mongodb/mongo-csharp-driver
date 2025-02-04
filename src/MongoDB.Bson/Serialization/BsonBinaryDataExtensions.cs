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
        /// Converts <see cref="BsonBinaryData"/> to <see cref="BsonVectorBase{TItem}"/>.
        /// </summary>
        /// <typeparam name="TItem">Data type of the Bson vector.</typeparam>
        /// <param name="binaryData">The binary data.</param>
        /// <returns>A <see cref="BsonVectorBase{TItem}"/> instance.</returns>
        public static BsonVectorBase<TItem> ToBsonVector<TItem>(this BsonBinaryData binaryData)
            where TItem : struct
        {
            EnsureBsonVectorSubType(binaryData);

            return BsonVectorReader.ReadBsonVector<TItem>(binaryData.Bytes);
        }

        /// <summary>
        /// Extracts Bson vector data from <see cref="BsonBinaryData"/> as bytes, padding and data type.
        /// The result bytes should be interpreted according to the vector data type.
        /// </summary>
        /// <param name="binaryData">The binary data.</param>
        /// <returns>Vector bytes, padding and datatype</returns>
        public static (ReadOnlyMemory<byte> Bytes, byte Padding, BsonVectorDataType VectorDataType) ToBsonVectorAsBytes(this BsonBinaryData binaryData)
        {
            EnsureBsonVectorSubType(binaryData);

            return BsonVectorReader.ReadBsonVectorAsBytes(binaryData.Bytes);
        }

        /// <summary>
        /// Extracts Bson vector data from <see cref="BsonBinaryData"/> as an array of <typeparamref name="TItem"/>, padding and data type.
        /// The result bytes should be interpreted according to the vector data type.
        /// </summary>
        /// <param name="binaryData">The binary data.</param>
        /// <returns>Vector data, padding and datatype</returns>
        public static (TItem[] Items, byte Padding, BsonVectorDataType VectorDataType) ToBsonVectorAsArray<TItem>(this BsonBinaryData binaryData)
            where TItem : struct
        {
            EnsureBsonVectorSubType(binaryData);

            return BsonVectorReader.ReadBsonVectorAsArray<TItem>(binaryData.Bytes);
        }

        private static void EnsureBsonVectorSubType(BsonBinaryData binaryData)
        {
            if (binaryData.SubType != BsonBinarySubType.Vector)
            {
                throw new InvalidOperationException($"Expected BsonBinary Vector subtype, but found {binaryData.SubType} instead.");
            }
        }
    }
}
