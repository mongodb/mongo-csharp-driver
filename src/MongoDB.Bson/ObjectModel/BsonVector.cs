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

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a BSON vector.
    /// </summary>
    public abstract class BsonVectorBase<TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorBase{TItem}"/> class.
        /// </summary>
        /// <param name="data">The vector data.</param>
        /// <param name="dataType">Type of the vector data.</param>
        private protected BsonVectorBase(ReadOnlyMemory<TItem> data, BsonVectorDataType dataType)
        {
            DataType = dataType;
            Data = data;
        }

        /// <summary>
        /// Gets the vector data type.
        /// </summary>
        public BsonVectorDataType DataType { get; }

        /// <summary>
        /// Gets the vector data.
        /// </summary>
        public ReadOnlyMemory<TItem> Data { get; }
    }

    /// <summary>
    /// Represents a vector of <see cref="float"/> values.
    /// </summary>
    public sealed class BsonVectorFloat32 : BsonVectorBase<float>
    {
        /// <summary>
        /// Initializes a new instance of the BsonVectorFloat32 class.
        /// </summary>
        public BsonVectorFloat32(ReadOnlyMemory<float> data) : base(data, BsonVectorDataType.Float32)
        {
        }
    }

    /// <summary>
    /// Represents a vector of <see cref="byte"/> values.
    /// </summary>
    public sealed class BsonVectorInt8 : BsonVectorBase<byte>
    {
        /// <summary>
        /// Initializes a new instance of the BsonVectorInt8 class.
        /// </summary>
        public BsonVectorInt8(ReadOnlyMemory<byte> data) : base(data, BsonVectorDataType.Int8)
        {
        }
    }

    /// <summary>
    /// Represents a vector of 0/1 values.
    /// The vector values are packed into groups of 8 (a byte).
    /// </summary>
    public sealed class BsonVectorPackedBit : BsonVectorBase<byte>
    {
        /// <summary>
        /// Initializes a new instance of the BsonVectorPackedBit class.
        /// </summary>
        public BsonVectorPackedBit(ReadOnlyMemory<byte> data, byte padding) : base(data, BsonVectorDataType.PackedBit)
        {
            if (padding > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(padding), padding, "Padding is expected to be in the range of [0..7].");
            }

            if (padding > 0 && data.Length == 0)
            {
                throw new ArgumentException("Can't specify non zero padding with no data.");
            }

            Padding = padding;
        }

        /// <summary>
        /// Gets the bits padding.
        /// </summary>
        public byte Padding { get; }
    }
}
