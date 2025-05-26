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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// A convention that allows to set the types that can be safely serialized and deserialized with the <see cref="ObjectSerializer"/>.
    /// </summary>
    public sealed class ObjectSerializerAllowedTypesConvention : ConventionBase, IMemberMapConvention
    {
        // static properties

        /// <summary>
        /// A predefined <see cref="ObjectSerializerAllowedTypesConvention"/> where all types are allowed for both serialization and deserialization.
        /// </summary>
        public static ObjectSerializerAllowedTypesConvention AllowAllTypes { get; } = new(ObjectSerializer.AllAllowedTypes);

        /// <summary>
        /// A predefined <see cref="ObjectSerializerAllowedTypesConvention"/> where no types are allowed for both serialization and deserialization.
        /// </summary>
        public static ObjectSerializerAllowedTypesConvention AllowNoTypes { get; } =
            new(ObjectSerializer.NoAllowedTypes) { AllowDefaultFrameworkTypes = false };

        /// <summary>
        /// A predefined <see cref="ObjectSerializerAllowedTypesConvention"/> where only default framework types are allowed for both serialization and deserialization.
        /// </summary>
        public static ObjectSerializerAllowedTypesConvention AllowOnlyDefaultFrameworkTypes { get; } = new();

        //static methods

        /// <summary>
        /// Builds a predefined <see cref="ObjectSerializerAllowedTypesConvention"/> where all calling assembly types and default framework types are allowed for both serialization and deserialization.
        /// </summary>
        public static ObjectSerializerAllowedTypesConvention GetAllowAllCallingAssemblyAndDefaultFrameworkTypesConvention() => new(Assembly.GetCallingAssembly());

        // private fields
        private readonly Func<Type, bool> _allowedDeserializationTypes;
        private readonly Func<Type, bool> _allowedSerializationTypes;
        private readonly bool _allowDefaultFrameworkTypes = true;
        private readonly Lazy<Func<Type, bool>> _effectiveAllowedDeserializationTypes;
        private readonly Lazy<Func<Type, bool>> _effectiveAllowedSerializationTypes;

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
            : this()
        {
            _allowedDeserializationTypes = allowedDeserializationTypes;
            _allowedSerializationTypes = allowedSerializationTypes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedTypes">A collection of the allowed types for both serialization and deserialization.</param>
        public ObjectSerializerAllowedTypesConvention(IEnumerable<Type> allowedTypes)
            : this()
        {
            var allowedTypesArray = allowedTypes.ToArray();

            _allowedDeserializationTypes =  allowedTypesArray.Contains;
            _allowedSerializationTypes = allowedTypesArray.Contains;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedDeserializationTypes">A collection of the allowed types for deserialization.</param>
        /// <param name="allowedSerializationTypes">A collection of the allowed types for serialization.</param>
        public ObjectSerializerAllowedTypesConvention(IEnumerable<Type> allowedDeserializationTypes, IEnumerable<Type> allowedSerializationTypes)
            : this()
        {
            var allowedDeserializationTypesArray = allowedDeserializationTypes.ToArray();
            var allowedSerializationTypesArray = allowedSerializationTypes.ToArray();

            _allowedDeserializationTypes = allowedDeserializationTypesArray.Contains;
            _allowedSerializationTypes = allowedSerializationTypesArray.Contains;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedAssemblies">A collection of allowed assemblies whose types can be serialized and deserialized.</param>
        public ObjectSerializerAllowedTypesConvention(params Assembly[] allowedAssemblies)
            : this()
        {
            _allowedDeserializationTypes = _allowedSerializationTypes = t => allowedAssemblies.Contains(t.Assembly);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        public ObjectSerializerAllowedTypesConvention()
        {
            _effectiveAllowedDeserializationTypes = new Lazy<Func<Type, bool>>(() => CreateEffectiveAllowedTypes(_allowedDeserializationTypes));
            _effectiveAllowedSerializationTypes = new Lazy<Func<Type, bool>>(() => CreateEffectiveAllowedTypes(_allowedSerializationTypes));

            Func<Type, bool> CreateEffectiveAllowedTypes(Func<Type, bool> allowedTypes)
            {
                return (allowedTypes, _allowDefaultFrameworkTypes) switch
                {
                    (null, false) => ObjectSerializer.NoAllowedTypes,
                    (null, true) => ObjectSerializer.DefaultAllowedTypes,
                    (not null, false) => allowedTypes,
                    (not null, true) => t => allowedTypes(t) || ObjectSerializer.DefaultAllowedTypes(t)
                };
            }
        }

        /// <summary>
        /// Indicates whether default framework types are included for serialization and deserialization. Defaults to true.
        /// </summary>
        public bool AllowDefaultFrameworkTypes
        {
            get => _allowDefaultFrameworkTypes;
            init => _allowDefaultFrameworkTypes = value;
        }

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

            var reconfiguredSerializer = Reconfigure(serializer);
            if (reconfiguredSerializer is not null)
            {
                memberMap.SetSerializer(reconfiguredSerializer);
            }

            bool CouldApply(Type type)
                => type == typeof(object) || type.IsNullable() || type.IsArray || typeof(IEnumerable).IsAssignableFrom(type);
        }

        // private methods
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
                return objectSerializer.WithAllowedTypes(_effectiveAllowedDeserializationTypes.Value, _effectiveAllowedSerializationTypes.Value);
            }

            return null;
        }
    }
}