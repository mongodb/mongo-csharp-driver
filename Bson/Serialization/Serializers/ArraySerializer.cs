﻿/* Copyright 2010-2012 10gen Inc.
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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for one-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class ArraySerializer<T> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the ArraySerializer class.
        /// </summary>
        public ArraySerializer()
        {
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(T[]));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var list = new List<T>();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var element = BsonSerializer.Deserialize<T>(bsonReader);
                        list.Add(element);
                    }
                    bsonReader.ReadEndArray();
                    return list.ToArray();
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t"); // skip over discriminator
                    bsonReader.ReadName("_v");
                    var value = Deserialize(bsonReader, actualType, actualType, options);
                    bsonReader.ReadEndDocument();
                    return value;
                default:
                    var message = string.Format("Can't deserialize a {0} from BsonType {1}.", actualType.FullName, bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public override BsonSerializationInfo GetItemSerializationInfo()
        {
            string elementName = null;
            var serializer = BsonSerializer.LookupSerializer(typeof(T));
            var nominalType = typeof(T);
            IBsonSerializationOptions serializationOptions = null;
            return new BsonSerializationInfo(elementName, serializer, nominalType, serializationOptions);
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
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                VerifyTypes(nominalType, actualType, typeof(T[]));

                if (nominalType != typeof(object))
                {
                    bsonWriter.WriteStartArray();
                    var array = (T[])value;
                    for (int index = 0; index < array.Length; index++)
                    {
                        BsonSerializer.Serialize(bsonWriter, typeof(T), array[index]);
                    }
                    bsonWriter.WriteEndArray();
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", BsonClassMap.GetTypeNameDiscriminator(actualType));
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options);
                    bsonWriter.WriteEndDocument();
                }
            }
        }
    }

    /// <summary>
    /// Represents a serializer for two-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class TwoDimensionalArraySerializer<T> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the TwoDimensionalArraySerializer class.
        /// </summary>
        public TwoDimensionalArraySerializer()
        {
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(T[,]));

            var bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var outerList = new List<List<T>>();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        bsonReader.ReadStartArray();
                        var innerList = new List<T>();
                        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                        {
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
                    for (int i = 0; i < length1; i++)
                    {
                        var innerList = outerList[i];
                        if (innerList.Count != length2)
                        {
                            message = string.Format("Inner list {0} is of length {1} but should be of length {2}.", i, innerList.Count, length2);
                            throw new FileFormatException(message);
                        }
                        for (int j = 0; j < length2; j++)
                        {
                            array[i, j] = innerList[j];
                        }
                    }

                    return array;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t"); // skip over discriminator
                    bsonReader.ReadName("_v");
                    var value = Deserialize(bsonReader, actualType, actualType, options);
                    bsonReader.ReadEndDocument();
                    return value;
                default:
                    message = string.Format("Can't deserialize a {0} from BsonType {1}.", actualType.FullName, bsonType);
                    throw new FileFormatException(message);
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
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                VerifyTypes(nominalType, actualType, typeof(T[,]));

                if (nominalType != typeof(object))
                {
                    bsonWriter.WriteStartArray();
                    var array = (T[,])value;
                    var length1 = array.GetLength(0);
                    var length2 = array.GetLength(1);
                    for (int i = 0; i < length1; i++)
                    {
                        bsonWriter.WriteStartArray();
                        for (int j = 0; j < length2; j++)
                        {
                            BsonSerializer.Serialize(bsonWriter, typeof(T), array[i, j]);
                        }
                        bsonWriter.WriteEndArray();
                    }
                    bsonWriter.WriteEndArray();
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", BsonClassMap.GetTypeNameDiscriminator(actualType));
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options);
                    bsonWriter.WriteEndDocument();
                }
            }
        }
    }

    /// <summary>
    /// Represents a serializer for three-dimensional arrays.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class ThreeDimensionalArraySerializer<T> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the ThreeDimensionalArraySerializer class.
        /// </summary>
        public ThreeDimensionalArraySerializer()
        {
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(T[, ,]));

            var bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var outerList = new List<List<List<T>>>();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        bsonReader.ReadStartArray();
                        var middleList = new List<List<T>>();
                        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                        {
                            bsonReader.ReadStartArray();
                            var innerList = new List<T>();
                            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                            {
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
                    for (int i = 0; i < length1; i++)
                    {
                        var middleList = outerList[i];
                        if (middleList.Count != length2)
                        {
                            message = string.Format("Middle list {0} is of length {1} but should be of length {2}.", i, middleList.Count, length2);
                            throw new FileFormatException(message);
                        }
                        for (int j = 0; j < length2; j++)
                        {
                            var innerList = middleList[j];
                            if (innerList.Count != length3)
                            {
                                message = string.Format("Inner list {0} is of length {1} but should be of length {2}.", j, innerList.Count, length3);
                                throw new FileFormatException(message);
                            }
                            for (int k = 0; k < length3; k++)
                            {
                                array[i, j, k] = innerList[k];
                            }
                        }
                    }

                    return array;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t"); // skip over discriminator
                    bsonReader.ReadName("_v");
                    var value = Deserialize(bsonReader, actualType, actualType, options);
                    bsonReader.ReadEndDocument();
                    return value;
                default:
                    message = string.Format("Can't deserialize a {0} from BsonType {1}.", actualType.FullName, bsonType);
                    throw new FileFormatException(message);
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
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                VerifyTypes(nominalType, actualType, typeof(T[, ,]));

                if (nominalType != typeof(object))
                {
                    bsonWriter.WriteStartArray();
                    var array = (T[, ,])value;
                    var length1 = array.GetLength(0);
                    var length2 = array.GetLength(1);
                    var length3 = array.GetLength(2);
                    for (int i = 0; i < length1; i++)
                    {
                        bsonWriter.WriteStartArray();
                        for (int j = 0; j < length2; j++)
                        {
                            bsonWriter.WriteStartArray();
                            for (int k = 0; k < length3; k++)
                            {
                                BsonSerializer.Serialize(bsonWriter, typeof(T), array[i, j, k]);
                            }
                            bsonWriter.WriteEndArray();
                        }
                        bsonWriter.WriteEndArray();
                    }
                    bsonWriter.WriteEndArray();
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", BsonClassMap.GetTypeNameDiscriminator(actualType));
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options);
                    bsonWriter.WriteEndDocument();
                }
            }
        }
    }
}
