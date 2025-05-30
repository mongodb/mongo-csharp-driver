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
using System.Collections;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that allows you to set the Enum serialization representation
    /// </summary>
    public class EnumRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        // private fields
        private readonly BsonType _representation;
        private readonly bool _topLevelOnly;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumRepresentationConvention" /> class.
        /// </summary>
        /// <param name="representation">The serialization representation. 0 is used to detect representation
        /// from the enum itself.</param>
        public EnumRepresentationConvention(BsonType representation)
            :this(representation, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumRepresentationConvention" /> class.
        /// </summary>
        /// <param name="representation">The serialization representation. 0 is used to detect representation
        /// from the enum itself.</param>
        /// <param name="topLevelOnly">If set to true, the convention will be applied only to top level enum properties, and not collections of enums, for example.</param>
        public EnumRepresentationConvention(BsonType representation, bool topLevelOnly)
        {
            EnsureRepresentationIsValidForEnums(representation);
            _representation = representation;
            _topLevelOnly = topLevelOnly;
        }

        /// <summary>
        /// Gets the representation.
        /// </summary>
        public BsonType Representation => _representation;

        /// <summary>
        /// Gets a boolean indicating if this convention should be also applied only to the top level enum properties and not to others,
        /// collections of enums for example. True by default.
        /// </summary>
        public bool TopLevelOnly => _topLevelOnly;

        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap) => Apply(memberMap, BsonSerializer.DefaultSerializationDomain);

        /// <inheritdoc />
        public void Apply(BsonMemberMap memberMap, IBsonSerializationDomain domain)
        {
            var memberType = memberMap.MemberType;

            if (!CouldApply(memberType))
            {
                return;
            }

            var serializer = memberMap.GetSerializer();
            var reconfiguredSerializer = _topLevelOnly && !serializer.ValueType.IsNullableEnum() ?
                Reconfigure(serializer) :
                SerializerConfigurator.ReconfigureSerializerRecursively(serializer, Reconfigure);

            if (reconfiguredSerializer is not null)
            {
                memberMap.SetSerializer(reconfiguredSerializer);
            }

            IBsonSerializer Reconfigure(IBsonSerializer s)
                => s.ValueType.IsEnum ? (s as IRepresentationConfigurable)?.WithRepresentation(_representation) : null;

            bool CouldApply(Type type)
                => type.IsEnum || type.IsNullableEnum() || type.IsArray || typeof(IEnumerable).IsAssignableFrom(type);
        }

        // private methods
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
