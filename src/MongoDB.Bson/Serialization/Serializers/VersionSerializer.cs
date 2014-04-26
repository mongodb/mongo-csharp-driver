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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for Versions.
    /// </summary>
    public class VersionSerializer : BsonBaseSerializer<Version>, IRepresentationConfigurable<VersionSerializer>
    {
        // private fields
        private readonly BsonType _representation;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionSerializer"/> class.
        /// </summary>
        public VersionSerializer()
            : this(BsonType.String)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionSerializer"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public VersionSerializer(BsonType representation)
        {
            switch (representation)
            {
                case BsonType.Document:
                case BsonType.String:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for a VersionSerializer.", representation);
                    throw new ArgumentException(message);
            }

            _representation = representation;
        }

        // public properties
        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <value>
        /// The representation.
        /// </value>
        public BsonType Representation
        {
            get { return _representation; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override Version Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            string message;

            BsonType bsonType = bsonReader.GetCurrentBsonType();
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
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, Version value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                switch (_representation)
                {
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        bsonWriter.WriteInt32("Major", value.Major);
                        bsonWriter.WriteInt32("Minor", value.Minor);
                        if (value.Build != -1)
                        {
                            bsonWriter.WriteInt32("Build", value.Build);
                            if (value.Revision != -1)
                            {
                                bsonWriter.WriteInt32("Revision", value.Revision);
                            }
                        }
                        bsonWriter.WriteEndDocument();
                        break;

                    case BsonType.String:
                        bsonWriter.WriteString(value.ToString());
                        break;

                    default:
                        var message = string.Format("'{0}' is not a valid Version representation.", _representation);
                        throw new BsonSerializationException(message);
                }
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified representation.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <returns>The reconfigured serializer.</returns>
        public VersionSerializer WithRepresentation(BsonType representation)
        {
            if (representation == _representation)
            {
                return this;
            }
            else
            {
                return new VersionSerializer(representation);
            }
        }

        // explicit interface implementations
        IBsonSerializer IRepresentationConfigurable.WithRepresentation(BsonType representation)
        {
            return WithRepresentation(representation);
        }
    }
}
