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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

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

            var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(Image));
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

    /// <summary>
    /// Represents a serializer for Bitmaps.
    /// </summary>
    public class BitmapSerializer : BsonBaseSerializer
    {
        // private static fields
        private static BitmapSerializer __instance = new BitmapSerializer();

        // static constructor
        static BitmapSerializer()
        {
            BsonDefaultSerializer.RegisterDiscriminator(typeof(Bitmap), "Bitmap");
        }

        // constructors
        /// <summary>
        /// Initializes a new instance of the BitmapSerializer class.
        /// </summary>
        public BitmapSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the BitmapSerializer class.
        /// </summary>
        public static BitmapSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an Bitmap from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the Bitmap.</param>
        /// <param name="actualType">The actual type of the Bitmap.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>A Bitmap.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            if (nominalType != typeof(Image) && nominalType != typeof(Bitmap))
            {
                var message = string.Format("Nominal type must be Image or Bitmap, not {0}.", nominalType.FullName);
                throw new ArgumentException(message, "nominalType");
            }

            if (actualType != typeof(Bitmap))
            {
                var message = string.Format("Actual type must be Bitmap, not {0}.", actualType.FullName);
                throw new ArgumentException(message, "actualType");
            }

            var bsonType = bsonReader.GetCurrentBsonType();
            byte[] bytes;
            BsonBinarySubType subType;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.Binary:
                    bsonReader.ReadBinaryData(out bytes, out subType);
                    break;

                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t");
                    bsonReader.ReadBinaryData("bitmap", out bytes, out subType);
                    bsonReader.ReadEndDocument();
                    break;

                default:
                    var message = string.Format("BsonType must be Null, Binary or Document, not {0}.", bsonType);
                    throw new FileFormatException(message);
            }

            if (subType != BsonBinarySubType.Binary)
            {
                var message = string.Format("Binary sub type must be Binary, not {0}.", subType);
                throw new FileFormatException(message);
            }

            var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }

        /// <summary>
        /// Serializes a Bitmap to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The Bitmap.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            if (nominalType != typeof(Image) && nominalType != typeof(Bitmap))
            {
                var message = string.Format("Nominal type must be Image or Bitmap, not {0}.", nominalType.FullName);
                throw new ArgumentException(message, "nominalType");
            }

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var actualType = value.GetType();
                if (actualType != typeof(Bitmap))
                {
                    var message = string.Format("Actual type must be Bitmap, not {0}.", actualType.FullName);
                    throw new ArgumentException(message, "actualType");
                }

                var bitmap = (Bitmap)value;
                var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Bmp);
                var bytes = stream.ToArray();

                if (nominalType == typeof(Image))
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", "Bitmap");
                    bsonWriter.WriteBinaryData("bitmap", bytes, BsonBinarySubType.Binary);
                    bsonWriter.WriteEndDocument();
                }
                else
                {
                    bsonWriter.WriteBinaryData(bytes, BsonBinarySubType.Binary);
                }
            }
        }
    }
}
