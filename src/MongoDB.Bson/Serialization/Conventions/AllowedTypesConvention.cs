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
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    ///
    /// </summary>
    public class AllowedTypesConvention
    {
        // private fields
        private readonly Func<Type, bool> _allowedDeserializationTypes;
        private readonly Func<Type, bool> _allowedSerializationTypes;

        public AllowedTypesConvention(Func<Type, bool> allowedTypes)
        {
            _allowedDeserializationTypes = allowedTypes;
            _allowedSerializationTypes = allowedTypes;
        }

        public AllowedTypesConvention(Func<Type, bool> allowedDeserializationTypes, Func<Type, bool> allowedSerializationTypes )
        {
            _allowedDeserializationTypes = allowedDeserializationTypes;
            _allowedSerializationTypes = allowedSerializationTypes;
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