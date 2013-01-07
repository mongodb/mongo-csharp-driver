﻿/* Copyright 2010-2013 10gen Inc.
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
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Stacks.
    /// </summary>
    public class StackSerializer : BsonBaseSerializer, IBsonArraySerializer
    {
        // private static fields
        private static StackSerializer __instance = new StackSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the StackSerializer class.
        /// </summary>
        public StackSerializer()
            : base(new ArraySerializationOptions())
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the StackSerializer class.
        /// </summary>
        [Obsolete("Use constructor instead.")]
        public static StackSerializer Instance
        {
            get { return __instance; }
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
            var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
            var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var stack = new Stack();
                    var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                        var serializer = BsonSerializer.LookupSerializer(elementType);
                        var element = serializer.Deserialize(bsonReader, typeof(object), elementType, itemSerializationOptions);
                        stack.Push(element);
                    }
                    bsonReader.ReadEndArray();
                    return stack;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t"); // skip over discriminator
                    bsonReader.ReadName("_v");
                    var value = Deserialize(bsonReader, actualType, actualType, options);
                    bsonReader.ReadEndDocument();
                    return value;
                default:
                    var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public BsonSerializationInfo GetItemSerializationInfo()
        {
            string elementName = null;
            var serializer = BsonSerializer.LookupSerializer(typeof(object));
            var nominalType = typeof(object);
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
                if (nominalType == typeof(object))
                {
                    var actualType = value.GetType();
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", TypeNameDiscriminator.GetDiscriminator(actualType));
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options);
                    bsonWriter.WriteEndDocument();
                    return;
                }

                var items = ((Stack)value).ToArray(); // convert to array to allow efficient access in reverse order
                var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
                var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

                // serialize first pushed item first (reverse of enumeration order)
                bsonWriter.WriteStartArray();
                for (var i = items.Length - 1; i >= 0; i--)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(object), items[i], itemSerializationOptions);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Stacks.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class StackSerializer<T> : BsonBaseSerializer, IBsonArraySerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the StackSerializer class.
        /// </summary>
        public StackSerializer()
            : base(new ArraySerializationOptions())
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
            var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
            var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    var stack = new Stack<T>();
                    var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(T));
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                        var serializer = BsonSerializer.LookupSerializer(elementType);
                        var element = (T)serializer.Deserialize(bsonReader, typeof(T), elementType, itemSerializationOptions);
                        stack.Push(element);
                    }
                    bsonReader.ReadEndArray();
                    return stack;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t"); // skip over discriminator
                    bsonReader.ReadName("_v");
                    var value = Deserialize(bsonReader, actualType, actualType, options);
                    bsonReader.ReadEndDocument();
                    return value;
                default:
                    var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public BsonSerializationInfo GetItemSerializationInfo()
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
                if (nominalType == typeof(object))
                {
                    var actualType = value.GetType();
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", TypeNameDiscriminator.GetDiscriminator(actualType));
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options);
                    bsonWriter.WriteEndDocument();
                    return;
                }

                var items = ((Stack<T>)value).ToArray(); // convert to array to allow efficient access in reverse order
                var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
                var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

                // serialize first pushed item first (reverse of enumeration order)
                bsonWriter.WriteStartArray();
                for (var i = items.Length - 1; i >= 0; i--)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), items[i], itemSerializationOptions);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }
}
