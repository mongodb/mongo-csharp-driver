/* Copyright 2010-2014 MongoDB Inc.
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
    public class DrawingSizeSerializer : StructSerializerBase<System.Drawing.Size>
    {
        // private constants
        private static class Flags
        {
            public const long Width = 1;
            public const long Height = 2;
        }

        // private fields
        private readonly SerializerHelper _helper;

        // constructors
        /// <summary>
        /// Initializes a new instance of the DrawingSizeSerializer class.
        /// </summary>
        public DrawingSizeSerializer()
        {
            _helper = new SerializerHelper
            (
                new SerializerHelper.Member("Width", Flags.Width),
                new SerializerHelper.Member("Height", Flags.Height)
            );
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override System.Drawing.Size Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    int width = 0, height = 0;
                    _helper.DeserializeMembers(context, (elementName, flag) =>
                    {
                        switch (flag)
                        {
                            case Flags.Width: width = bsonReader.ReadInt32(); break;
                            case Flags.Height: height = bsonReader.ReadInt32(); break;
                        }
                    });
                    return new System.Drawing.Size(width, height);

                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <summary>
        /// Serializes an object of type System.Drawing.Size  to a BsonWriter.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, System.Drawing.Size value)
        {
            var bsonWriter = context.Writer;

            bsonWriter.WriteStartDocument();
            bsonWriter.WriteInt32("Width", value.Width);
            bsonWriter.WriteInt32("Height", value.Height);
            bsonWriter.WriteEndDocument();
        }
    }
}
