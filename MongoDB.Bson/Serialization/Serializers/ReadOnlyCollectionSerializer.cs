/* Copyright 2010-2013 10gen Inc.
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
using System.Collections.ObjectModel;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for ReadOnlyCollections.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public class ReadOnlyCollectionSerializer<T> : BsonBaseSerializer, IBsonArraySerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the ReadOnlyCollectionSerializer class.
        /// </summary>
        public ReadOnlyCollectionSerializer()
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
                    var list = new List<T>();
                    var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(T));
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var elementType = discriminatorConvention.GetActualType(bsonReader, typeof(T));
                        var serializer = BsonSerializer.LookupSerializer(elementType);
                        var element = (T)serializer.Deserialize(bsonReader, typeof(T), elementType, itemSerializationOptions);
                        list.Add(element);
                    }
                    bsonReader.ReadEndArray();
                    return CreateInstance(actualType, list);
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
                var actualType = value.GetType();
                if (actualType != nominalType)
                {
                    string discriminator;
                    if (nominalType == typeof(object))
                    {
                        discriminator = TypeNameDiscriminator.GetDiscriminator(actualType);
                    }
                    else
                    {
                        discriminator = actualType.Name;
                    }

                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", discriminator);
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options);
                    bsonWriter.WriteEndDocument();
                    return;
                }

                var items = (ReadOnlyCollection<T>)value;
                var arraySerializationOptions = EnsureSerializationOptions<ArraySerializationOptions>(options);
                var itemSerializationOptions = arraySerializationOptions.ItemSerializationOptions;

                bsonWriter.WriteStartArray();
                foreach (var item in items)
                {
                    BsonSerializer.Serialize(bsonWriter, typeof(T), item, itemSerializationOptions);
                }
                bsonWriter.WriteEndArray();
            }
        }

        // private methods
        private ReadOnlyCollection<T> CreateInstance(Type type, IList<T> list)
        {
            if (type == typeof(ReadOnlyCollection<T>))
            {
                return new ReadOnlyCollection<T>(list);
            }
            else if (typeof(ReadOnlyCollection<T>).IsAssignableFrom(type))
            {
                return (ReadOnlyCollection<T>)Activator.CreateInstance(type, list);
            }

            var message = string.Format("ReadOnlyCollectionSerializer<{0}> can't be used with type {1}.", BsonUtils.GetFriendlyTypeName(typeof(T)), BsonUtils.GetFriendlyTypeName(type));
            throw new BsonSerializationException(message);
        }
    }
}
