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

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// Contains extensions methods for <see cref="BinaryVector{TItem}"/>
    /// </summary>
    public static class BinaryVectorExtensions
    {
        /// <summary>
        /// Converts <see cref="BinaryVector{TItem}"/> to <see cref="BsonBinaryData"/>.
        /// </summary>
        /// <typeparam name="TItem">The .NET data type.</typeparam>
        /// <param name="binaryVector">The binary vector.</param>
        /// <returns>A <see cref="BsonBinaryData"/> instance.</returns>
        public static BsonBinaryData ToBsonBinaryData<TItem>(this BinaryVector<TItem> binaryVector)
            where TItem : struct
        {
            var bytes = BinaryVectorWriter.WriteToBytes(binaryVector);
            var binaryData = new BsonBinaryData(bytes, BsonBinarySubType.Vector);

            return binaryData;
        }
    }
}
