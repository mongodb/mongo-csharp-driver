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

namespace MongoDB.Bson
{
    /// <summary>
    /// Represents the data type of a binary Vector.
    /// </summary>
    /// <seealso cref="BinaryVector{TItem}"/>
    public enum BinaryVectorDataType
    {
        /// <summary>
        /// Data type is float32.
        /// </summary>
        Float32 = 0x27,

        /// <summary>
        /// Data type is int8.
        /// </summary>
        Int8 = 0x03,

        /// <summary>
        /// Data type is packed bit.
        /// </summary>
        PackedBit = 0x10
    }
}
