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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson
{
    /// <summary>
    /// Sets the representation for this field or property as a binary vector with the specified data type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class BinaryVectorAttribute : Attribute, IBsonMemberMapAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryVectorAttribute"/> class.
        /// </summary>
        public BinaryVectorAttribute(BinaryVectorDataType dataType)
        {
            DataType = dataType;
        }

        /// <summary>
        /// Gets the vector data type.
        /// </summary>
        public BinaryVectorDataType DataType { get; init; }

        /// <summary>
        /// Applies the attribute to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            var serializer = CreateSerializer(memberMap.MemberType);
            memberMap.SetSerializer(serializer);
        }

        private IBsonSerializer CreateSerializer(Type type) => BinaryVectorSerializer.CreateSerializer(type, DataType);
    }
}
