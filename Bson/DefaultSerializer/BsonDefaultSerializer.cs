/* Copyright 2010-2011 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.DefaultSerializer.Conventions;

namespace MongoDB.Bson.DefaultSerializer {
    public class BsonDefaultSerializer : IBsonSerializationProvider {
        #region private static fields
        private static object staticLock = new object();
        private static BsonDefaultSerializer instance = new BsonDefaultSerializer();
        private static Dictionary<Type, IDiscriminatorConvention> discriminatorConventions = new Dictionary<Type, IDiscriminatorConvention>();
        private static Dictionary<BsonValue, HashSet<Type>> discriminators = new Dictionary<BsonValue, HashSet<Type>>();
        #endregion

        #region constructors
        public BsonDefaultSerializer() {
        }
        #endregion

        #region public static properties
        public static BsonDefaultSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public static methods
        public static void Initialize() {
            RegisterSerializers();
        }

        public static Type LookupActualType(
            Type nominalType,
            BsonValue discriminator
        ) {
            if (discriminator == null) {
                return nominalType;
            }

            // TODO: will there be too much contention on staticLock?
            lock (staticLock) {
                Type actualType = null;

                // TODO: I'm not sure this is quite right, what if nominalType doesn't use class maps?
                BsonClassMap.LookupClassMap(nominalType); // make sure any "known types" of nominal type have been registered

                HashSet<Type> hashSet;
                if (discriminators.TryGetValue(discriminator, out hashSet)) {
                    foreach (var type in hashSet) {
                        if (nominalType.IsAssignableFrom(type)) {
                            if (actualType == null) {
                                actualType = type;
                            } else {
                                string message = string.Format("Ambiguous discriminator: {0}", discriminator);
                                throw new BsonSerializationException(message);
                            }
                        }
                    }
                }

                if (actualType == null && discriminator.IsString) {
                    actualType = Type.GetType(discriminator.AsString); // see if it's a Type name
                }

                if (actualType == null) {
                    string message = string.Format("Unknown discriminator value: {0}", discriminator);
                    throw new BsonSerializationException(message);
                }

                if (!nominalType.IsAssignableFrom(actualType)) {
                    string message = string.Format("Actual type {0} is not assignable to expected type {1}", actualType.FullName, nominalType.FullName);
                    throw new FileFormatException(message);
                }

                return actualType;
            }
        }

        public static IDiscriminatorConvention LookupDiscriminatorConvention(
            Type type
        ) {
            lock (staticLock) {
                IDiscriminatorConvention convention;
                if (!discriminatorConventions.TryGetValue(type, out convention)) {
                    // if there is no convention registered for object register the default one
                    if (!discriminatorConventions.ContainsKey(typeof(object))) {
                        var defaultDiscriminatorConvention = StandardDiscriminatorConvention.Hierarchical;
                        discriminatorConventions.Add(typeof(object), defaultDiscriminatorConvention);
                        if (type == typeof(object)) {
                            return defaultDiscriminatorConvention;
                        }
                    }

                    if (type.IsInterface) {
                        // TODO: should convention for interfaces be inherited from parent interfaces?
                        convention = discriminatorConventions[typeof(object)];
                        discriminatorConventions[type] = convention;
                    } else {
                        // inherit the discriminator convention from the closest parent that has one
                        Type parentType = type.BaseType;
                        while (convention == null) {
                            if (parentType == null) {
                                var message = string.Format("No discriminator convention found for type: {0}", type.FullName);
                                throw new BsonSerializationException(message);
                            }
                            if (discriminatorConventions.TryGetValue(parentType, out convention)) {
                                break;
                            }
                            parentType = parentType.BaseType;
                        }

                        // register this convention for all types between this and the parent type where we found the convention
                        var unregisteredType = type;
                        while (unregisteredType != parentType) {
                            BsonDefaultSerializer.RegisterDiscriminatorConvention(unregisteredType, convention);
                            unregisteredType = unregisteredType.BaseType;
                        }
                    }
                }
                return convention;
            }
        }

        public static void RegisterDiscriminator(
            Type type,
            BsonValue discriminator
        ) {
            lock (staticLock) {
                HashSet<Type> hashSet;
                if (!discriminators.TryGetValue(discriminator, out hashSet)) {
                    hashSet = new HashSet<Type>();
                    discriminators.Add(discriminator, hashSet);
                }

                hashSet.Add(type);
            }
        }

        public static void RegisterDiscriminatorConvention(
            Type type,
            IDiscriminatorConvention convention
        ) {
            lock (staticLock) {
                if (!discriminatorConventions.ContainsKey(type)) {
                    discriminatorConventions.Add(type, convention);
                } else {
                    var message = string.Format("There is already a discriminator convention registered for type: {0}", type.FullName);
                    throw new BsonSerializationException(message);
                }
            }
        }
        #endregion

        #region private static methods
        // automatically register all BsonSerializers found in the Bson library
        private static void RegisterSerializers() {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes()) {
                if (typeof(IBsonSerializer).IsAssignableFrom(type) && type != typeof(IBsonSerializer)) {
                    if (type.IsGenericType) {
                        // static methods in generic type definitions don't really work
                        // so every generic serializer definition has a matching static Registration class to hold the registration method
                        var registrationTypeName = Regex.Replace(type.FullName, @"`\d+$", "Registration");
                        var registrationType = type.Assembly.GetType(registrationTypeName);
                        if (registrationType != null) {
                            var registerGenericSerializerDefinitionsInfo = registrationType.GetMethod("RegisterGenericSerializerDefinitions", BindingFlags.Public | BindingFlags.Static);
                            if (registerGenericSerializerDefinitionsInfo != null) {
                                registerGenericSerializerDefinitionsInfo.Invoke(null, null);
                            }
                        }
                    } else {
                        var registerSerializersInfo = type.GetMethod("RegisterSerializers", BindingFlags.Public | BindingFlags.Static);
                        if (registerSerializersInfo != null) {
                            registerSerializersInfo.Invoke(null, null);
                        }
                    }
                }
            }
        }
        #endregion

        #region public methods
        public IBsonSerializer GetSerializer(
            Type type
        ) {
            if (type.IsArray) {
                var elementType = type.GetElementType();
                switch (type.GetArrayRank()) {
                    case 1:
                        var arraySerializerDefinition = typeof(ArraySerializer<>);
                        var arraySerializerType = arraySerializerDefinition.MakeGenericType(elementType);
                        return (IBsonSerializer) Activator.CreateInstance(arraySerializerType);
                    case 2:
                        var twoDimensionalArraySerializerDefinition = typeof(TwoDimensionalArraySerializer<>);
                        var twoDimensionalArraySerializerType = twoDimensionalArraySerializerDefinition.MakeGenericType(elementType);
                        return (IBsonSerializer) Activator.CreateInstance(twoDimensionalArraySerializerType);
                    case 3:
                        var threeDimensionalArraySerializerDefinition = typeof(ThreeDimensionalArraySerializer<>);
                        var threeDimensionalArraySerializerType = threeDimensionalArraySerializerDefinition.MakeGenericType(elementType);
                        return (IBsonSerializer) Activator.CreateInstance(threeDimensionalArraySerializerType);
                    default:
                        var message = string.Format("No serializer found for array for rank: {0}", type.GetArrayRank());
                        throw new BsonSerializationException(message);
                }
            }

            if (type.IsEnum) {
                return EnumSerializer.Instance;
            }

            if (
                (type.IsClass || (type.IsValueType && !type.IsPrimitive)) &&
                !typeof(Array).IsAssignableFrom(type) &&
                !typeof(Enum).IsAssignableFrom(type)
            ) {
                return BsonClassMapSerializer.Instance;
            }

            return null;
        }
        #endregion
    }
}
