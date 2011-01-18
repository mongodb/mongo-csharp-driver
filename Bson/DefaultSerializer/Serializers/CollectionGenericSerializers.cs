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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public static class EnumerableSerializerRegistration {
        #region public static methods
        public static void RegisterGenericSerializerDefinitions() {
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(HashSet<>), typeof(EnumerableSerializer<>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(ICollection<>), typeof(EnumerableSerializer<>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(IEnumerable<>), typeof(EnumerableSerializer<>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(IList<>), typeof(EnumerableSerializer<>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(List<>), typeof(EnumerableSerializer<>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(LinkedList<>), typeof(EnumerableSerializer<>));
        }
        #endregion
    }

    public class EnumerableSerializer<T> : BsonBaseSerializer {
        #region constructors
        public EnumerableSerializer() {
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Array) {
                bsonReader.ReadStartArray();
                var list = (nominalType == typeof(List<T>) || nominalType.IsInterface) ? new List<T>() : (ICollection<T>) Activator.CreateInstance(nominalType);
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(T));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = (T) serializer.Deserialize(bsonReader, typeof(T), elementType, null);
                    list.Add(element);
                }
                bsonReader.ReadEndArray();
                return list;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteStartArray();
                foreach (var element in (IEnumerable<T>) value) {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion
    }

    public static class QueueSerializerRegistration {
        #region public static methods
        public static void RegisterGenericSerializerDefinitions() {
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(Queue<>), typeof(QueueSerializer<>));
        }
        #endregion
    }

    public class QueueSerializer<T> : BsonBaseSerializer {
        #region constructors
        public QueueSerializer() {
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Array) {
                bsonReader.ReadStartArray();
                var queue = new Queue<T>();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(T));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = (T) serializer.Deserialize(bsonReader, typeof(T), elementType, null);
                    queue.Enqueue(element);
                }
                bsonReader.ReadEndArray();
                return queue;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteStartArray();
                foreach (var element in (Queue<T>) value) {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion
    }

    public static class StackSerializerRegistration {
        #region public static methods
        public static void RegisterGenericSerializerDefinitions() {
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(Stack<>), typeof(StackSerializer<>));
        }
        #endregion
    }

    public class StackSerializer<T> : BsonBaseSerializer {
        #region constructors
        public StackSerializer() {
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Array) {
                bsonReader.ReadStartArray();
                var stack = new Stack<T>();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(T));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = (T) serializer.Deserialize(bsonReader, typeof(T), elementType, null);
                    stack.Push(element);
                }
                bsonReader.ReadEndArray();
                return stack;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                bsonWriter.WriteStartArray();
                var outputOrder = new List<T>((Stack<T>) value); // serialize first pushed item first (reverse of enumerator order)
                outputOrder.Reverse();
                foreach (var element in outputOrder) {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion
    }
}
