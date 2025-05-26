/* Copyright 2010-present MongoDB Inc.
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
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson
{
    /// <summary>
    /// A static class containing BSON extension methods.
    /// </summary>
    public static class BsonExtensionMethods
    {
        /// <summary>
        /// Serializes an object to a BSON byte array.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="estimatedBsonSize">The estimated size of the serialized object</param>
        /// <returns>A BSON byte array.</returns>
        public static byte[] ToBson<TNominalType>(
            this TNominalType obj,
            IBsonSerializer<TNominalType> serializer = null,
            BsonBinaryWriterSettings writerSettings = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default(BsonSerializationArgs),
            int estimatedBsonSize = 0)
        {
            args.SetOrValidateNominalType(typeof(TNominalType), "<TNominalType>");

            return ToBson(obj, typeof(TNominalType), writerSettings, serializer, configurator, args, estimatedBsonSize);
        }

        /// <summary>
        /// Serializes an object to a BSON byte array.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="nominalType">The nominal type of the object..</param>
        /// <param name="writerSettings">The writer settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="estimatedBsonSize">The estimated size of the serialized object.</param>
        /// <returns>A BSON byte array.</returns>
        /// <exception cref="System.ArgumentNullException">nominalType</exception>
        /// <exception cref="System.ArgumentException">serializer</exception>
        public static byte[] ToBson(
            this object obj,
            Type nominalType,
            BsonBinaryWriterSettings writerSettings = null,
            IBsonSerializer serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default,
            int estimatedBsonSize = 0) => ToBson(obj, nominalType, BsonSerializer.DefaultSerializationDomain, writerSettings,
            serializer, configurator, args, estimatedBsonSize);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nominalType"></param>
        /// <param name="domain"></param>
        /// <param name="writerSettings"></param>
        /// <param name="serializer"></param>
        /// <param name="configurator"></param>
        /// <param name="args"></param>
        /// <param name="estimatedBsonSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static byte[] ToBson(
            this object obj,
            Type nominalType,
            IBsonSerializationDomain domain,
            BsonBinaryWriterSettings writerSettings = null,
            IBsonSerializer serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default(BsonSerializationArgs),
            int estimatedBsonSize = 0)
        {
            if (estimatedBsonSize < 0)
            {
                throw new ArgumentException("Value cannot be negative.", nameof(estimatedBsonSize));
            }

            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            args.SetOrValidateNominalType(nominalType, "nominalType");

            if (serializer == null)
            {
                serializer = domain.LookupSerializer(nominalType);
            }
            if (serializer.ValueType != nominalType)
            {
                var message = string.Format("Serializer type {0} value type does not match document types {1}.", serializer.GetType().FullName, nominalType.FullName);
                throw new ArgumentException(message, "serializer");
            }

            using (var memoryStream = new MemoryStream(estimatedBsonSize))
            {
                using (var bsonWriter = new BsonBinaryWriter(memoryStream, writerSettings ?? BsonBinaryWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot(bsonWriter, configurator);
                    serializer.Serialize(context, args, obj);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Serializes an object to a BsonDocument.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        /// <returns>A BsonDocument.</returns>
        public static BsonDocument ToBsonDocument<TNominalType>(
            this TNominalType obj,
            IBsonSerializer<TNominalType> serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default(BsonSerializationArgs))
        {
            args.SetOrValidateNominalType(typeof(TNominalType), "<TNominalType>");
            return ToBsonDocument(obj, typeof(TNominalType), serializer, configurator, args);
        }

        /// <summary>
        /// Serializes an object to a BsonDocument.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        /// <returns>A BsonDocument.</returns>
        /// <exception cref="System.ArgumentNullException">nominalType</exception>
        /// <exception cref="System.ArgumentException">serializer</exception>
        public static BsonDocument ToBsonDocument(
            this object obj,
            Type nominalType,
            IBsonSerializer serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default) => ToBsonDocument(obj, nominalType,
            BsonSerializer.DefaultSerializationDomain, serializer, configurator, args);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nominalType"></param>
        /// <param name="domain"></param>
        /// <param name="serializer"></param>
        /// <param name="configurator"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static BsonDocument ToBsonDocument(
            this object obj,
            Type nominalType,
            IBsonSerializationDomain domain,
            IBsonSerializer serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default)
        {
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            args.SetOrValidateNominalType(nominalType, "nominalType");

            if (obj == null)
            {
                return null;
            }

            if (serializer == null)
            {
                var bsonDocument = obj as BsonDocument;
                if (bsonDocument != null)
                {
                    return bsonDocument; // it's already a BsonDocument
                }

                var convertibleToBsonDocument = obj as IConvertibleToBsonDocument;
                if (convertibleToBsonDocument != null)
                {
                    return convertibleToBsonDocument.ToBsonDocument(); // use the provided ToBsonDocument method
                }

                serializer = domain.LookupSerializer(nominalType);
            }
            if (serializer.ValueType != nominalType)
            {
                var message = string.Format("Serializer type {0} value type does not match document types {1}.", serializer.GetType().FullName, nominalType.FullName);
                throw new ArgumentException(message, "serializer");
            }

            // otherwise serialize into a new BsonDocument
            var document = new BsonDocument();
            using (var bsonWriter = new BsonDocumentWriter(document))
            {
                var context = BsonSerializationContext.CreateRoot(bsonWriter, configurator);
                serializer.Serialize(context, args, obj);
            }
            return document;
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="writerSettings">The JsonWriter settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        /// <returns>
        /// A JSON string.
        /// </returns>
        public static string ToJson<TNominalType>(
            this TNominalType obj,
            JsonWriterSettings writerSettings = null,
            IBsonSerializer<TNominalType> serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default(BsonSerializationArgs))
        {
            args.SetOrValidateNominalType(typeof(TNominalType), "<TNominalType>");
            return ToJson(obj, typeof(TNominalType), writerSettings, serializer, configurator, args);
        }

        /// <summary>
        /// Serializes an object to a JSON string.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="nominalType">The nominal type of the objectt.</param>
        /// <param name="writerSettings">The JsonWriter settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        /// <returns>
        /// A JSON string.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">nominalType</exception>
        /// <exception cref="System.ArgumentException">serializer</exception>
        public static string ToJson(
            this object obj,
            Type nominalType,
            JsonWriterSettings writerSettings = null,
            IBsonSerializer serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default)
            => ToJson(obj, nominalType, BsonSerializer.DefaultSerializationDomain, writerSettings, serializer, configurator, args);

        /// <summary>
        /// //TODO
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nominalType"></param>
        /// <param name="domain"></param>
        /// <param name="writerSettings"></param>
        /// <param name="serializer"></param>
        /// <param name="configurator"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static string ToJson(
            this object obj,
            Type nominalType,
            IBsonSerializationDomain domain,
            JsonWriterSettings writerSettings = null,
            IBsonSerializer serializer = null,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default(BsonSerializationArgs))
        {
            if (nominalType == null)
            {
                throw new ArgumentNullException("nominalType");
            }
            args.SetOrValidateNominalType(nominalType, "nominalType");

            if (serializer == null)
            {
                serializer = domain.LookupSerializer(nominalType);
            }
            if (serializer.ValueType != nominalType)
            {
                var message = string.Format("Serializer type {0} value type does not match document types {1}.", serializer.GetType().FullName, nominalType.FullName);
                throw new ArgumentException(message, "serializer");
            }

            using (var stringWriter = new StringWriter())
            {
                using (var bsonWriter = new JsonWriter(stringWriter, writerSettings ?? JsonWriterSettings.Defaults))
                {
                    var context = BsonSerializationContext.CreateRoot(bsonWriter, configurator);
                    serializer.Serialize(context, args, obj);
                }
                return stringWriter.ToString();
            }
        }
    }
}
