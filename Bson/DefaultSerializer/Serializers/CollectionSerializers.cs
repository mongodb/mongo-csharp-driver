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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class EnumerableSerializer : BsonBaseSerializer {
        #region private static fields
        private static EnumerableSerializer singleton = new EnumerableSerializer();
        #endregion

        #region constructors
        private EnumerableSerializer() {
        }
        #endregion

        #region public static properties
        public static EnumerableSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ArrayList), singleton);
            BsonSerializer.RegisterSerializer(typeof(ICollection), singleton);
            BsonSerializer.RegisterSerializer(typeof(IEnumerable), singleton);
            BsonSerializer.RegisterSerializer(typeof(IList), singleton);
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
                var list = new ArrayList();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = serializer.Deserialize(bsonReader, typeof(object), elementType, null);
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
                foreach (var element in (IEnumerable) value) {
                    BsonSerializer.Serialize(bsonWriter, typeof(object), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion
    }

    public class QueueSerializer : BsonBaseSerializer {
        #region private static fields
        private static QueueSerializer singleton = new QueueSerializer();
        #endregion

        #region constructors
        private QueueSerializer() {
        }
        #endregion

        #region public static properties
        public static QueueSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Queue), singleton);
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
                var queue = new Queue();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = serializer.Deserialize(bsonReader, typeof(object), elementType, null);
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
                foreach (var element in (Queue) value) {
                    BsonSerializer.Serialize(bsonWriter, typeof(object), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion
    }

    public class StackSerializer : BsonBaseSerializer {
        #region private static fields
        private static StackSerializer singleton = new StackSerializer();
        #endregion

        #region constructors
        private StackSerializer() {
        }
        #endregion

        #region public static properties
        public static StackSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Stack), singleton);
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
                var stack = new Stack();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = serializer.Deserialize(bsonReader, typeof(object), elementType, null);
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
                var outputOrder = new ArrayList((Stack) value); // serialize first pushed item first (reverse of enumerator order)
                outputOrder.Reverse();
                foreach (var element in outputOrder) {
                    BsonSerializer.Serialize(bsonWriter, typeof(object),  element);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion
    }
}
