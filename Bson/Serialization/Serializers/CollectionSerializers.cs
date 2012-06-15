/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for enumerable values.
    /// </summary>
    public class EnumerableSerializer : BsonBaseSerializer, IBsonArraySerializer
    {
        // private static fields
        private static EnumerableSerializer __instance = new EnumerableSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the EnumerableSerializer class.
        /// </summary>
        public EnumerableSerializer()
            : base(new ArraySerializationOptions())
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the EnumerableSerializer class.
        /// </summary>
        public static EnumerableSerializer Instance
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
                    var collection = CreateInstance(actualType);
                    var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                        var serializer = BsonSerializer.LookupSerializer(elementType);
                        var element = serializer.Deserialize(bsonReader, typeof(object), elementType, itemSerializationOptions);
                        collection.Add(element);
                    }
                    bsonReader.ReadEndArray();
                    return collection;
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

                var items = (IEnumerable)value;
                var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
                var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

                bsonWriter.WriteStartArray();
                foreach (var item in items)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(object), item, itemSerializationOptions);
                }
                bsonWriter.WriteEndArray();
            }
        }

        // private methods
        private IList CreateInstance(Type type)
        {
            string message;

            if (type.IsInterface)
            {
                // in the case of an interface pick a reasonable class that implements that interface
                if (type == typeof(IEnumerable) || type == typeof(ICollection) || type == typeof(IList))
                {
                    return new ArrayList();
                }
            }
            else
            {
                if (type == typeof(ArrayList))
                {
                    return new ArrayList();
                }
                else if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    var instance = Activator.CreateInstance(type);
                    var list = instance as IList;
                    if (list == null)
                    {
                        message = string.Format("Enumerable class {0} does not implement IList so it can't be deserialized.", BsonUtils.GetFriendlyTypeName(type));
                        throw new BsonSerializationException(message);
                    }
                    return list;
                }
            }

            message = string.Format("EnumerableSerializer can't be used with type {0}.", BsonUtils.GetFriendlyTypeName(type));
            throw new BsonSerializationException(message);
        }
    }

    /// <summary>
    /// Represents a serializer for Queues.
    /// </summary>
    public class QueueSerializer : BsonBaseSerializer, IBsonArraySerializer
    {
        // private static fields
        private static QueueSerializer __instance = new QueueSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the QueueSerializer class.
        /// </summary>
        public QueueSerializer()
            : base(new ArraySerializationOptions())
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the QueueSerializer class.
        /// </summary>
        public static QueueSerializer Instance
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
                    var queue = new Queue();
                    var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(object));
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                        var serializer = BsonSerializer.LookupSerializer(elementType);
                        var element = serializer.Deserialize(bsonReader, typeof(object), elementType, itemSerializationOptions);
                        queue.Enqueue(element);
                    }
                    bsonReader.ReadEndArray();
                    return queue;
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

                var items = (Queue)value;
                var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
                var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

                bsonWriter.WriteStartArray();
                foreach (var item in items)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(object), item, itemSerializationOptions);
                }
                bsonWriter.WriteEndArray();
            }
        }
    }

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
}
