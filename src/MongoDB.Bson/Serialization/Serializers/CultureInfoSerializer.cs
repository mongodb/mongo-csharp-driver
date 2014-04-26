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
using System.Globalization;
using System.IO;
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for CultureInfos.
    /// </summary>
    public class CultureInfoSerializer : BsonBaseSerializer<CultureInfo>
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the CultureInfoSerializer class.
        /// </summary>
        public CultureInfoSerializer()
        {
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override CultureInfo Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;

                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    var name = bsonReader.ReadString("Name");
                    var useUserOverride = bsonReader.ReadBoolean("UseUserOverride");
                    bsonReader.ReadEndDocument();
                    return new CultureInfo(name, useUserOverride);

                case BsonType.String:
                    return new CultureInfo(bsonReader.ReadString());

                default:
                    var message = string.Format("Cannot deserialize CultureInfo from BsonType {0}.", bsonType);
                    throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, CultureInfo value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                if (value.UseUserOverride)
                {
                    // the default for UseUserOverride is true so we don't need to serialize it
                    bsonWriter.WriteString(value.Name);
                }
                else
                {
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("Name", value.Name);
                    bsonWriter.WriteBoolean("UseUserOverride", value.UseUserOverride);
                    bsonWriter.WriteEndDocument();
                }
            }
        }
    }
}
