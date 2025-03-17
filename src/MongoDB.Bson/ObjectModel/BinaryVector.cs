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

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents a binary vector.
    /// </summary>
    public abstract class BinaryVector<TItem>
         where TItem : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVector{TItem}"/> class.
        /// </summary>
        /// <param name="data">The vector data.</param>
        /// <param name="dataType">Type of the vector data.</param>
        private protected BinaryVector(ReadOnlyMemory<TItem> data, BinaryVectorDataType dataType)
        {
            DataType = dataType;
            Data = data;
        }

        /// <summary>
        /// Gets the vector data type.
        /// </summary>
        public BinaryVectorDataType DataType { get; }

        /// <summary>
        /// Gets the vector data.
        /// </summary>
        public ReadOnlyMemory<TItem> Data { get; }
    }

    /// <summary>
    /// Represents a vector of <see cref="float"/> values.
    /// </summary>
    public sealed class BinaryVectorFloat32 : BinaryVector<float>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVectorFloat32"/> class.
        /// </summary>
        public BinaryVectorFloat32(ReadOnlyMemory<float> data) : base(data, BinaryVectorDataType.Float32)
        {
        }
    }

    /// <summary>
    /// Represents a vector of <see cref="byte"/> values.
    /// </summary>
    [CLSCompliant(false)]
    public sealed class BinaryVectorInt8 : BinaryVector<sbyte>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVectorInt8"/> class.
        /// </summary>
        public BinaryVectorInt8(ReadOnlyMemory<sbyte> data) : base(data, BinaryVectorDataType.Int8)
        {
        }
    }

    /// <summary>
    /// Represents a vector of 0/1 values.
    /// The vector values are packed into groups of 8 (a byte).
    /// </summary>
    public sealed class BinaryVectorPackedBit : BinaryVector<byte>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVectorPackedBit"/> class.
        /// </summary>
        public BinaryVectorPackedBit(ReadOnlyMemory<byte> data, byte padding) : base(data, BinaryVectorDataType.PackedBit)
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
