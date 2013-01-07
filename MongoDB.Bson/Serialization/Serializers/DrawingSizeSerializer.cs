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
using System.IO;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for System.Drawing.Size.
    /// </summary>
    public class DrawingSizeSerializer : BsonBaseSerializer
    {
        // private static fields
        private static DrawingSizeSerializer __instance = new DrawingSizeSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the DrawingSizeSerializer class.
        /// </summary>
        public DrawingSizeSerializer()
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the DrawingSizeSerializer class.
        /// </summary>
        [Obsolete("Use constructor instead.")]
        public static DrawingSizeSerializer Instance
        {
            get { return __instance; }
        }

        // public methods
        /// <summary>
        /// Deserializes an object of type System.Drawing.Size from a BsonReader.
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
            VerifyTypes(nominalType, actualType, typeof(System.Drawing.Size));

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var width = bsonReader.ReadInt32("Width");
                    var height = bsonReader.ReadInt32("Height");
                    bsonReader.ReadEndDocument();
                    return new System.Drawing.Size(width, height);
                default:
                    var message = string.Format("Cannot deserialize Size from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes an object of type System.Drawing.Size  to a BsonWriter.
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
            var size = (System.Drawing.Size)value;
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteInt32("Width", size.Width);
            bsonWriter.WriteInt32("Height", size.Height);
            bsonWriter.WriteEndDocument();
        }
    }
}
