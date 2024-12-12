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
using System.Reflection;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that allows you to set the Enum serialization representation
    /// </summary>
    public class EnumRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        // private fields
        private readonly BsonType _representation;
        private readonly bool _shouldApplyToCollections;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumRepresentationConvention" /> class.
        /// </summary>
        /// <param name="representation">The serialization representation. 0 is used to detect representation
        /// from the enum itself.</param>
        public EnumRepresentationConvention(BsonType representation)
            :this(representation, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumRepresentationConvention" /> class.
        /// </summary>
        /// <param name="representation">The serialization representation. 0 is used to detect representation
        /// from the enum itself.</param>
        /// <param name="shouldApplyToCollections">If set to true, the convention will be applied also to collection of enums, recursively.</param>
        public EnumRepresentationConvention(BsonType representation, bool shouldApplyToCollections)
        {
            EnsureRepresentationIsValidForEnums(representation);
            _representation = representation;
            _shouldApplyToCollections = shouldApplyToCollections;
        }

        /// <summary>
        /// Gets the representation.
        /// </summary>
        public BsonType Representation => _representation;

        /// <summary>
        /// Gets a boolean indicating if this convention should be also applied to collections of enums.
        /// </summary>
        public bool ShouldApplyToCollections => _shouldApplyToCollections;

        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            var serializer = memberMap.GetSerializer();

            if (memberMap.MemberType.IsEnum && serializer is IRepresentationConfigurable representationConfigurableSerializer)
            {
                memberMap.SetSerializer(representationConfigurableSerializer.WithRepresentation(_representation));
                return;
            }

            if (_shouldApplyToCollections || IsNullableEnum(memberMap.MemberType))
            {
                var reconfiguredSerializer = Reconfigure(serializer);
                if (reconfiguredSerializer is not null)
                {
                    memberMap.SetSerializer(reconfiguredSerializer);
                }
            }
        }

        private IBsonSerializer Reconfigure(IBsonSerializer serializer)
        {
            if (serializer is IChildSerializerConfigurable childSerializerConfigurable)
            {
                var childSerializer = childSerializerConfigurable.ChildSerializer;
                var reconfiguredChildSerializer = Reconfigure(childSerializer);
                return reconfiguredChildSerializer is null ? null : childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer);
            }

            if (serializer.ValueType.IsEnum && serializer is IRepresentationConfigurable representationConfigurable)
            {
                return representationConfigurable.WithRepresentation(_representation);
            }

            return null;
        }

        // private methods
        // private methods
        private bool IsNullableEnum(Type type)
        {
            return
                type.GetTypeInfo().IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                Nullable.GetUnderlyingType(type)!.GetTypeInfo().IsEnum;
        }

        private void EnsureRepresentationIsValidForEnums(BsonType representation)
        {
            if (representation is 0 or BsonType.String or BsonType.Int32 or BsonType.Int64)
            {
                return;
            }
            throw new ArgumentException("Enums can only be represented as String, Int32, Int64 or the type of the enum", nameof(representation));
        }
    }
}
