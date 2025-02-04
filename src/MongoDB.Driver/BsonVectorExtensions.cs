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
    /// Contains extensions methods for <see cref="BsonVectorBase{TItem}"/>
    /// </summary>
    public static class BsonVectorDriverExtensions
    {
        /// <summary>
        /// Converts <see cref="BsonVectorBase{TItem}"/> to <see cref="BsonBinaryData"/>.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="bsonVector">The BSON vector.</param>
        /// <returns>A <see cref="BsonBinaryData"/> instance.</returns>
        public static QueryVector ToQueryVector<TItem>(this BsonVectorBase<TItem> bsonVector)
            where TItem  : struct =>
            bsonVector switch
            {
                BsonVectorFloat32 bsonVectorFloat32 => new(bsonVectorFloat32.ToBsonBinaryData()),
                BsonVectorInt8 bsonVectorInt8 => new(bsonVectorInt8.ToBsonBinaryData()),
                BsonVectorPackedBit bsonVectorPackedBit => new(bsonVectorPackedBit.ToBsonBinaryData()),
                _ => throw new InvalidOperationException($"Invalidate Bson Vector type {bsonVector?.GetType()}")
            };
    }
}
