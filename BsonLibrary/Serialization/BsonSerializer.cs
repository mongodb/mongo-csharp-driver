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
using System.Linq;
using System.Text;

using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization {
    public static class BsonSerializer {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, IBsonSerializer> registry = new Dictionary<Type, IBsonSerializer>();
        private static Dictionary<Type, IBsonSerializer> cache = new Dictionary<Type, IBsonSerializer>();
        private static IBsonSerializer defaultSerializer = BsonPropertySerializer.Singleton;
        #endregion

        #region public static properties
        public static IBsonSerializer DefaultSerializer {
            get { return defaultSerializer; }
            set { defaultSerializer = value; }
        }
        #endregion

        #region public static methods
        public static object Deserialize(
            BsonReader bsonReader,
            Type type
        ) {
            // optimize for the most common case
            if (type == typeof(BsonDocument)) {
                var obj = new BsonDocument();
                obj.Deserialize(bsonReader);
                return obj;
            }

            var serializer = FindSerializer(type);
            return serializer.Deserialize(bsonReader, type);
        }

        public static IBsonSerializer FindSerializer(
            Type type
        ) {
            lock (staticLock) {
                IBsonSerializer serializer;
                if (cache.TryGetValue(type, out serializer)) {
                    return serializer;
                }

                if (type.GetInterface(typeof(IBsonSerializable).FullName) != null) {
                    serializer = BsonSerializableSerializer.Singleton;
                } else {
                    Type ancestorType = type;
                    while (ancestorType != null) {
                        if (registry.TryGetValue(ancestorType, out serializer)) {
                            break;
                        }
                        ancestorType = ancestorType.BaseType;
                    }

                    if (serializer == null) {
                        serializer = defaultSerializer;
                    }
                }

                cache[type] = serializer;
                return serializer;
            }
        }

        public static void RegisterSerializer(
            Type type,
            IBsonSerializer serializer
        ) {
            lock (staticLock) {
                registry[type] = serializer;
                cache.Clear();
            }
        }

        public static void Serialize(
            BsonWriter bsonWriter,
            object obj,
            bool serializeIdFirst
        ) {
            // optimize for the most common case
            var bsonSerializable = obj as IBsonSerializable;
            if (bsonSerializable != null) {
                bsonSerializable.Serialize(bsonWriter, serializeIdFirst);
                return;
            }

            var serializer = FindSerializer(obj.GetType());
            serializer.Serialize(bsonWriter, obj, serializeIdFirst);
        }

        public static void UnregisterSerializer(
            Type type
        ) {
            lock (staticLock) {
                registry.Remove(type);
                cache.Clear();
            }
        }
        #endregion
    }
}
