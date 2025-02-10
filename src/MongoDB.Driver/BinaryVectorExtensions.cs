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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver
{
    /// <summary>
    /// Contains extensions methods for <see cref="BinaryVector{TItem}"/>
    /// </summary>
    public static class BinaryVectorDriverExtensions
    {
        /// <summary>
        /// Converts <see cref="BinaryVector{TItem}"/> to <see cref="BsonBinaryData"/>.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="binaryVector">The binary vector.</param>
        /// <returns>A <see cref="BsonBinaryData"/> instance.</returns>
        public static QueryVector ToQueryVector<TItem>(this BinaryVector<TItem> binaryVector)
            where TItem  : struct =>
            binaryVector switch
            {
                BinaryVectorFloat32 binaryVectorFloat32 => new(binaryVectorFloat32.ToBsonBinaryData()),
                BinaryVectorInt8 binaryVectorInt8 => new(binaryVectorInt8.ToBsonBinaryData()),
                BinaryVectorPackedBit binaryVectorPackedBit => new(binaryVectorPackedBit.ToBsonBinaryData()),
                _ => throw new InvalidOperationException($"Invalid binary vector type {binaryVector?.GetType()}")
            };
    }
}
