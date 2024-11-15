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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    ///
    /// </summary>
    public class AllowedTypesConvention
    {
        // private fields
        private Func<Type, bool> _allowedDeserializationTypes;
        private Func<Type, bool> _allowedSerializationTypes;

        // 1) Set allowed types with func
        /// <summary>
        ///
        /// </summary>
        /// <param name="allowedTypes"></param>
        public AllowedTypesConvention(Func<Type, bool> allowedTypes)
            : this(allowedTypes, allowedTypes)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="allowedDeserializationTypes"></param>
        /// <param name="allowedSerializationTypes"></param>
        public AllowedTypesConvention(Func<Type, bool> allowedDeserializationTypes, Func<Type, bool> allowedSerializationTypes)
        {
            _allowedDeserializationTypes = allowedDeserializationTypes;
            _allowedSerializationTypes = allowedSerializationTypes;
        }

        // 2) Set allowed types by passing list of types
        /// <summary>
        ///
        /// </summary>
        /// <param name="allowedTypes"></param>
        public AllowedTypesConvention(IEnumerable<Type> allowedTypes)
            : this(allowedTypes, allowedTypes)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="allowedDeserializationTypes"></param>
        /// <param name="allowedSerializationTypes"></param>
        public AllowedTypesConvention(IEnumerable<Type> allowedDeserializationTypes, IEnumerable<Type> allowedSerializationTypes)
        {
            var allowedDeserializationTypesArray = allowedDeserializationTypes as Type[] ?? allowedDeserializationTypes.ToArray();
            var allowedSerializationTypesArray = allowedSerializationTypes as Type[] ?? allowedSerializationTypes.ToArray();

            _allowedDeserializationTypes = allowedDeserializationTypesArray.Contains;
            _allowedDeserializationTypes = allowedSerializationTypesArray.Contains;
        }

        // 3a) Set allowed types by setting allowedAssembly. In this case we assume
        // both allowedSerialization/deserialization types are in the same assembly
        // Can be called like: new AllowedTypesConvention(typeof(myType).Assembly);
        // (also Assembly.GetExecutingAssembly would work, but less efficient)
        /// <summary>
        ///
        /// </summary>
        /// <param name="allowedAssembly"></param>
        public AllowedTypesConvention(Assembly allowedAssembly)
        {
            _allowedDeserializationTypes = _allowedSerializationTypes = t => t.Assembly == allowedAssembly;
        }

        // 3b) Set allowed types by setting the allowed assembly as the calling assembly
        /// <summary>
        ///
        /// </summary>
        public AllowedTypesConvention() : this(Assembly.GetCallingAssembly())
        {
        }

        // This is defined as a property, so it can be used together with all the other possible constructors.
        // According to CA1044 setter only properties shouldn't exist, but I think it would make sense for this case
        // but I'm open to other suggestions
        /// <summary>
        ///
        /// </summary>
#pragma warning disable CA1044
        public bool AllowDefaultFrameworkTypes
#pragma warning restore CA1044
        {
            set
            {
                if (!value) return;

                _allowedDeserializationTypes =
                    t => _allowedDeserializationTypes(t) || ObjectSerializer.DefaultAllowedTypes(t);
                _allowedSerializationTypes =
                    t => _allowedSerializationTypes(t) || ObjectSerializer.DefaultAllowedTypes(t);
            }
        }

        // Used for fast testing
        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool IsTypeAllowedForDeserialization(Type type)
        {
            return _allowedDeserializationTypes(type);
        }

        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            var memberType = memberMap.MemberType;

            if (memberType == typeof(object) &&  memberMap.GetSerializer() is ObjectSerializer os)
            {
                var reconfiguredSerializer =
                    os.WithAllowedTypes(_allowedDeserializationTypes, _allowedSerializationTypes);
                memberMap.SetSerializer(reconfiguredSerializer);
            }
        }
    }
}