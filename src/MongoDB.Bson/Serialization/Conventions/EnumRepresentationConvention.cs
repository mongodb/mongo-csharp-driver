/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that allows you to set the Enum serialization representation
    /// </summary>
    public class EnumRepresentationConvention : ConventionBase, IMemberMapConvention
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumRepresentationConvention" /> class.
        /// </summary>  
        /// <param name="representation">The serialization representation. 0 is used to detect representation
        /// from the enum itself.</param>
        public EnumRepresentationConvention(BsonType representation)
        {
            if (!((representation == 0) ||
                (representation == BsonType.String) ||
                (representation == BsonType.Int32) ||
                (representation == BsonType.Int64)))
            {
                throw new ArgumentException("Enums can only be represented as String, Int32, Int64 or the type of the enum");
            }
            _representation = representation;
        }

        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public virtual void Apply(BsonMemberMap memberMap)
        {
            if (IsEnumType(memberMap.MemberType))
            {
                var serializer = memberMap.GetSerializer();
                var reconfiguredSerializer = Apply(serializer);
                memberMap.SetSerializer(reconfiguredSerializer);
            }
        }

        // protected methods
        /// <summary>
        /// Reconfigures the specified serializer by applying this attribute to it.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>A reconfigured serializer.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        protected virtual IBsonSerializer ApplyChild(IBsonSerializer serializer)
        {
            // if none of the overrides applied the attribute to the serializer see if it can be applied to a child serializer
            var childSerializerConfigurable = serializer as IChildSerializerConfigurable;
            if (childSerializerConfigurable != null)
            {
                var childSerializer = childSerializerConfigurable.ChildSerializer;
                var reconfiguredChildSerializer = Apply(childSerializer);
                return childSerializerConfigurable.WithChildSerializer(reconfiguredChildSerializer);
            }

            var message = string.Format(
                "A serializer of type '{0}' is not configurable using an attribute of type '{1}'.",
                BsonUtils.GetFriendlyTypeName(serializer.GetType()),
                BsonUtils.GetFriendlyTypeName(this.GetType()));
            throw new NotSupportedException(message);
        }

        private IBsonSerializer Apply(IBsonSerializer serializer)
        {
            var representationConfigurable = serializer as IRepresentationConfigurable;
            if (representationConfigurable != null)
            {
                var reconfiguredSerializer = representationConfigurable.WithRepresentation(_representation);

                var converterConfigurable = reconfiguredSerializer as IRepresentationConverterConfigurable;
                if (converterConfigurable != null)
                {
                    var converter = new RepresentationConverter(false, false);
                    reconfiguredSerializer = converterConfigurable.WithConverter(converter);
                }

                return reconfiguredSerializer;
            }

            // for backward compatibility representations of Array and Document are mapped to DictionaryRepresentations if possible
            var dictionaryRepresentationConfigurable = serializer as IDictionaryRepresentationConfigurable;
            if (dictionaryRepresentationConfigurable != null)
            {
                if (_representation == BsonType.Array || _representation == BsonType.Document)
                {
                    var dictionaryRepresentation = (_representation == BsonType.Array) ? DictionaryRepresentation.ArrayOfArrays : DictionaryRepresentation.Document;
                    return dictionaryRepresentationConfigurable.WithDictionaryRepresentation(dictionaryRepresentation);
                }
            }

            return ApplyChild(serializer);
        }

        private bool IsEnumType(Type t)
        {
            if (t.IsEnum) return true;
            Type u = Nullable.GetUnderlyingType(t);
            return (u != null) && u.IsEnum;
        }

    }
}
