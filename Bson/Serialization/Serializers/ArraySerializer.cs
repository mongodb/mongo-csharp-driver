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

namespace MongoDB.Bson.Serialization.Serializers {
    /// <summary>
    /// Represents a serializer for one-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class ArraySerializer<T> : BsonBaseSerializer {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the ArraySerializer class.
        /// </summary>
        public ArraySerializer() {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            VerifyType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartArray();
                List<T> list = new List<T>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var element = BsonSerializer.Deserialize<T>(bsonReader);
                    list.Add(element);
                }
                bsonReader.ReadEndArray();
                return list.ToArray();
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                VerifyType(value.GetType());
                var array = (T[]) value;
                bsonWriter.WriteStartArray();
                for (int index = 0; index < array.Length; index++) {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), array[index]);
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion

        #region private methods
        private void VerifyType(
            Type type
        ) {
            if (type != typeof(T[])) {
                var message = string.Format("ArraySerializer<{0}> cannot be used with type {1}.", typeof(T).FullName, type.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for two-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class TwoDimensionalArraySerializer<T> : BsonBaseSerializer {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the TwoDimensionalArraySerializer class.
        /// </summary>
        public TwoDimensionalArraySerializer() {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            VerifyType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartArray();
                var outerList = new List<List<T>>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    bsonReader.ReadStartArray();
                    var innerList = new List<T>();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                        var element = BsonSerializer.Deserialize<T>(bsonReader);
                        innerList.Add(element);
                    }
                    bsonReader.ReadEndArray();
                    outerList.Add(innerList);
                }
                bsonReader.ReadEndArray();

                var length1 = outerList.Count;
                var length2 = (length1 == 0) ? 0 : outerList[0].Count;
                var array = new T[length1, length2];
                for (int i = 0; i < length1; i++) {
                    var innerList = outerList[i];
                    if (innerList.Count != length2) {
                        var message = string.Format("Inner list {0} is of length {1} but should be of length {2}.", i, innerList.Count, length2);
                        throw new FileFormatException(message);
                    }
                    for (int j = 0; j < length2; j++) {
                        array[i, j] = innerList[j];
                    }
                }

                return array;
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                VerifyType(value.GetType());
                var array = (T[,]) value;
                bsonWriter.WriteStartArray();
                var length1 = array.GetLength(0);
                var length2 = array.GetLength(1);
                for (int i = 0; i < length1; i++) {
                    bsonWriter.WriteStartArray();
                    for (int j = 0; j < length2; j++) {
                        BsonSerializer.Serialize(bsonWriter, typeof(T), array[i, j]);
                    }
                    bsonWriter.WriteEndArray();
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion

        #region private methods
        private void VerifyType(
            Type type
        ) {
            if (type != typeof(T[,])) {
                var message = string.Format("TwoDimensionalArraySerializer<{0}> cannot be used with type {1}.", typeof(T).FullName, type.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }

    /// <summary>
    /// Represents a serializer for three-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class ThreeDimensionalArraySerializer<T> : BsonBaseSerializer {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the ThreeDimensionalArraySerializer class.
        /// </summary>
        public ThreeDimensionalArraySerializer() {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            VerifyType(nominalType);
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartArray();
                var outerList = new List<List<List<T>>>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    bsonReader.ReadStartArray();
                    var middleList = new List<List<T>>();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                        bsonReader.ReadStartArray();
                        var innerList = new List<T>();
                        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                            var element = BsonSerializer.Deserialize<T>(bsonReader);
                            innerList.Add(element);
                        }
                        bsonReader.ReadEndArray();
                        middleList.Add(innerList);
                    }
                    bsonReader.ReadEndArray();
                    outerList.Add(middleList);
                }
                bsonReader.ReadEndArray();

                var length1 = outerList.Count;
                var length2 = (length1 == 0) ? 0 : outerList[0].Count;
                var length3 = (length2 == 0) ? 0 : outerList[0][0].Count;
                var array = new T[length1, length2, length3];
                for (int i = 0; i < length1; i++) {
                    var middleList = outerList[i];
                    if (middleList.Count != length2) {
                        var message = string.Format("Middle list {0} is of length {1} but should be of length {2}.", i, middleList.Count, length2);
                        throw new FileFormatException(message);
                    }
                    for (int j = 0; j < length2; j++) {
                        var innerList = middleList[j];
                        if (innerList.Count != length3) {
                            var message = string.Format("Inner list {0} is of length {1} but should be of length {2}.", j, innerList.Count, length3);
                            throw new FileFormatException(message);
                        }
                        for (int k = 0; k < length3; k++) {
                            array[i, j, k] = innerList[k];
                        }
                    }
                }

                return array;
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                VerifyType(value.GetType());
                var array = (T[,,]) value;
                bsonWriter.WriteStartArray();
                var length1 = array.GetLength(0);
                var length2 = array.GetLength(1);
                var length3 = array.GetLength(2);
                for (int i = 0; i < length1; i++) {
                    bsonWriter.WriteStartArray();
                    for (int j = 0; j < length2; j++) {
                        bsonWriter.WriteStartArray();
                        for (int k = 0; k < length3; k++) {
                            BsonSerializer.Serialize(bsonWriter, typeof(T), array[i, j, k]);
                        }
                        bsonWriter.WriteEndArray();
                    }
                    bsonWriter.WriteEndArray();
                }
                bsonWriter.WriteEndArray();
            }
        }
        #endregion

        #region private methods
        private void VerifyType(
            Type type
        ) {
            if (type != typeof(T[,,])) {
                var message = string.Format("ThreeDimensionalArraySerializer<{0}> cannot be used with type {1}.", typeof(T).FullName, type.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
