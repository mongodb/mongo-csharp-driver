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
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Attributes
{
    /// <summary>
    /// Sets the representation for this field or property as BSON Vector and specifies the serialization options.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class BsonVectorAttribute : Attribute, IBsonMemberMapAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BsonVectorAttribute"/> class.
        /// </summary>
        public BsonVectorAttribute(BsonVectorDataType dataType)
        {
            DataType = dataType;
        }

        /// <summary>
        /// Gets the vector data type representation.
        /// </summary>
        public BsonVectorDataType DataType { get; init; }

        /// <summary>
        /// Applies the attribute to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            var serializer = CreateSerializer(memberMap.MemberType);
            memberMap.SetSerializer(serializer);
        }

        private IBsonSerializer CreateSerializer(Type type)
        {
            // Arrays
            if (type.IsArray)
            {
                var itemType = type.GetElementType();
                var vectorSerializerType = typeof(BsonVectorArraySerializer<>).MakeGenericType(itemType);
                var vectorSerializer = (IBsonSerializer)Activator.CreateInstance(vectorSerializerType, DataType);

                return vectorSerializer;
            }

            // BsonVector
            if (type == typeof(BsonVectorFloat32) ||
                type == typeof(BsonVectorInt8) ||
                type == typeof(BsonVectorPackedBit))
            {
                var vectorSerializerType = typeof(BsonVectorSerializer<,>).MakeGenericType(type, GetItemType(type.BaseType));
                var vectorSerializer = (IBsonSerializer)Activator.CreateInstance(vectorSerializerType, DataType);

                return vectorSerializer;
            }

            // Memory/ReadonlyMemory
            var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (genericTypeDefinition == typeof(Memory<>))
            {
                var vectorSerializerType = typeof(BsonVectorMemorySerializer<>).MakeGenericType(GetItemType(type));
                var vectorSerializer = (IBsonSerializer)Activator.CreateInstance(vectorSerializerType, DataType);

                return vectorSerializer;
            }
            else if (genericTypeDefinition == typeof(ReadOnlyMemory<>))
            {
                var vectorSerializerType = typeof(BsonVectorReadOnlyMemorySerializer<>).MakeGenericType(GetItemType(type));
                var vectorSerializer = (IBsonSerializer)Activator.CreateInstance(vectorSerializerType, DataType);

                return vectorSerializer;
            }

            throw new InvalidOperationException($"Type {type} is not supported for a binary vector.");

            Type GetItemType(Type actualType)
            {
                var arguments = actualType.GetGenericArguments();
                if (arguments.Length != 1)
                {
                    throw new InvalidOperationException($"Type {type} is not supported for a binary vector.");
                }

                return arguments[0];
            }
        }
    }
}
