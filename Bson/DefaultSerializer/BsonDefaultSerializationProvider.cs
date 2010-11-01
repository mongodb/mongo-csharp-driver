/* Copyright 2010 10gen Inc.
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

namespace MongoDB.Bson.DefaultSerializer {
    public class BsonDefaultSerializationProvider : IBsonSerializationProvider {
        #region private static fields
        private static object staticLock = new object();
        private static BsonDefaultSerializationProvider singleton = new BsonDefaultSerializationProvider();
        private static Dictionary<Type, Type> genericSerializerDefinitions = new Dictionary<Type, Type>();
        #endregion

        #region constructors
        private BsonDefaultSerializationProvider() {
        }
        #endregion

        #region public static properties
        public static BsonDefaultSerializationProvider Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void Initialize() {
            RegisterSerializers();
        }

        public static Type LookupGenericSerializerDefinition(
            Type genericTypeDefinition
        ) {
            lock (staticLock) {
                Type genericSerializerDefinition;
                genericSerializerDefinitions.TryGetValue(genericTypeDefinition, out genericSerializerDefinition);
                return genericSerializerDefinition;
            }
        }

        public static void RegisterGenericSerializerDefinition(
            Type genericTypeDefinition,
            Type genericSerializerDefinition
        ) {
            lock (staticLock) {
                genericSerializerDefinitions[genericTypeDefinition] = genericSerializerDefinition;
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
                        // so every generic serializer definition has a matching Registration class
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
        public IBsonIdGenerator GetIdGenerator(
            Type type
        ) {
            // TODO: implement more IdGenerators?
            if (type == typeof(ObjectId)) {
                return new ObjectIdGenerator();
            } else if (type == typeof(Guid)) {
                return new GuidGenerator();
            } else {
                return null;
            }
        }

        public IBsonSerializer GetSerializer(
            Type type,
            object serializationOptions
        ) {
            if (type.IsArray) {
                return GenericArraySerializer.Singleton;
            }

            if (type.IsEnum) {
                return GeneralEnumSerializer.GetSerializer(serializationOptions);
            }

            if (
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>)
            ) {
                return NullableTypeSerializer.Singleton;
            }

            if (type.IsGenericType) {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var genericSerializerDefinition = LookupGenericSerializerDefinition(genericTypeDefinition);
                if (genericSerializerDefinition != null) {
                    var genericSerializerType = genericSerializerDefinition.MakeGenericType(type.GetGenericArguments());
                    return (IBsonSerializer) Activator.CreateInstance(genericSerializerType);
                }
            }

            if (
                type.IsClass &&
                !typeof(Array).IsAssignableFrom(type) &&
                !typeof(Enum).IsAssignableFrom(type)
            ) {
                return BsonClassMapSerializer.Singleton;
            }

            return null;
        }
        #endregion
    }
}
