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
        private readonly bool _validateGenericArgumentsAndElementTypes;
        private readonly Lazy<Func<Type, bool>> _effectiveAllowedDeserializationTypes;
        private readonly Lazy<Func<Type, bool>> _effectiveAllowedSerializationTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedTypes">A delegate that determines what types are allowed to be serialized and deserialized.</param>
        /// <remarks>
        /// The delegate is called with the full type, including constructed generic types and array types. It is the
        /// delegate's responsibility to inspect generic arguments and array element types; a delegate that only looks
        /// at the type itself (for example at its assembly or namespace) will allow types such as
        /// Allowed.Wrapper&lt;NotAllowed.Anything&gt;.
        /// </remarks>
        public ObjectSerializerAllowedTypesConvention(Func<Type, bool> allowedTypes)
            : this(allowedTypes, allowedTypes)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        /// <param name="allowedDeserializationTypes">A delegate that determines what types are allowed to be deserialized.</param>
        /// <param name="allowedSerializationTypes">A delegate that determines what types are allowed to be serialized.</param>
        /// <remarks>
        /// The delegates are called with the full type, including constructed generic types and array types. It is each
        /// delegate's responsibility to inspect generic arguments and array element types; a delegate that only looks
        /// at the type itself (for example at its assembly or namespace) will allow types such as
        /// Allowed.Wrapper&lt;NotAllowed.Anything&gt;.
        /// </remarks>
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
        /// <remarks>
        /// A type is allowed when it is declared in an allowed assembly or, if <see cref="AllowDefaultFrameworkTypes"/>
        /// is true, is a default framework type. Constructed generic types and array types are only allowed when their
        /// generic arguments and element types are themselves allowed, so a generic type declared in an allowed assembly
        /// cannot be used to smuggle in a type from an assembly that is not allowed. When
        /// <see cref="AllowDefaultFrameworkTypes"/> is true the default framework generic types (for example
        /// <see cref="List{T}"/> and <see cref="Dictionary{TKey, TValue}"/>) can be constructed over allowed types, such
        /// as List&lt;AllowedAssembly.MyType&gt;.
        /// </remarks>
        public ObjectSerializerAllowedTypesConvention(params Assembly[] allowedAssemblies)
            : this(true)
        {
            if (allowedAssemblies == null)
            {
                throw new ArgumentNullException(nameof(allowedAssemblies));
            }

            _allowedDeserializationTypes = _allowedSerializationTypes = t => allowedAssemblies.Contains(t.Assembly);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSerializerAllowedTypesConvention"/> class.
        /// </summary>
        public ObjectSerializerAllowedTypesConvention()
            : this(false)
        {
        }

        private ObjectSerializerAllowedTypesConvention(bool validateGenericArgumentsAndElementTypes)
        {
            _validateGenericArgumentsAndElementTypes = validateGenericArgumentsAndElementTypes;
            _effectiveAllowedDeserializationTypes = new Lazy<Func<Type, bool>>(() => CreateEffectiveAllowedTypes(_allowedDeserializationTypes));
            _effectiveAllowedSerializationTypes = new Lazy<Func<Type, bool>>(() => CreateEffectiveAllowedTypes(_allowedSerializationTypes));

            Func<Type, bool> CreateEffectiveAllowedTypes(Func<Type, bool> allowedTypes)
            {
                if (_validateGenericArgumentsAndElementTypes && allowedTypes is not null)
                {
                    return WithGenericArgumentsAndElementTypesValidation(allowedTypes, _allowDefaultFrameworkTypes);
                }

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
        public void Apply(BsonMemberMap memberMap)
        {
            var memberType = memberMap.MemberType;

            if (!CouldApply(memberType))
            {
                return;
            }

            var serializer = memberMap.GetSerializer();

            var reconfiguredSerializer = SerializerConfigurator.ReconfigureSerializerRecursively(serializer, Reconfigure);

            if (reconfiguredSerializer is not null)
            {
                memberMap.SetSerializer(reconfiguredSerializer);
            }

            bool CouldApply(Type type)
            {
                var visited = new HashSet<Type>();
                return CouldApplyCore(type);

                bool CouldApplyCore(Type t)
                {
                    if (!visited.Add(t))
                    {
                        return false;
                    }

                    return t == typeof(object) ||
                           (t.IsArray && CouldApplyCore(t.GetElementType())) || // This is covered by IEnumerable<T>, but it's a good short-circuit
                           IsNonGenericEnumerable(t) ||
                           t.GetInterfaces().Prepend(t).Any(
                               i =>
                                   i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) && CouldApplyCore(i.GetGenericArguments()[0]) || // IEnumerable<T>
                                   i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && (CouldApplyCore(i.GetGenericArguments()[0]) || CouldApplyCore(i.GetGenericArguments()[1])) // IDictionary<TKey, TValue>
                           );
                }
            }

            // A type that implements IEnumerable but not IEnumerable<T> (e.g. ArrayList, Hashtable).
            static bool IsNonGenericEnumerable(Type type)
                => !type.IsArray &&
                   typeof(IEnumerable).IsAssignableFrom(type) &&
                   !type.GetInterfaces().Prepend(type).Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private static Func<Type, bool> WithGenericArgumentsAndElementTypesValidation(Func<Type, bool> allowedTypes, bool allowDefaultFrameworkTypes)
        {
            return IsAllowedType;

            bool IsAllowedType(Type type)
            {
                if (type.HasElementType)
                {
                    return IsAllowedType(type.GetElementType());
                }

                if (type.IsConstructedGenericType)
                {
                    var isAllowedDefinition =
                        allowedTypes(type) ||
                        (allowDefaultFrameworkTypes && ObjectSerializer.IsDefaultAllowedGenericTypeDefinition(type));

                    return isAllowedDefinition && type.GetGenericArguments().All(IsAllowedType);
                }

                return allowedTypes(type) || (allowDefaultFrameworkTypes && ObjectSerializer.DefaultAllowedTypes(type));
            }
        }

        // private methods
        private IBsonSerializer Reconfigure(IBsonSerializer serializer)
        {
            if (serializer.ValueType == typeof(object) && serializer is ObjectSerializer objectSerializer)
            {
                return objectSerializer.WithAllowedTypes(_effectiveAllowedDeserializationTypes.Value, _effectiveAllowedSerializationTypes.Value);
            }

            return null;
        }
    }
}