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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for CultureInfos.
    /// </summary>
    public class CultureInfoSerializer : ClassSerializerBase<CultureInfo>
    {
        // private constants
        private static class Flags
        {
            public const long Name = 1;
            public const long UseUserOverride = 2;
        }

        // private fields
        private readonly BooleanSerializer _booleanSerializer = new BooleanSerializer();
        private readonly SerializerHelper _helper;

        // constructors
        /// <summary>
        /// Initializes a new instance of the CultureInfoSerializer class.
        /// </summary>
        public CultureInfoSerializer()
        {
            _helper = new SerializerHelper
            (
                new SerializerHelper.Member("Name", Flags.Name),
                new SerializerHelper.Member("UseUserOverride", Flags.UseUserOverride)
            );
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override CultureInfo DeserializeValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Document:
                    string name = null;
                    bool useUserOverride = false;
                    _helper.DeserializeMembers(context, (elementName, flag) =>
                    {
                        switch (flag)
                        {
                            case Flags.Name: name = bsonReader.ReadString(); break;
                            case Flags.UseUserOverride: useUserOverride = context.DeserializeWithChildContext(_booleanSerializer); break;
                        }
                    });
                    return new CultureInfo(name, useUserOverride);

                case BsonType.String:
                    return new CultureInfo(bsonReader.ReadString());

                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, CultureInfo value)
        {
            var bsonWriter = context.Writer;

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
