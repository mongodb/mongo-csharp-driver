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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Images.
    /// </summary>
    public class ImageSerializer : BsonBaseSerializer
    {
        // private static fields
        private static ImageSerializer __instance = new ImageSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the ImageSerializer class.
        /// </summary>
        public ImageSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the ImageSerializer class.
        /// </summary>
        [Obsolete("Use constructor instead.")]
        public static ImageSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an Image from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the Image.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An Image.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options)
        {
            if (nominalType != typeof(Image))
            {
                var message = string.Format("Nominal type must be Image, not {0}.", nominalType.FullName);
                throw new ArgumentException(message, "nominalType");
            }

            var discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(typeof(Image));
            var actualType = discriminatorConvention.GetActualType(bsonReader, typeof(Image));
            if (actualType == typeof(Image))
            {
                var message = string.Format("Unable to determine actual type of Image to deserialize.");
                throw new FileFormatException(message);
            }

            var serializer = BsonSerializer.LookupSerializer(actualType);
            return serializer.Deserialize(bsonReader, nominalType, actualType, options);
        }

        /// <summary>
        /// Deserializes an Image from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the Image.</param>
        /// <param name="actualType">The actual type of the Image.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An Image.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            if (nominalType != typeof(Image))
            {
                var message = string.Format("Nominal type must be Image, not {0}.", nominalType.FullName);
                throw new ArgumentException(message, "nominalType");
            }

            if (actualType != typeof(Image))
            {
                var message = string.Format("Actual type must be Image, not {0}.", actualType.FullName);
                throw new ArgumentException(message, "actualType");
            }

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                var message = string.Format("BsonType must be Null, not {0}.", bsonType);
                throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an Image to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The Image.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            if (nominalType != typeof(Image))
            {
                var message = string.Format("Nominal type must be Image, not {0}.", nominalType.FullName);
                throw new ArgumentException(message, "nominalType");
            }

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var message = string.Format("Value must be null.");
                throw new ArgumentException(message, "value");
            }
        }
    }
}
