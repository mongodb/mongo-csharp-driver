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
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Versions.
    /// </summary>
    public class VersionSerializer : BsonBaseSerializer
    {
        // private static fields
        private static VersionSerializer __instance = new VersionSerializer();

        // constructors
        /// <summary>
        /// Initializes a new instance of the VersionSerializer class.
        /// </summary>
        public VersionSerializer()
            : base(new RepresentationSerializationOptions(BsonType.String))
        {
        }

        // public static properties
        /// <summary>
        /// Gets an instance of the VersionSerializer class.
        /// </summary>
        [Obsolete("Use constructor instead.")]
        public static VersionSerializer Instance
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
            VerifyTypes(nominalType, actualType, typeof(Version));

            BsonType bsonType = bsonReader.GetCurrentBsonType();
            string message;
            switch (bsonType)
            {
                case BsonType.Null:
                    bsonReader.ReadNull();
                    return null;
                case BsonType.Document:
                    bsonReader.ReadStartDocument();
                    int major = -1, minor = -1, build = -1, revision = -1;
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = bsonReader.ReadName();
                        switch (name)
                        {
                            case "Major": major = bsonReader.ReadInt32(); break;
                            case "Minor": minor = bsonReader.ReadInt32(); break;
                            case "Build": build = bsonReader.ReadInt32(); break;
                            case "Revision": revision = bsonReader.ReadInt32(); break;
                            default:
                                message = string.Format("Unrecognized element '{0}' while deserializing a Version value.", name);
                                throw new FileFormatException(message);
                        }
                    }
                    bsonReader.ReadEndDocument();
                    if (major == -1)
                    {
                        message = string.Format("Version missing Major element.");
                        throw new FileFormatException(message);
                    }
                    else if (minor == -1)
                    {
                        message = string.Format("Version missing Minor element.");
                        throw new FileFormatException(message);
                    }
                    else if (build == -1)
                    {
                        return new Version(major, minor);
                    }
                    else if (revision == -1)
                    {
                        return new Version(major, minor, build);
                    }
                    else
                    {
                        return new Version(major, minor, build, revision);
                    }
                case BsonType.String:
                    return new Version(bsonReader.ReadString());
                default:
                    message = string.Format("Cannot deserialize Version from BsonType {0}.", bsonType);
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
                var version = (Version)value;
                var representationSerializationOptions = EnsureSerializationOptions<RepresentationSerializationOptions>(options);

                switch (representationSerializationOptions.Representation)
                {
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteInt32("Major", version.Major);
                        bsonWriter.WriteInt32("Minor", version.Minor);
                        if (version.Build != -1)
                        {
                            bsonWriter.WriteInt32("Build", version.Build);
                            if (version.Revision != -1)
                            {
                                bsonWriter.WriteInt32("Revision", version.Revision);
                            }
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case BsonType.String:
                        bsonWriter.WriteString(version.ToString());
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid Version representation.", representationSerializationOptions.Representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
    }
}
