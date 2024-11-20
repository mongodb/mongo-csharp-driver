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
    /// A convention that allows to set the types that can be safely serialized and deserialized with the <see cref="ObjectSerializer"/>.
    /// </summary>
    public sealed class ObjectSerializerAllowedTypesConvention
    {
        // private fields
        private readonly Func<Type, bool> _allowedDeserializationTypes;
        private readonly Func<Type, bool> _allowedSerializationTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedTypes">A delegate that determines what types are allowed to be serialized and deserialized.</param>
        public ObjectSerializerAllowedTypesConvention(Func<Type, bool> allowedTypes)
            : this(allowedTypes, allowedTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedDeserializationTypes">A delegate that determines what types are allowed to be deserialized.</param>
        /// <param name="allowedSerializationTypes">A delegate that determines what types are allowed to be serialized.</param>
        public ObjectSerializerAllowedTypesConvention(Func<Type, bool> allowedDeserializationTypes, Func<Type, bool> allowedSerializationTypes)
        {
            _allowedDeserializationTypes = allowedDeserializationTypes;
            _allowedSerializationTypes = allowedSerializationTypes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedTypes">A collection of the allowed types for both serialization and deserialization.</param>
        public ObjectSerializerAllowedTypesConvention(IEnumerable<Type> allowedTypes)
        {
            var allowedTypesArray = allowedTypes.ToArray();

            _allowedDeserializationTypes = t => Array.IndexOf(allowedTypesArray, t) != -1;
            _allowedSerializationTypes = t => Array.IndexOf(allowedTypesArray, t) != -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedDeserializationTypes">A collection of the allowed types for deserialization.</param>
        /// <param name="allowedSerializationTypes">A collection of the allowed types for serialization.</param>
        public ObjectSerializerAllowedTypesConvention(IEnumerable<Type> allowedDeserializationTypes, IEnumerable<Type> allowedSerializationTypes)
        {
            var allowedDeserializationTypesArray = allowedDeserializationTypes.ToArray();
            var allowedSerializationTypesArray = allowedSerializationTypes.ToArray();

            _allowedDeserializationTypes = t => Array.IndexOf(allowedDeserializationTypesArray, t) != -1;
            _allowedSerializationTypes = t => Array.IndexOf(allowedSerializationTypesArray, t) != -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedAssemblies">A collection of allowed assemblies whose types can be serialized and deserialized.</param>
        public ObjectSerializerAllowedTypesConvention(params Assembly[] allowedAssemblies)
        {
            _allowedDeserializationTypes = _allowedSerializationTypes = t => Array.IndexOf(allowedAssemblies, t.Assembly) != -1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class
        /// that allows all types contained in the calling assembly.
        /// </summary>
        public ObjectSerializerAllowedTypesConvention() : this(Assembly.GetCallingAssembly())
        {
        }

        /// <summary>
        /// Sets a value indicating whether default framework types should be added to the list of types that
        /// can be serialized and deserialized.
        /// </summary>
#pragma warning disable CA1044
        public bool AllowDefaultFrameworkTypes
#pragma warning restore CA1044
        {
            init
            {
                if (!value) return;

                var previousAllowedDeserializationTypes = _allowedDeserializationTypes;
                var previousAllowedSerializationTypes = _allowedSerializationTypes;

                _allowedDeserializationTypes =
                    t => previousAllowedDeserializationTypes(t) || ObjectSerializer.DefaultAllowedTypes(t);
                _allowedSerializationTypes =
                    t => previousAllowedSerializationTypes(t) || ObjectSerializer.DefaultAllowedTypes(t);
            }
        }

        /// <summary>
        /// Applies a modification to the member map.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public void Apply(BsonMemberMap memberMap)
        {
            var serializer = memberMap.GetSerializer();

            var reconfiguredSerializer = Reconfigure(serializer);
            if (reconfiguredSerializer is not null)
            {
                memberMap.SetSerializer(reconfiguredSerializer);
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

            if (serializer.ValueType == typeof(object) && serializer is ObjectSerializer objectSerializer)
            {
                return objectSerializer.WithAllowedTypes(_allowedDeserializationTypes, _allowedSerializationTypes);
            }

            return null;
        }
    }
}