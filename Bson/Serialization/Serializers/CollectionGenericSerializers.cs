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
    /// Represents a serializer for enumerable values.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class EnumerableSerializer<T> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the EnumerableSerializer class.
        /// </summary>
        public EnumerableSerializer()
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
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else if (bsonType == BsonType.Array)
            {
                bsonReader.ReadStartArray();
                var list = (nominalType == typeof(List<T>) || nominalType.IsInterface) ? new List<T>() : (ICollection<T>)Activator.CreateInstance(nominalType);
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(T));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = (T)serializer.Deserialize(bsonReader, typeof(T), elementType, null);
                    list.Add(element);
                }
                bsonReader.ReadEndArray();
                return list;
            }
            else
            {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
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
                bsonWriter.WriteStartArray();
                foreach (var element in (IEnumerable<T>)value)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Queues.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class QueueSerializer<T> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the QueueSerializer class.
        /// </summary>
        public QueueSerializer()
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
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else if (bsonType == BsonType.Array)
            {
                bsonReader.ReadStartArray();
                var queue = new Queue<T>();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(T));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = (T)serializer.Deserialize(bsonReader, typeof(T), elementType, null);
                    queue.Enqueue(element);
                }
                bsonReader.ReadEndArray();
                return queue;
            }
            else
            {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
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
                bsonWriter.WriteStartArray();
                foreach (var element in (Queue<T>)value)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }

    /// <summary>
    /// Represents a serializer for Stacks.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class StackSerializer<T> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the StackSerializer class.
        /// </summary>
        public StackSerializer()
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
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else if (bsonType == BsonType.Array)
            {
                bsonReader.ReadStartArray();
                var stack = new Stack<T>();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(T));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                    var serializer = BsonSerializer.LookupSerializer(elementType);
                    var element = (T)serializer.Deserialize(bsonReader, typeof(T), elementType, null);
                    stack.Push(element);
                }
                bsonReader.ReadEndArray();
                return stack;
            }
            else
            {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
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
                bsonWriter.WriteStartArray();
                var outputOrder = new List<T>((Stack<T>)value); // serialize first pushed item first (reverse of enumerator order)
                outputOrder.Reverse();
                foreach (var element in outputOrder)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), element);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }
}
