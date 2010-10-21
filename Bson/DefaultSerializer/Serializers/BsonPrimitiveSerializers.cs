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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public class BooleanSerializer : BsonBaseSerializer {
        #region private static fields
        private static BooleanSerializer singleton = new BooleanSerializer();
        #endregion

        #region constructors
        private BooleanSerializer() {
        }
        #endregion

        #region public static properties
        public static BooleanSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(bool), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadBoolean(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            bsonWriter.WriteBoolean(name, (bool) value);
        }
        #endregion
    }

    public class DateTimeSerializer : BsonBaseSerializer {
        #region private static fields
        private static DateTimeSerializer singleton = new DateTimeSerializer();
        #endregion

        #region constructors
        private DateTimeSerializer() {
        }
        #endregion

        #region public static properties
        public static DateTimeSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(DateTime), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadDateTime(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            bsonWriter.WriteDateTime(name, (DateTime) value);
        }
        #endregion
    }

    public class DoubleSerializer : BsonBaseSerializer {
        #region private static fields
        private static DoubleSerializer singleton = new DoubleSerializer();
        #endregion

        #region constructors
        private DoubleSerializer() {
        }
        #endregion

        #region public static properties
        public static DoubleSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(double), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadDouble(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            bsonWriter.WriteDouble(name, (double) value);
        }
        #endregion
    }

    public class GuidSerializer : BsonBaseSerializer {
        #region private static fields
        private static GuidSerializer singleton = new GuidSerializer();
        #endregion

        #region constructors
        private GuidSerializer() {
        }
        #endregion

        #region public static properties
        public static GuidSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(Guid), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            byte[] bytes;
            BsonBinarySubType subType;
            bsonReader.ReadBinaryData(out name, out bytes, out subType);
            if (bytes.Length != 16) {
                throw new FileFormatException("BinaryData length is not 16");
            }
            if (subType != BsonBinarySubType.Uuid) {
                throw new FileFormatException("BinaryData sub type is not Uuid");
            }
            return new Guid(bytes);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (Guid) obj;
            bsonWriter.WriteBinaryData(name, value.ToByteArray(), BsonBinarySubType.Uuid);
        }
        #endregion
    }

    public class Int32Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int32Serializer singleton = new Int32Serializer();
        #endregion

        #region constructors
        private Int32Serializer() {
        }
        #endregion

        #region public static properties
        public static Int32Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(int), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadInt32(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            bsonWriter.WriteInt32(name, (int) value);
        }
        #endregion
    }

    public class Int64Serializer : BsonBaseSerializer {
        #region private static fields
        private static Int64Serializer singleton = new Int64Serializer();
        #endregion

        #region constructors
        private Int64Serializer() {
        }
        #endregion

        #region public static properties
        public static Int64Serializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(long), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            return bsonReader.ReadInt64(out name);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            bsonWriter.WriteInt64(name, (long) value);
        }
        #endregion
    }

    public class ObjectIdSerializer : BsonBaseSerializer {
        #region private static fields
        private static ObjectIdSerializer singleton = new ObjectIdSerializer();
        #endregion

        #region constructors
        private ObjectIdSerializer() {
        }
        #endregion

        #region public static properties
        public static ObjectIdSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(ObjectId), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            int timestamp;
            long machinePidIncrement;
            bsonReader.ReadObjectId(out name, out timestamp, out machinePidIncrement);
            return new ObjectId(timestamp, machinePidIncrement);
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object obj,
            bool useCompactRepresentation
        ) {
            var value = (ObjectId) obj;
            bsonWriter.WriteObjectId(name, value.Timestamp, value.MachinePidIncrement);
        }
        #endregion
    }

    public class StringSerializer : BsonBaseSerializer {
        #region private static fields
        private static StringSerializer singleton = new StringSerializer();
        #endregion

        #region constructors
        private StringSerializer() {
        }
        #endregion

        #region public static properties
        public static StringSerializer Singleton {
            get { return singleton; }
        }
        #endregion

        #region public static methods
        public static void RegisterSerializers() {
            BsonSerializer.RegisterSerializer(typeof(string), singleton);
        }
        #endregion

        #region public methods
        public override object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                return bsonReader.ReadString(out name);
            }
        }

        public override void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            if (value == null) {
                bsonWriter.WriteNull(name);
            } else {
                bsonWriter.WriteString(name, (string) value);
            }
        }
        #endregion
    }
}
